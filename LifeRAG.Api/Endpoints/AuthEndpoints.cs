using LifeRAG.Core.DTOs;
using LifeRAG.Core.Entities;
using LifeRAG.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LifeRAG.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentication");

        group.MapPost("/register", async (
            [FromBody] RegisterRequest request,
            IRepository<User> userRepository,
            IAuthService authService) =>
        {
            var existingUser = await userRepository.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingUser != null)
            {
                return Results.BadRequest(new { message = "Email already registered" });
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                PasswordHash = authService.HashPassword(request.Password),
                FullName = request.FullName,
                CreatedAt = DateTime.UtcNow
            };

            await userRepository.AddAsync(user);
            await userRepository.SaveChangesAsync();

            var token = await authService.GenerateJwtToken(user.Id, user.Email);

            return Results.Ok(new AuthResponse(token, user.Id, user.Email));
        }).AllowAnonymous();

        group.MapPost("/login", async (
            [FromBody] LoginRequest request,
            IRepository<User> userRepository,
            IAuthService authService) =>
        {
            var user = await userRepository.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || !authService.VerifyPassword(request.Password, user.PasswordHash))
            {
                return Results.Unauthorized();
            }

            var token = await authService.GenerateJwtToken(user.Id, user.Email);

            return Results.Ok(new AuthResponse(token, user.Id, user.Email));
        }).AllowAnonymous();
    }
}
