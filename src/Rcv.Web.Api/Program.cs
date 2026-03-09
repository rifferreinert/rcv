using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Rcv.Web.Api.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

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
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

    if (builder.Environment.IsDevelopment())
        options.UseSqlite(connectionString);
    else
        options.UseSqlServer(connectionString);
});

// Configure Authentication: JWT Bearer (default) + temporary external cookie + OAuth providers
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    // JWT Bearer: validates the httpOnly cookie named "rcv_jwt".
    // Config is read inside the lambda so WebApplicationFactory overrides are picked up.
    .AddJwtBearer(options =>
    {
        var jwtCfg = builder.Configuration.GetSection("Authentication:Jwt");
        var key = jwtCfg["SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey is not configured.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
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
    // Google OAuth2 — credentials read lazily so WebApplicationFactory overrides are picked up.
    // If credentials are not configured, the scheme is still registered but OAuth flow will fail
    // at the provider level. Use the dev-login endpoint for local development instead.
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "not-configured";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "not-configured";
        options.SignInScheme = "External";
        options.CallbackPath = "/api/auth/signin/google";
    })
    // Microsoft Account OAuth2
    .AddMicrosoftAccount(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"] ?? "not-configured";
        options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"] ?? "not-configured";
        options.SignInScheme = "External";
        options.CallbackPath = "/api/auth/signin/microsoft";
    });

builder.Services.AddAuthorization();

// Register application services
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPollService, PollService>();
builder.Services.AddScoped<IVotingService, VotingService>();
builder.Services.AddScoped<IResultsService, ResultsService>();

var app = builder.Build();

// In development, automatically create the SQLite database schema on startup
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<Rcv.Web.Api.Data.RcvDbContext>();
    db.Database.EnsureCreated();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "RCV Web API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at the app's root
    });
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Expose Program to the test project so WebApplicationFactory<Program> can discover it.
public partial class Program { }
