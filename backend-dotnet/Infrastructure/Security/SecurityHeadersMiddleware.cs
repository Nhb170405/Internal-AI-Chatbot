namespace backend_dotnet.Infrastructure.Security;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Bai tap Milestone 16 - Security headers baseline:
        //
        // Muc tieu:
        // - Them mot so HTTP response headers an toan mac dinh.
        // - Headers nay giup browser han che hanh vi nguy hiem.
        // - Chua them Content-Security-Policy o buoc nay vi CSP de lam hong Swagger/frontend neu cau hinh sai.
        //
        // Can lam:
        // 1. Them header "X-Content-Type-Options" = "nosniff".
        //    Tac dung: bao browser khong tu doan MIME type.
        //
        // 2. Them header "X-Frame-Options" = "DENY".
        //    Tac dung: khong cho site khac nhung app vao iframe, giam rui ro clickjacking.
        //
        // 3. Them header "Referrer-Policy" = "no-referrer".
        //    Tac dung: khong gui URL noi bo sang website khac qua referrer.
        //
        // 4. Them header "Permissions-Policy" = "camera=(), microphone=(), geolocation=()".
        //    Tac dung: tat cac browser permissions khong can cho app chatbot/document.
        //
        // Goi y cu phap:
        // context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
        //
        // Ly do dung TryAdd:
        // - Neu header da ton tai tu middleware/proxy khac thi khong ghi de.
        //
        // 5. Goi await _next(context) de request di tiep middleware phia sau.
        context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
        context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
        context.Response.Headers.TryAdd("Referrer-Policy", "no-referrer");
        context.Response.Headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");

        await _next(context);

    }
}
