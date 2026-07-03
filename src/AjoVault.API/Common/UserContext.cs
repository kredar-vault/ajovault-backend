namespace AjoVault.API.Common;

public static class UserContext
{
    public static Guid GetUserId(HttpContext context)
    {
        var claim = context.User.FindFirst("userId")?.Value;
        return Guid.TryParse(claim, out var id) ? id : throw new UnauthorizedAccessException("User not found.");
    }
}
