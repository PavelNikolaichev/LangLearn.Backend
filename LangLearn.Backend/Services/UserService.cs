using System.Security.Claims;

namespace LangLearn.Backend.Services;

public class UserService
{
    public static Guid? GetUserId(ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                     user.FindFirst("sub")?.Value;
        
        return Guid.TryParse(userId, out var userGuid) ? userGuid : null;
    }
    
    public static string? GetUserEmail(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value ?? 
               user.FindFirst("email")?.Value;
    }
}
