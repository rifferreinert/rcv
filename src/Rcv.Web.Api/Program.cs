using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Rcv.Web.Api.Services;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Rcv.Web.Api.Infrastructure;
using Serilog;
using Microsoft.AspNetCore.WebUtilities;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

// Add services to the container
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.Name = "rcv_xsrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.None
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services.AddMemoryCache();
builder.Services.AddSingleton(TimeProvider.System);

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "RCV Web API",
        Version = "v1",
        Description = "API for Ranked Choice Voting platform"
    });
});

// Configure CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Configure Database
builder.Services.AddDbContext<Rcv.Web.Api.Data.RcvDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
});

// Configure Authentication: JWT Bearer (default) + temporary external cookie + OAuth providers
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    // JWT Bearer: validates the httpOnly cookie named "rcv_jwt".
    // Config is read inside the lambda so WebApplicationFactory overrides are picked up.
    .AddJwtBearer(options =>
    {
        var jwtCfg = builder.Configuration.GetSection("Authentication:Jwt");
        var key = JwtSigningKeyValidator.CreateSecurityKey(jwtCfg["SecretKey"]);

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = true,
            ValidIssuer = jwtCfg["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtCfg["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
        // Read the JWT from the httpOnly cookie instead of the Authorization header
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                ctx.Token = ctx.Request.Cookies["rcv_jwt"];
                return Task.CompletedTask;
            }
        };
    })
    // Temporary cookie used only to hold state during the OAuth2 handshake
    .AddCookie("External")
    // Google OAuth2
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]
            ?? throw new InvalidOperationException("Google ClientId is not configured.");
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
            ?? throw new InvalidOperationException("Google ClientSecret is not configured.");
        options.SignInScheme = "External";
        // Middleware handles the raw callback from Google at this path,
        // sets the External cookie, then redirects to our controller route.
        options.CallbackPath = "/api/auth/signin/google";
    })
    // Microsoft Account OAuth2
    .AddMicrosoftAccount(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"]
            ?? throw new InvalidOperationException("Microsoft ClientId is not configured.");
        options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"]
            ?? throw new InvalidOperationException("Microsoft ClientSecret is not configured.");
        options.SignInScheme = "External";
        // Middleware handles the raw callback from Microsoft at this path,
        // sets the External cookie, then redirects to our controller route.
        options.CallbackPath = "/api/auth/signin/microsoft";
    });

builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        await Results.Problem(
            statusCode: StatusCodes.Status429TooManyRequests,
            title: "Too many requests",
            detail: "The request rate limit has been exceeded.")
            .ExecuteAsync(context.HttpContext);
    };
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        }));
    options.AddPolicy("votes", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        }));
});

// Register application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPollService, PollService>();
builder.Services.AddScoped<IVotingService, VotingService>();
builder.Services.AddScoped<IPollLifecycleService, PollLifecycleService>();
builder.Services.AddScoped<IResultsService, ResultsService>();
builder.Services.AddScoped<DatabaseReadyHealthCheck>();
builder.Services.AddHostedService<JwtConfigurationValidationService>();
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseReadyHealthCheck>("database", tags: new[] { "ready" });

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "RCV Web API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at the app's root
    });
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

if (app.Environment.IsDevelopment())
    app.UseCors();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages(async context =>
{
    var httpContext = context.HttpContext;
    var status = httpContext.Response.StatusCode;
    var problemDetails = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
    await problemDetails.TryWriteAsync(new ProblemDetailsContext
    {
        HttpContext = httpContext,
        ProblemDetails = new ProblemDetails
        {
            Status = status,
            Title = ReasonPhrases.GetReasonPhrase(status),
            Instance = httpContext.Request.Path,
        },
    });
});
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false,
});
app.MapHealthChecks("/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});
if (!app.Environment.IsDevelopment())
{
    app.MapFallback(async context =>
    {
        var path = context.Request.Path;
        if (path.StartsWithSegments("/api") ||
            path.StartsWithSegments("/health") ||
            path.StartsWithSegments("/ready") ||
            path.StartsWithSegments("/swagger"))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var webRoot = app.Environment.WebRootPath
            ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
        var indexPath = Path.Combine(webRoot, "index.html");
        if (!File.Exists(indexPath))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.SendFileAsync(indexPath);
    });
}

app.Run();

// Expose Program to the test project so WebApplicationFactory<Program> can discover it.
public partial class Program { }
