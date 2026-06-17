using ImageVault.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ImageVault.Api.Middleware;

/// <summary>
/// Map exception → ProblemDetails (RFC 7807) — SPEC §4.3.
/// AppException có sẵn StatusCode; lỗi khác → 500.
/// </summary>
public sealed class AppExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<AppExceptionHandler> _logger;

    public AppExceptionHandler(IProblemDetailsService problemDetails, ILogger<AppExceptionHandler> logger)
    {
        _problemDetails = problemDetails;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            AppException app => (app.StatusCode, ReasonFor(app.StatusCode)),
            _ => (StatusCodes.Status500InternalServerError, "Lỗi máy chủ"),
        };

        if (status >= 500)
            _logger.LogError(exception, "Lỗi chưa xử lý");
        else
            _logger.LogWarning("Lỗi nghiệp vụ {Status}: {Message}", status, exception.Message);

        httpContext.Response.StatusCode = status;

        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = status >= 500 ? "Đã xảy ra lỗi không mong muốn." : exception.Message,
            },
        });
    }

    private static string ReasonFor(int status) => status switch
    {
        400 => "Dữ liệu không hợp lệ",
        401 => "Chưa xác thực",
        403 => "Không có quyền",
        404 => "Không tìm thấy",
        409 => "Xung đột",
        502 => "Lỗi upstream",
        _ => "Lỗi",
    };
}
