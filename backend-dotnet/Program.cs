using backend_dotnet.Modules.Auth;
using backend_dotnet.Modules.Audit;
using backend_dotnet.Modules.Sessions;
using backend_dotnet.Modules.Users;
using backend_dotnet.Modules.Chat;
using Microsoft.EntityFrameworkCore;
using backend_dotnet.Infrastructure.Persistence;
using backend_dotnet.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using backend_dotnet.Infrastructure.OpenAI;
using backend_dotnet.Infrastructure.Retention;
using backend_dotnet.Infrastructure.Storage;
using backend_dotnet.Modules.Documents;
using backend_dotnet.Infrastructure.Python;
using backend_dotnet.Modules.Rag;
using backend_dotnet.Modules.Datasets;
using backend_dotnet.Modules.Charts;
using backend_dotnet.Modules.Assistant;
using backend_dotnet.Modules.Admin;
using backend_dotnet.Modules.BackgroundJobs;
using Hangfire;
using Hangfire.SqlServer;
using backend_dotnet.Infrastructure.Errors;
using backend_dotnet.Infrastructure.Security;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Dang ky Controllers và Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpContextAccessor();


// Dang ky service cua ung dung
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddScoped<ChatHistoryService>();

builder.Services.AddScoped<DocumentService>();
builder.Services.Configure<FileValidationOptions>(
    builder.Configuration.GetSection("FileValidation"));

builder.Services.AddScoped<FileValidationService>();

builder.Services.AddScoped<DocumentIngestionService>();
builder.Services.AddScoped<DocumentChunkingService>();
builder.Services.AddScoped<DocumentIndexingService>();
builder.Services.AddScoped<DocumentMetadataService>();
builder.Services.AddScoped<DocumentMetadataRoutingService>();
builder.Services.AddScoped<DatasetProfileService>();
builder.Services.AddScoped<DatasetAnalysisService>();
builder.Services.AddScoped<ChartService>();
builder.Services.AddScoped<ChartFileService>();
builder.Services.AddScoped<AssistantRouter>();
builder.Services.AddScoped<AssistantDatasetProfileHandler>();
builder.Services.AddScoped<AssistantService>();
builder.Services.AddScoped<AdminAuditLogService>();
builder.Services.AddScoped<AdminDashboardService>();
builder.Services.AddScoped<AdminUserService>();
builder.Services.AddScoped<DocumentProcessingJobService>();
builder.Services.AddScoped<DocumentProcessingJobRunner>();
builder.Services.AddScoped<DocumentProcessingJobHandler>();
builder.Services.AddScoped<DeletedDocumentPurgeJobHandler>();
builder.Services.AddScoped<PromptBuilder>();
builder.Services.AddScoped<RagService>();
builder.Services.AddScoped<LocalFileStorageService>();

builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(option =>
{
    option.LoginPath = "/api/auth/login";
    option.LogoutPath = "/api/auth/logout";
    option.AccessDeniedPath = "/api/auth/access-denied";
    option.Cookie.HttpOnly = true;
    option.Cookie.Name = "internal_chatbot_auth";
    option.Cookie.SameSite = SameSiteMode.Lax;
    option.ExpireTimeSpan = TimeSpan.FromHours(8);
    option.SlidingExpiration = true;
    if (builder.Environment.IsDevelopment())
    {
        option.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    }
    else
    {
        option.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    }
    option.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        },
        OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Cau hinh DbContext voi SQL Server. Connection string se duoc lay tu appsettings.Development.json
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var defaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(defaultConnectionString))
{
    throw new InvalidOperationException("Missing DefaultConnection connection string.");
}

// Hangfire dung SQL Server de luu queue/state cua background jobs.
// Luu y: Hangfire se tu tao cac bang rieng khi app chay lan dau.
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(defaultConnectionString, new SqlServerStorageOptions
    {
        SchemaName = "Hangfire",
        PrepareSchemaIfNecessary = true
    }));

builder.Services.AddHangfireServer();

builder.Services.Configure<LocalFileStorageOptions>(
    builder.Configuration.GetSection("LocalFileStorage"));

builder.Services.Configure<DocumentRetentionOptions>(
    builder.Configuration.GetSection("DocumentRetention"));

builder.Services.Configure<PythonServiceOptions>(
    builder.Configuration.GetSection("PythonService"));

builder.Services.Configure<ChartStorageOptions>(
    builder.Configuration.GetSection("ChartStorage"));

builder.Services.AddHttpClient<PythonIngestionClient>();
builder.Services.AddHttpClient<PythonChunkingClient>();
builder.Services.AddHttpClient<PythonVectorClient>();
builder.Services.AddHttpClient<PythonDatasetClient>();
builder.Services.AddHttpClient<PythonChartClient>();

var openAIOptions = builder.Configuration.GetSection("OpenAI").Get<OpenAIOptions>();

if (openAIOptions == null)
{
    throw new InvalidOperationException("Missing OpenAI configuration section.");
}

if (string.IsNullOrWhiteSpace(openAIOptions.ApiKey))
{
    throw new InvalidOperationException("Missing OpenAI:ApiKey configuration.");
}

if (string.IsNullOrWhiteSpace(openAIOptions.ChatModel))
{
    throw new InvalidOperationException("Missing OpenAI:ChatModel configuration.");
}

builder.Services.AddSingleton(openAIOptions);

builder.Services.AddHttpClient<OpenAIClient>();
builder.Services.AddScoped<ChatService>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";

        await context.HttpContext.Response.WriteAsJsonAsync(new ApiErrorResponse
        {
            Code = "rate_limit_exceeded",
            Message = "Too many requests. Please try again later.",
            TraceId = context.HttpContext.TraceIdentifier
        }, cancellationToken);
    };

    options.AddPolicy("auth-login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetIpKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("chat", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetUserOrGuestOrIpKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("upload", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetUserOrGuestOrIpKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

var app = builder.Build();

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    await DevelopmentDataSeeder.SeedAsync(app.Services);
}

// Cau hinh Swagger khi chay moi truong Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Dashboard de debug background jobs trong development.
    // Khi len production phai bao ve bang admin auth/filter, khong public dashboard.
    app.UseHangfireDashboard("/hangfire");
}

app.UseHttpsRedirection();

// Middleware
app.UseRouting();
app.UseCors("FrontendDev");
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();


app.Run();



/////////////////////////////////////////////////////////////////////////////////
// Helper
/////////////////////////////////////////////////////////////////////////////////

static string GetIpKey(HttpContext context)
{
    return context.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";
}

static string GetUserOrGuestOrIpKey(HttpContext context)
{
    var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!string.IsNullOrWhiteSpace(userId))
    {
        return "user:" + userId;
    }

    var guestSessionId = context.User.FindFirstValue("guest_session_id");
    if (!string.IsNullOrWhiteSpace(guestSessionId))
    {
        return "guest:" + guestSessionId;
    }

    return "ip:" + GetIpKey(context);
}
