using AjoVault.API.Common;
using AjoVault.API.Dashboard.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AjoVault.API.Dashboard;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public class DashboardController(DashboardService dashboardService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userId = UserContext.GetUserId(HttpContext);
        var result = await dashboardService.GetAsync(userId);
        return Ok(ApiResponse<DashboardResponse>.Success(result));
    }

    [HttpGet("{groupId:guid}")]
    public async Task<IActionResult> GetByGroup(Guid groupId)
    {
        var userId = UserContext.GetUserId(HttpContext);
        var result = await dashboardService.GetByGroupAsync(userId, groupId);
        return Ok(ApiResponse<GroupDashboardResponse>.Success(result));
    }
}
