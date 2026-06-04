using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using TerziTuran.Web.Services;

namespace TerziTuran.Web.ApiControllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class DashboardApiController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> Summary()
        => Ok(DTOs.ApiResponse<object>.Ok(await dashboardService.GetDashboardAsync(null, null, null, null), "Dashboard verileri getirildi."));
}
