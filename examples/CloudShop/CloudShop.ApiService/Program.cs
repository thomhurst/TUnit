using System.Text;
using CloudShop.ApiService.Data;
using CloudShop.ApiService.Endpoints;
using CloudShop.ApiService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (telemetry, health checks, resilience)
builder.AddServiceDefaults();

// Add Aspire-managed services
builder.AddNpgsqlDbContext<AppDbContext>("postgresdb");
builder.AddRedisClient("redis");
builder.AddRabbitMQClient("rabbitmq");

// Application services
builder.Services.AddSingleton<ProductCacheService>();
builder.Services.AddScoped<OrderEventPublisher>();
builder.Services.AddScoped<AuthService>();

// Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "cloudshop-super-secret-key-for-testing-only-1234567890";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "cloudshop",
            ValidAudience = "cloudshop",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("admin", policy => policy.RequireRole("admin"));

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

// Create database schema and seed data on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Map endpoints
app.MapAuthEndpoints();
app.MapProductEndpoints();
app.MapOrderEndpoints();

app.MapGet("/", () => "CloudShop API is running");
app.MapDefaultEndpoints();

app.Run();

// Make Program class accessible for testing
public partial class Program;
