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
    private readonly AppDbContext _db = db;
    private readonly IConfiguration _config = config;

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
        {
            return new AuthResult(false, "Email already in use.");
        }

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        };

        _db.Users.Add(user);

        await _db.SaveChangesAsync();

        return new AuthResult(true, "Registration successful.");
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return new AuthResult(false, "Invalid credentials.");
        }

        var token = GenerateJwtToknen(user);

        return new AuthResult(true, "Login successful.", token);
    }

    private string GenerateJwtToknen(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim(JwtRegisteredClaimNames.Email, user.Email)
        };

        var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "default_secret_key");
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
