namespace LifeRAG.Core.Interfaces;

public interface IAuthService
{
    Task<string> GenerateJwtToken(Guid userId, string email);
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}
