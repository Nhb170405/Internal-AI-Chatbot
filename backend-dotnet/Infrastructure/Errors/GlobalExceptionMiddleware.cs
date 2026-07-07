using System.Text.Json;

namespace backend_dotnet.Infrastructure.Errors;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Bai tap Milestone 16:
        // 1. Goi _next(context) trong try.
        // 2. Neu co exception thi log loi bang _logger.
        // 3. Map exception thanh status code va error code an toan.
        // 4. Tra ve JSON ApiErrorResponse.
        // 5. Khong tra stack trace, SQL detail, local path, API key ra client.
        //
        // Goi y thiet ke:
        // - KeyNotFoundException -> 404 / "not_found".
        // - ArgumentException -> 400 / "bad_request".
        // - UnauthorizedAccessException -> 401 hoac 403 tuy ngu canh.
        // - InvalidOperationException -> 409 / "invalid_operation".
        // - Exception khac -> 500 / "internal_error".
        //
        // Chua gan middleware nay vao Program.cs cho den khi ban implement xong.
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var statusCode = StatusCodes.Status500InternalServerError;
            var code = "internal_error";
            var message = "An unexpected error occurred.";

            _logger.LogError(ex, "Unhandled API exception.");

            if (ex is ApiException apiException)
            {
                statusCode = apiException.StatusCode;
                code = apiException.Code;
                message = apiException.Message;
            }
            else if (ex is KeyNotFoundException)
            {
                statusCode = StatusCodes.Status404NotFound;
                code = "not_found";
                message = ex.Message;
            }
            else if (ex is ArgumentException)
            {
                statusCode = StatusCodes.Status400BadRequest;
                code = "bad_request";
                message = ex.Message;
            }
            else if (ex is UnauthorizedAccessException)
            {
                var isUnauthorized = string.Equals(ex.Message, "Unauthorized", StringComparison.OrdinalIgnoreCase);
                statusCode = isUnauthorized
                    ? StatusCodes.Status401Unauthorized
                    : StatusCodes.Status403Forbidden;
                code = isUnauthorized ? "unauthorized" : "forbidden";
                message = ex.Message;
            }
            else if (ex is InvalidOperationException)
            {
                statusCode = StatusCodes.Status409Conflict;
                code = "invalid_operation";
                message = ex.Message;
            }

            var response = CreateErrorResponse(context, code, message);
            await WriteJsonAsync(context, statusCode, response);
        }

    }

    private static ApiErrorResponse CreateErrorResponse(HttpContext context, string code, string message)
    {
        // Bai tap:
        // 1. Tao ApiErrorResponse.
        // 2. Gan TraceId = context.TraceIdentifier.
        // 3. Return object cho InvokeAsync serialize ra JSON.
        return new ApiErrorResponse
        {
            Code = code,
            Message = message,
            TraceId = context.TraceIdentifier
        };
    }

    private static async Task WriteJsonAsync(HttpContext context, int statusCode, ApiErrorResponse response)
    {
        // Bai tap:
        // 1. Set context.Response.StatusCode.
        // 2. Set ContentType = "application/json".
        // 3. Dung JsonSerializer.SerializeAsync de ghi response.
        //
        // Goi y:
        // await JsonSerializer.SerializeAsync(context.Response.Body, response, cancellationToken: context.RequestAborted);
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(context.Response.Body, response, cancellationToken: context.RequestAborted);
    }
}
