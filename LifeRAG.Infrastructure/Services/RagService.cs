using System.Text;
using System.Text.Json;
using LifeRAG.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LifeRAG.Infrastructure.Services;

public class RagService : IRagService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RagService> _logger;
    private readonly string _pythonServiceUrl;

    public RagService(IConfiguration configuration, ILogger<RagService> logger)
    {
        _logger = logger;
        _pythonServiceUrl = configuration["PythonRagService:Url"] ?? "http://localhost:8000";
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_pythonServiceUrl),
            Timeout = TimeSpan.FromMinutes(5)
        };
    }

    public async Task<bool> IngestDocumentAsync(Guid documentId, byte[] fileData, string fileName, string contentType)
    {
        try
        {
            _logger.LogInformation("Ingesting document {DocumentId} to RAG service", documentId);

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(fileData);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            content.Add(fileContent, "file", fileName);
            content.Add(new StringContent(documentId.ToString()), "document_id");

            var response = await _httpClient.PostAsync("/ingest", content);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Document {DocumentId} ingested successfully: {Result}", documentId, result);
                return true;
            }

            _logger.LogError("Failed to ingest document {DocumentId}: {StatusCode}", documentId, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ingesting document {DocumentId}", documentId);
            return false;
        }
    }

    public async Task<string> QueryAsync(string query, List<(string role, string content)> chatHistory)
    {
        try
        {
            _logger.LogInformation("Querying RAG service with: {Query}", query);

            var request = new
            {
                query,
                chat_history = chatHistory.Select(h => new { role = h.role, content = h.content }).ToList(),
                top_k = 5
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/query", content);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(result);
                var answer = jsonDoc.RootElement.GetProperty("answer").GetString();
                return answer ?? "No answer generated";
            }

            _logger.LogError("Failed to query RAG service: {StatusCode}", response.StatusCode);
            return "Sorry, I couldn't process your query at the moment.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying RAG service");
            return "An error occurred while processing your query.";
        }
    }

    public async Task<bool> DeleteDocumentAsync(Guid documentId)
    {
        try
        {
            _logger.LogInformation("Deleting document {DocumentId} from RAG service", documentId);

            var response = await _httpClient.DeleteAsync($"/documents/{documentId}");
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Document {DocumentId} deleted successfully", documentId);
                return true;
            }

            _logger.LogError("Failed to delete document {DocumentId}: {StatusCode}", documentId, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
            return false;
        }
    }
}
