using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using LifeRAG.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace LifeRAG.Tests;

public class AuthServiceTests
{
    private static IConfiguration BuildConfiguration()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "super-secret-test-key-12345678901234567890",
            ["Jwt:Issuer"] = "LifeRAG.Tests",
            ["Jwt:Audience"] = "LifeRAG.Tests"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    [Fact]
    public async Task GenerateJwtToken_ShouldIncludeUserClaims()
    {
        // Arrange
        var configuration = BuildConfiguration();
        var service = new AuthService(configuration);
        var userId = Guid.NewGuid();
        var email = "test@example.com";

        // Act
        var token = await service.GenerateJwtToken(userId, email);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var validatedToken = handler.ReadJwtToken(token);

        validatedToken.Claims.Should().ContainSingle(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userId.ToString());
        validatedToken.Claims.Should().ContainSingle(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == email);
        validatedToken.ValidTo.Should().BeAfter(DateTime.UtcNow);
        validatedToken.Issuer.Should().Be("LifeRAG.Tests");
        validatedToken.Audiences.Should().Contain("LifeRAG.Tests");
    }

    [Fact]
    public void HashPassword_ShouldProduceVerifiableHash()
    {
        // Arrange
        var configuration = BuildConfiguration();
        var service = new AuthService(configuration);
        var password = "P@ssword!234";

        // Act
        var hash = service.HashPassword(password);
        var isValid = service.VerifyPassword(password, hash);
        var isInvalid = service.VerifyPassword("wrong", hash);

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        isValid.Should().BeTrue();
        isInvalid.Should().BeFalse();
    }
}
