namespace LifeRAG.Core.DTOs;

public record CreateChatSessionRequest(string Title);
public record CreateChatSessionResponse(Guid Id, string Title, DateTime CreatedAt);

public record SendMessageRequest(string Content);
public record ChatMessageDto(Guid Id, string Role, string Content, DateTime CreatedAt);

public record ChatSessionDto(Guid Id, string Title, DateTime CreatedAt, DateTime? UpdatedAt, List<ChatMessageDto> Messages);

public record ChatSessionListItem(Guid Id, string Title, DateTime CreatedAt, DateTime? UpdatedAt, int MessageCount);
