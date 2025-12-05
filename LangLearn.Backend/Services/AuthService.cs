using LangLearn.Backend.Data;
using LangLearn.Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LangLearn.Backend.Dto;
using System.Globalization;

namespace LangLearn.Backend.Services;

public class AuthService(AppDbContext db, IConfiguration config)
{
    private const int TokenExpirationHours = 12;

    public async Task<AuthResultDto> RegisterAsync(RegisterRequestDto requestDto)
    {
        if (await db.Users.AnyAsync(u => u.Email == requestDto.Email))
        {
            return new AuthResultDto(false, "Email already in use.");
        }

        var user = new User
        {
            Email = requestDto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(requestDto.Password),
        };

        db.Users.Add(user);

        await db.SaveChangesAsync();

        return new AuthResultDto(true, "Registration successful.");
    }

    public async Task<AuthResultDto> LoginAsync(LoginRequestDto requestDto)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == requestDto.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(requestDto.Password, user.PasswordHash))
        {
            return new AuthResultDto(false, "Invalid credentials.");
        }

        var (token, expiresAt) = GenerateJwtToken(user);

        return new AuthResultDto(true, "Login successful.", token, expiresAt);
    }

    // RefreshTokenAsync validates the token signature (allows expired tokens) and
    // issues a new token. Note for frontend: the refresh endpoint expects the
    // expired token in the request body; do NOT rely on sending an Authorization
    // header (expired tokens cannot authenticate).
    public async Task<AuthResultDto> RefreshTokenAsync(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(config["Jwt:Key"] ?? "default_secret_key");

        try
        {
            // Validate token signature and structure
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false, // Allow expired tokens for refresh
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };

            var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);

            // Ensure it's a JWT token with correct algorithm
            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return new AuthResultDto(false, "Invalid token.");
            }

            var userIdClaim = principal.Claims.FirstOrDefault(c =>
                c.Type == JwtRegisteredClaimNames.Sub || c.Type == ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return new AuthResultDto(false, "Invalid token.");
            }

            var user = await db.Users.FindAsync(userId);
            if (user == null)
            {
                return new AuthResultDto(false, "User not found.");
            }

            var (newToken, expiresAt) = GenerateJwtToken(user);
            return new AuthResultDto(true, "Token refreshed successfully.", newToken, expiresAt);
        }
        catch (SecurityTokenException)
        {
            return new AuthResultDto(false, "Invalid token.");
        }
        catch (Exception)
        {
            return new AuthResultDto(false, "Invalid token.");
        }
    }

    private (string token, DateTime expiresAt) GenerateJwtToken(User user)
    {
        var expires = DateTime.UtcNow.AddHours(TokenExpirationHours);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var key = Encoding.UTF8.GetBytes(config["Jwt:Key"] ?? "default_secret_key");
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}