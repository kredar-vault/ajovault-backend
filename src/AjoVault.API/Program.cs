using System.Text;
using AjoVault.API.Account;
using AjoVault.API.Auth;
using AjoVault.API.Common;
using AjoVault.API.Config;
using Resend;
using AjoVault.API.Contributions;
using AjoVault.API.Dashboard;
using AjoVault.API.Data;
using AjoVault.API.Groups;
using AjoVault.API.Kredar;
using AjoVault.API.Payouts;
using AjoVault.API.Wallet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.Configure<KredarSettings>(builder.Configuration.GetSection("KredarSettings"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// Resend email
var resendApiKey = builder.Configuration["EmailSettings:ApiKey"] ?? "";
builder.Services.AddResend(options => options.ApiToken = resendApiKey);
builder.Services.AddScoped<EmailService>();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
        };
    });

builder.Services.AddAuthorization();

// CORS — allowed frontend origins come from config (Cors:AllowedOrigins, comma-separated),
// falling back to localhost for local dev.
var corsOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? "http://localhost:3000,https://localhost:3000")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// HTTP client for Kredar
builder.Services.AddHttpClient("kredar", c => { c.Timeout = TimeSpan.FromSeconds(30); });
builder.Services.AddScoped<KredarClient>();

// Services
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<GroupRepository>();
builder.Services.AddScoped<GroupsService>();
builder.Services.AddScoped<ContributionRepository>();
builder.Services.AddScoped<ContributionsService>();
builder.Services.AddScoped<PayoutRepository>();
builder.Services.AddScoped<PayoutsService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<WithdrawalRepository>();
builder.Services.AddScoped<WalletService>();


// Exception handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Health check
builder.Services.AddHealthChecks();

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AjoVault API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token here."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Auto-run migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseExceptionHandler();
app.UseCors("AllowFrontend");
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
