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
            builder.AddOpenAIChatCompletion(
                modelId: model,
                endpoint: new Uri(ollamaUrl),
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
            var chatHistoryJson = System.Text.Json.JsonSerializer.Serialize(
                chatHistory.Select(h => new Dictionary<string, string>
                {
                    ["role"] = h.role,
                    ["content"] = h.content
                }).ToList()
            );

            var prompt = @$"
You are a highly intelligent personal AI assistant with access to the user's private knowledge base.

Your capabilities:
1. Use the {{{{rag.RetrieveContext}}}} function to search the user's documents, notes, and personal data
2. Provide accurate answers based on retrieved context
3. Maintain conversation context from chat history
4. If information isn't in the knowledge base, say so clearly

Chat History:
{string.Join("\n", chatHistory.Select(h => $"{h.role.ToUpper()}: {h.content}"))}

User Question: {userMessage}

Instructions:
- First, retrieve relevant context using the rag plugin
- Then provide a comprehensive answer based on that context
- Cite sources when possible
- Be conversational and helpful
";

            var function = _kernel.CreateFunctionFromPrompt(
                prompt,
                new OpenAIPromptExecutionSettings
                {
                    Temperature = 0.7,
                    MaxTokens = 1000,
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                }
            );

            var result = await _kernel.InvokeAsync(function, new KernelArguments
            {
                ["input"] = userMessage,
                ["chatHistory"] = chatHistoryJson
            });

            return result.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Semantic Kernel chat");
            return "I encountered an error processing your request. Please try again.";
        }
    }
}
