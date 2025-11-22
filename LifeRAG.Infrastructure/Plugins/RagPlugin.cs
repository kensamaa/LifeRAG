using System.ComponentModel;
using LifeRAG.Core.Interfaces;
using Microsoft.SemanticKernel;

namespace LifeRAG.Infrastructure.Plugins;

public class RagPlugin
{
    private readonly IRagService _ragService;

    public RagPlugin(IRagService ragService)
    {
        _ragService = ragService;
    }

    [KernelFunction, Description("Retrieve relevant context from user's personal knowledge base")]
    public async Task<string> RetrieveContext(
        [Description("The user's question or query")] string query,
        [Description("Chat history as JSON string")] string? chatHistory = null)
    {
        var history = new List<(string role, string content)>();
        
        if (!string.IsNullOrEmpty(chatHistory))
        {
            try
            {
                var historyItems = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, string>>>(chatHistory);
                if (historyItems != null)
                {
                    history = historyItems
                        .Select(h => (h.GetValueOrDefault("role", "user"), h.GetValueOrDefault("content", "")))
                        .ToList();
                }
            }
            catch { }
        }

        return await _ragService.QueryAsync(query, history);
    }
}
