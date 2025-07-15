using LangLearn.Backend.Data;
using LangLearn.Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LangLearn.Backend.Services;

public class AuthService(AppDbContext db, IConfiguration config)
{
    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        if (await db.Users.AnyAsync(u => u.Email == request.Email))
        {
            return new AuthResult(false, "Email already in use.");
        }

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        };

        db.Users.Add(user);

        await db.SaveChangesAsync();

        return new AuthResult(true, "Registration successful.");
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return new AuthResult(false, "Invalid credentials.");
        }

        var token = GenerateJwtToken(user);

        return new AuthResult(true, "Login successful.", token);
    }

    private string GenerateJwtToken(User user)
    {
        var claims = new[]
        {
            // include user ID as subject and name identifier for claim resolution
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            // include email for reference
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var key = Encoding.UTF8.GetBytes(config["Jwt:Key"] ?? "default_secret_key");
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public record AuthResult(bool Success, string Message, string? Token = null);
