using FluentAssertions;
using LifeRAG.Core.Entities;
using LifeRAG.Infrastructure.Data;
using LifeRAG.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LifeRAG.Tests;

public class RepositoryTests
{
    private static Repository<T> CreateRepository<T>() where T : class
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        return new Repository<T>(context);
    }

    [Fact]
    public async Task AddAndGetByIdAsync_ShouldPersistEntity()
    {
        // Arrange
        var repository = CreateRepository<User>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "repo@test.com",
            PasswordHash = "hash",
            FullName = "Repo Tester",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await repository.AddAsync(user);
        await repository.SaveChangesAsync();
        var retrieved = await repository.GetByIdAsync(user.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Email.Should().Be("repo@test.com");
    }

    [Fact]
    public async Task FindAsync_ShouldFilterEntities()
    {
        // Arrange
        var repository = CreateRepository<Document>();
        var documents = new[]
        {
            new Document { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), FileName = "a.pdf", ContentType = "application/pdf", FileSize = 10, FileData = Array.Empty<byte>(), UploadedAt = DateTime.UtcNow },
            new Document { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), FileName = "b.pdf", ContentType = "application/pdf", FileSize = 20, FileData = Array.Empty<byte>(), UploadedAt = DateTime.UtcNow }
        };

        foreach (var doc in documents)
        {
            await repository.AddAsync(doc);
        }
        await repository.SaveChangesAsync();

        // Act
        var results = await repository.FindAsync(d => d.FileSize > 10);

        // Assert
        results.Should().ContainSingle();
        results.First().FileName.Should().Be("b.pdf");
    }
}
