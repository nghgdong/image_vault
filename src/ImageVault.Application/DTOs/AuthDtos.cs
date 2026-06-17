namespace ImageVault.Application.DTOs;

public sealed record LoginRequest(string Username, string Password);

public sealed record LoginResponse(string Token, DateTime ExpiresAt);
