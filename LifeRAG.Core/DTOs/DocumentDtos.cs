namespace LifeRAG.Core.DTOs;

public record DocumentUploadResponse(Guid Id, string FileName, long FileSize, DateTime UploadedAt);

public record DocumentListItem(Guid Id, string FileName, string ContentType, long FileSize, DateTime UploadedAt);
