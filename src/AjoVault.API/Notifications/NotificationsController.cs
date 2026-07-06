using AjoVault.API.Common;
using AjoVault.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AjoVault.API.Notifications;

public class MarkReadRequest
{
    public List<Guid>? Ids { get; set; }
}

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public class NotificationsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var userId = UserContext.GetUserId(HttpContext);
        var notifications = await db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new
            {
                n.Id, n.Title, n.Message, n.Type, n.IsRead, n.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Success(notifications));
    }

    [HttpPatch("read")]
    public async Task<IActionResult> MarkRead([FromBody] MarkReadRequest? request, CancellationToken ct)
    {
        var userId = UserContext.GetUserId(HttpContext);

        var query = db.Notifications.Where(n => n.UserId == userId && !n.IsRead);
        if (request?.Ids != null && request.Ids.Count > 0)
            query = query.Where(n => request.Ids.Contains(n.Id));

        await query.ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);

        return Ok(ApiResponse<object>.Success(new { }, "Notifications marked as read."));
    }
}
