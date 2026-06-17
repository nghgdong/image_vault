namespace ImageVault.Application.Common;

/// <summary>
/// Lỗi nghiệp vụ có mã HTTP tương ứng. API map sang ProblemDetails (RFC 7807) — SPEC §4.3.
/// </summary>
public abstract class AppException : Exception
{
    public abstract int StatusCode { get; }

    protected AppException(string message) : base(message) { }
}

/// <summary>404 — không tìm thấy tài nguyên.</summary>
public sealed class NotFoundException : AppException
{
    public override int StatusCode => 404;
    public NotFoundException(string message) : base(message) { }
}

/// <summary>400 — dữ liệu vào không hợp lệ (vd move folder vào con cháu của nó).</summary>
public sealed class ValidationAppException : AppException
{
    public override int StatusCode => 400;
    public ValidationAppException(string message) : base(message) { }
}

/// <summary>401 — chưa xác thực / sai thông tin đăng nhập.</summary>
public sealed class UnauthorizedAppException : AppException
{
    public override int StatusCode => 401;
    public UnauthorizedAppException(string message) : base(message) { }
}

/// <summary>409 — xung đột (trùng tên trong cùng parent, vòng lặp khi xóa có con...).</summary>
public sealed class ConflictException : AppException
{
    public override int StatusCode => 409;
    public ConflictException(string message) : base(message) { }
}

/// <summary>502 — lỗi upstream (freeimage.host).</summary>
public sealed class UpstreamException : AppException
{
    public override int StatusCode => 502;
    public UpstreamException(string message) : base(message) { }
}
