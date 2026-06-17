using ImageVault.Application.Abstractions;
using ImageVault.Application.Common;
using ImageVault.Application.DTOs;

namespace ImageVault.Application.Services;

public sealed class AuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;

    public AuthService(IUserRepository users, IPasswordHasher hasher, IJwtTokenService jwt)
    {
        _users = users;
        _hasher = hasher;
        _jwt = jwt;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByUsernameAsync(request.Username ?? string.Empty, ct);

        // Cùng thông báo cho cả 2 trường hợp (không tiết lộ username tồn tại hay không).
        if (user is null || !_hasher.Verify(request.Password ?? string.Empty, user.PasswordHash))
            throw new UnauthorizedAppException("Sai tên đăng nhập hoặc mật khẩu.");

        var token = _jwt.Generate(user);
        return new LoginResponse(token.Token, token.ExpiresAt);
    }
}
