using LangLearn.Backend.Data;
using LangLearn.Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LangLearn.Backend.Dto;

namespace LangLearn.Backend.Services;

public class AuthService(AppDbContext db, IConfiguration config)
{
    public async Task<AuthResultDto> RegisterAsync(RegisterRequest request)
    {
        if (await db.Users.AnyAsync(u => u.Email == request.Email))
        {
            return new AuthResultDto(false, "Email already in use.");
        }

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        };

        db.Users.Add(user);

        await db.SaveChangesAsync();

        return new AuthResultDto(true, "Registration successful.");
    }

    public async Task<AuthResultDto> LoginAsync(LoginRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return new AuthResultDto(false, "Invalid credentials.");
        }

        var token = GenerateJwtToken(user);

        return new AuthResultDto(true, "Login successful.", token);
    }

    public async Task<AuthResultDto> RefreshTokenAsync(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        try
        {
            var jwtToken = handler.ReadJwtToken(token);
            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return new AuthResultDto(false, "Invalid token.");
            }

            var user = await db.Users.FindAsync(userId);
            if (user == null)
            {
                return new AuthResultDto(false, "User not found.");
            }

            // Generate a new token
            var newToken = GenerateJwtToken(user);
            return new AuthResultDto(true, "Token refreshed successfully.", newToken);
        }
        catch (Exception)
        {
            return new AuthResultDto(false, "Invalid token.");
        }
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
