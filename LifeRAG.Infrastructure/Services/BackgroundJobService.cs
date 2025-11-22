using Hangfire;
using Hangfire.Server;
using LifeRAG.Core.Entities;
using LifeRAG.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace LifeRAG.Infrastructure.Services;

public class BackgroundJobService : IBackgroundJobService
{
    private readonly ILogger<BackgroundJobService> _logger;

    public BackgroundJobService(ILogger<BackgroundJobService> logger)
    {
        _logger = logger;
    }

    public string EnqueueDocumentIngestion(Guid documentId)
    {
        var jobId = BackgroundJob.Enqueue<DocumentIngestionJob>(
            job => job.ProcessAsync(documentId, null!)
        );
        
        _logger.LogInformation("Enqueued document ingestion job {JobId} for document {DocumentId}", jobId, documentId);
        return jobId;
    }

    public string EnqueueDocumentDeletion(Guid documentId)
    {
        var jobId = BackgroundJob.Enqueue<DocumentDeletionJob>(
            job => job.ProcessAsync(documentId, null!)
        );
        
        _logger.LogInformation("Enqueued document deletion job {JobId} for document {DocumentId}", jobId, documentId);
        return jobId;
    }
}

public class DocumentIngestionJob
{
    private readonly IRepository<Document> _documentRepository;
    private readonly IRagService _ragService;
    private readonly ILogger<DocumentIngestionJob> _logger;

    public DocumentIngestionJob(
        IRepository<Document> documentRepository,
        IRagService ragService,
        ILogger<DocumentIngestionJob> logger)
    {
        _documentRepository = documentRepository;
        _ragService = ragService;
        _logger = logger;
    }

    public async Task ProcessAsync(Guid documentId, PerformContext context)
    {
        _logger.LogInformation("Processing document ingestion for {DocumentId}", documentId);

        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null)
        {
            _logger.LogWarning("Document {DocumentId} not found", documentId);
            return;
        }

        var success = await _ragService.IngestDocumentAsync(
            document.Id,
            document.FileData,
            document.FileName,
            document.ContentType
        );

        if (success)
        {
            _logger.LogInformation("Document {DocumentId} ingested successfully", documentId);
        }
        else
        {
            _logger.LogError("Failed to ingest document {DocumentId}", documentId);
            throw new Exception($"Failed to ingest document {documentId}");
        }
    }
}

public class DocumentDeletionJob
{
    private readonly IRagService _ragService;
    private readonly ILogger<DocumentDeletionJob> _logger;

    public DocumentDeletionJob(
        IRagService ragService,
        ILogger<DocumentDeletionJob> logger)
    {
        _ragService = ragService;
        _logger = logger;
    }

    public async Task ProcessAsync(Guid documentId, PerformContext context)
    {
        _logger.LogInformation("Processing document deletion for {DocumentId}", documentId);

        var success = await _ragService.DeleteDocumentAsync(documentId);

        if (success)
        {
            _logger.LogInformation("Document {DocumentId} deleted from RAG service", documentId);
        }
        else
        {
            _logger.LogWarning("Failed to delete document {DocumentId} from RAG service", documentId);
        }
    }
}
