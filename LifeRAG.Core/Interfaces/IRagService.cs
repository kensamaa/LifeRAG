namespace LifeRAG.Core.Interfaces;

public interface IRagService
{
    Task<bool> IngestDocumentAsync(Guid documentId, byte[] fileData, string fileName, string contentType);
    Task<string> QueryAsync(string query, List<(string role, string content)> chatHistory);
    Task<bool> DeleteDocumentAsync(Guid documentId);
}
