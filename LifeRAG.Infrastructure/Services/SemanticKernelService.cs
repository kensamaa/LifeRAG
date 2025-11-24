using LifeRAG.Core.Interfaces;
using LifeRAG.Infrastructure.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace LifeRAG.Infrastructure.Services;

public class SemanticKernelService
{
    private readonly Kernel _kernel;
    private readonly ILogger<SemanticKernelService> _logger;

    public SemanticKernelService(
        IConfiguration configuration,
        IRagService ragService,
        ILogger<SemanticKernelService> logger)
    {
        _logger = logger;

        var builder = Kernel.CreateBuilder();

        var useOpenAI = configuration.GetValue<bool>("SemanticKernel:UseOpenAI");
        
        if (useOpenAI)
        {
            var apiKey = configuration["SemanticKernel:OpenAI:ApiKey"];
            var model = configuration["SemanticKernel:OpenAI:Model"] ?? "gpt-4o-mini";
            builder.AddOpenAIChatCompletion(model, apiKey!);
        }
        else
        {
            var ollamaUrl = configuration["SemanticKernel:Ollama:Url"] ?? "http://localhost:11434";
            var model = configuration["SemanticKernel:Ollama:Model"] ?? "llama3.1:8b";
            
            // Ollama's OpenAI-compatible endpoint
            var ollamaEndpoint = new Uri($"{ollamaUrl}/v1");
            
            builder.AddOpenAIChatCompletion(
                modelId: model,
                endpoint: ollamaEndpoint,
                apiKey: "not-needed"
            );
        }

        _kernel = builder.Build();
        
        var ragPlugin = new RagPlugin(ragService);
        _kernel.ImportPluginFromObject(ragPlugin, "rag");

        _logger.LogInformation("Semantic Kernel initialized with RAG plugin");
    }

    public async Task<string> ChatAsync(
        string userMessage,
        List<(string role, string content)> chatHistory)
    {
        try
        {
            // Get the chat completion service
            var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
            
            // Create chat history with system message
            var history = new ChatHistory();
            history.AddSystemMessage(@"You are a helpful AI assistant with access to the user's personal knowledge base.
When answering questions, use the RetrieveContext function to search for relevant information from uploaded documents.
When citing sources, always mention the specific document filename (e.g., 'Profile.pdf', 'Resume.docx') instead of generic labels.
Format your sources like this: Source: [actual_filename.pdf]");

            // Add previous conversation history
            foreach (var (role, content) in chatHistory)
            {
                if (role.ToLower() == "user")
                {
                    history.AddUserMessage(content);
                }
                else if (role.ToLower() == "assistant")
                {
                    history.AddAssistantMessage(content);
                }
            }

            // Add current user message
            history.AddUserMessage(userMessage);

            // Prepare chat history JSON for the RAG plugin
            var chatHistoryJson = System.Text.Json.JsonSerializer.Serialize(
                chatHistory.Select(h => new Dictionary<string, string>
                {
                    ["role"] = h.role,
                    ["content"] = h.content
                }).ToList()
            );

            // Enable automatic function calling
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.7,
                MaxTokens = 1000,
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };

            // Get response with automatic RAG invocation
            var result = await chatCompletionService.GetChatMessageContentAsync(
                history,
                executionSettings,
                _kernel
            );

            return result.Content ?? "I couldn't generate a response.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Semantic Kernel chat");
            return "I encountered an error processing your request. Please try again.";
        }
    }
}
