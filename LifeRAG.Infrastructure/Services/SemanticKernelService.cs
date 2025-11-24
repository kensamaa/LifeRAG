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
            var chatHistoryJson = System.Text.Json.JsonSerializer.Serialize(
                chatHistory.Select(h => new Dictionary<string, string>
                {
                    ["role"] = h.role,
                    ["content"] = h.content
                }).ToList()
            );

            var prompt = $@"You are a helpful AI assistant.

Chat History:
{string.Join("\n", chatHistory.Select(h => $"{h.role.ToUpper()}: {h.content}"))}

User: {userMessage}";

            var function = _kernel.CreateFunctionFromPrompt(
                prompt,
                new OpenAIPromptExecutionSettings
                {
                    Temperature = 0.7,
                    MaxTokens = 1000
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
