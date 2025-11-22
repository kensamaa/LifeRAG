namespace LifeRAG.Core.Interfaces;

public interface IBackgroundJobService
{
    string EnqueueDocumentIngestion(Guid documentId);
    string EnqueueDocumentDeletion(Guid documentId);
}
