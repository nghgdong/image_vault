using ImageVault.Application.DTOs;
using ImageVault.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ImageVault.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth) => _auth = auth;

    /// <summary>Đăng nhập admin → JWT (SPEC §4.2).</summary>
    [HttpPost("login")]
    public Task<LoginResponse> Login([FromBody] LoginRequest request, CancellationToken ct)
        => _auth.LoginAsync(request, ct);
}
