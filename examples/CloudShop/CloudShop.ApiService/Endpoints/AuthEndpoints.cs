using CloudShop.ApiService.Services;
using CloudShop.Shared.Contracts;

namespace CloudShop.ApiService.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentication");

        group.MapPost("/login", async (LoginRequest request, AuthService authService) =>
        {
            var result = await authService.AuthenticateAsync(request);
            return result is not null
                ? Results.Ok(result)
                : Results.Unauthorized();
        });

        group.MapPost("/register", async (RegisterRequest request, AuthService authService) =>
        {
            var result = await authService.RegisterAsync(request);
            return result is not null
                ? Results.Ok(result)
                : Results.Conflict(new ErrorResponse("Email already registered"));
        });
    }
}
