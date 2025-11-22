using System.Security.Claims;
using LifeRAG.Core.DTOs;
using LifeRAG.Core.Entities;
using LifeRAG.Core.Interfaces;
using LifeRAG.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LifeRAG.Infrastructure.Data;

namespace LifeRAG.Api.Endpoints;

public static class ChatEndpoints
{
    public static void MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/chat")
            .WithTags("Chat")
            .RequireAuthorization();

        group.MapPost("/sessions", async (
            [FromBody] CreateChatSessionRequest request,
            HttpContext context,
            IRepository<ChatSession> sessionRepository) =>
        {
            var userId = Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var session = new ChatSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = request.Title,
                CreatedAt = DateTime.UtcNow
            };

            await sessionRepository.AddAsync(session);
            await sessionRepository.SaveChangesAsync();

            return Results.Ok(new CreateChatSessionResponse(
                session.Id,
                session.Title,
                session.CreatedAt
            ));
        });

        group.MapGet("/sessions", async (
            HttpContext context,
            AppDbContext dbContext) =>
        {
            var userId = Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var sessions = await dbContext.ChatSessions
                .Where(s => s.UserId == userId)
                .Select(s => new ChatSessionListItem(
                    s.Id,
                    s.Title,
                    s.CreatedAt,
                    s.UpdatedAt,
                    s.Messages.Count
                ))
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return Results.Ok(sessions);
        });

        group.MapGet("/sessions/{sessionId:guid}", async (
            Guid sessionId,
            HttpContext context,
            AppDbContext dbContext) =>
        {
            var userId = Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var session = await dbContext.ChatSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

            if (session == null)
            {
                return Results.NotFound();
            }

            var messages = session.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => new ChatMessageDto(m.Id, m.Role, m.Content, m.CreatedAt))
                .ToList();

            var result = new ChatSessionDto(
                session.Id,
                session.Title,
                session.CreatedAt,
                session.UpdatedAt,
                messages
            );

            return Results.Ok(result);
        });

        group.MapPost("/sessions/{sessionId:guid}/messages", async (
            Guid sessionId,
            [FromBody] SendMessageRequest request,
            HttpContext context,
            AppDbContext dbContext,
            IRepository<ChatSession> sessionRepository,
            IRepository<ChatMessage> messageRepository,
            SemanticKernelService semanticKernelService) =>
        {
            var userId = Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var session = await sessionRepository.GetByIdAsync(sessionId);
            if (session == null || session.UserId != userId)
            {
                return Results.NotFound();
            }

            var userMessage = new ChatMessage
            {
                Id = Guid.NewGuid(),
                ChatSessionId = sessionId,
                Role = "user",
                Content = request.Content,
                CreatedAt = DateTime.UtcNow
            };

            await messageRepository.AddAsync(userMessage);
            await messageRepository.SaveChangesAsync();

            var messages = await dbContext.ChatMessages
                .Where(m => m.ChatSessionId == sessionId && m.Id != userMessage.Id)
                .OrderByDescending(m => m.CreatedAt)
                .Take(6)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
            
            var chatHistory = messages.Select(m => (m.Role, m.Content)).ToList();

            var answer = await semanticKernelService.ChatAsync(request.Content, chatHistory);

            var assistantMessage = new ChatMessage
            {
                Id = Guid.NewGuid(),
                ChatSessionId = sessionId,
                Role = "assistant",
                Content = answer,
                CreatedAt = DateTime.UtcNow
            };

            await messageRepository.AddAsync(assistantMessage);

            session.UpdatedAt = DateTime.UtcNow;
            await sessionRepository.UpdateAsync(session);

            await messageRepository.SaveChangesAsync();

            return Results.Ok(new
            {
                userMessage = new ChatMessageDto(userMessage.Id, userMessage.Role, userMessage.Content, userMessage.CreatedAt),
                assistantMessage = new ChatMessageDto(assistantMessage.Id, assistantMessage.Role, assistantMessage.Content, assistantMessage.CreatedAt)
            });
        });

        group.MapDelete("/sessions/{sessionId:guid}", async (
            Guid sessionId,
            HttpContext context,
            IRepository<ChatSession> sessionRepository) =>
        {
            var userId = Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var session = await sessionRepository.GetByIdAsync(sessionId);
            if (session == null || session.UserId != userId)
            {
                return Results.NotFound();
            }

            await sessionRepository.DeleteAsync(session);
            await sessionRepository.SaveChangesAsync();

            return Results.NoContent();
        });
    }
}
