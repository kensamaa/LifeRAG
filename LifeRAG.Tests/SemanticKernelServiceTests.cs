using FluentAssertions;
using LifeRAG.Core.Interfaces;
using LifeRAG.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LifeRAG.Tests;

public class SemanticKernelServiceTests
{
    private static IConfiguration BuildConfiguration(bool useOpenAi = false) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SemanticKernel:UseOpenAI"] = useOpenAi.ToString(),
                ["SemanticKernel:OpenAI:ApiKey"] = "test-key",
                ["SemanticKernel:OpenAI:Model"] = "gpt-test",
                ["SemanticKernel:Ollama:Url"] = "http://localhost:11434",
                ["SemanticKernel:Ollama:Model"] = "llama3.1:8b",
            })
            .Build();

    [Fact]
    public async Task ChatAsync_ShouldReturnFallbackMessageOnRagError()
    {
        // Arrange
        var configuration = BuildConfiguration();
        var ragService = new Mock<IRagService>();
        var logger = Mock.Of<ILogger<SemanticKernelService>>();

        ragService
            .Setup(r => r.QueryAsync(It.IsAny<string>(), It.IsAny<List<(string role, string content)>>()))
            .ThrowsAsync(new Exception("RAG failure"));

        var service = new SemanticKernelService(configuration, ragService.Object, logger);

        // Act
        var result = await service.ChatAsync("Hello", new List<(string role, string content)> { ("user", "Hi") });

        // Assert
        result.Should().Be("I encountered an error processing your request. Please try again.");
    }
}
