using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CloudShop.ApiService.Data;
using CloudShop.Shared.Contracts;
using CloudShop.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CloudShop.ApiService.Services;

public class AuthService(AppDbContext db, IConfiguration config)
{
    public async Task<TokenResponse?> AuthenticateAsync(LoginRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null) return null;

        var hash = HashPassword(request.Password);
        if (user.PasswordHash != hash) return null;

        return GenerateToken(user);
    }

    public async Task<TokenResponse?> RegisterAsync(RegisterRequest request)
    {
        if (await db.Users.AnyAsync(u => u.Email == request.Email))
            return null;

        var user = new User
        {
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            Role = "customer",
            Name = request.Name
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return GenerateToken(user);
    }

    private TokenResponse GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            config["Jwt:Key"] ?? "cloudshop-super-secret-key-for-testing-only-1234567890"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(24);

        var claims = new[]
        {
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim("sub", user.Id.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: "cloudshop",
            audience: "cloudshop",
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new TokenResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            user.Email,
            user.Role,
            expires);
    }

    private static string HashPassword(string password)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password + "cloudshop-salt"));
        return Convert.ToBase64String(bytes);
    }
}
