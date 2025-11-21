using System.Security.Claims;
using LifeRAG.Core.DTOs;
using LifeRAG.Core.Entities;
using LifeRAG.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LifeRAG.Api.Endpoints;

public static class DocumentEndpoints
{
    public static void MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/documents")
            .WithTags("Documents")
            .RequireAuthorization();

        group.MapPost("/upload", async (
            HttpContext context,
            IFormFile file,
            IRepository<Document> documentRepository) =>
        {
            var userId = Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (file.Length == 0)
            {
                return Results.BadRequest(new { message = "File is empty" });
            }

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);

            var document = new Document
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                FileData = memoryStream.ToArray(),
                UploadedAt = DateTime.UtcNow
            };

            await documentRepository.AddAsync(document);
            await documentRepository.SaveChangesAsync();

            return Results.Ok(new DocumentUploadResponse(
                document.Id,
                document.FileName,
                document.FileSize,
                document.UploadedAt
            ));
        }).DisableAntiforgery();

        group.MapGet("/", async (
            HttpContext context,
            IRepository<Document> documentRepository) =>
        {
            var userId = Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var documents = await documentRepository.FindAsync(d => d.UserId == userId);

            var result = documents.Select(d => new DocumentListItem(
                d.Id,
                d.FileName,
                d.ContentType,
                d.FileSize,
                d.UploadedAt
            )).ToList();

            return Results.Ok(result);
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            HttpContext context,
            IRepository<Document> documentRepository) =>
        {
            var userId = Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var document = await documentRepository.GetByIdAsync(id);

            if (document == null || document.UserId != userId)
            {
                return Results.NotFound();
            }

            return Results.File(document.FileData, document.ContentType, document.FileName);
        });

        group.MapDelete("/{id:guid}", async (
            Guid id,
            HttpContext context,
            IRepository<Document> documentRepository) =>
        {
            var userId = Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var document = await documentRepository.GetByIdAsync(id);

            if (document == null || document.UserId != userId)
            {
                return Results.NotFound();
            }

            await documentRepository.DeleteAsync(document);
            await documentRepository.SaveChangesAsync();

            return Results.NoContent();
        });
    }
}
