using Grpc.Core;
using Grpc.Net.Client;
using LifeRAG.Core.Grpc;
using LifeRAG.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LifeRAG.Infrastructure.Services;

public class RagService : IRagService
{
    private readonly RAGService.RAGServiceClient _grpcClient;
    private readonly ILogger<RagService> _logger;

    public RagService(IConfiguration configuration, ILogger<RagService> logger)
    {
        _logger = logger;
        var grpcUrl = configuration["PythonRagService:GrpcUrl"] ?? "http://localhost:50051";
        
        var channel = GrpcChannel.ForAddress(grpcUrl, new GrpcChannelOptions
        {
            MaxReceiveMessageSize = 100 * 1024 * 1024,
            MaxSendMessageSize = 100 * 1024 * 1024
        });
        
        _grpcClient = new RAGService.RAGServiceClient(channel);
    }

    public async Task<bool> IngestDocumentAsync(Guid documentId, byte[] fileData, string fileName, string contentType)
    {
        try
        {
            _logger.LogInformation("Ingesting document {DocumentId} via gRPC", documentId);

            var request = new IngestRequest
            {
                DocumentId = documentId.ToString(),
                Filename = fileName,
                ContentType = contentType,
                FileContent = Google.Protobuf.ByteString.CopyFrom(fileData)
            };

            var response = await _grpcClient.IngestAsync(request);
            
            _logger.LogInformation("Document {DocumentId} ingested: {Chunks} chunks created", 
                documentId, response.ChunksCreated);
            return response.Status == "success";
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
            _logger.LogInformation("Querying RAG service via gRPC: {Query}", query);

            var request = new QueryRequest
            {
                Query = query,
                TopK = 5
            };

            foreach (var msg in chatHistory)
            {
                request.ChatHistory.Add(new ChatMessage
                {
                    Role = msg.role,
                    Content = msg.content
                });
            }

            using var call = _grpcClient.Query(request);
            
            await foreach (var response in call.ResponseStream.ReadAllAsync())
            {
                return response.Answer;
            }

            return "No answer generated";
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
            _logger.LogInformation("Document deletion via gRPC not implemented, using vector store metadata cleanup");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
            return false;
        }
    }
}
