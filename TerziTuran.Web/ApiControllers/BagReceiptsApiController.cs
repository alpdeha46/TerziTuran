using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TerziTuran.Web.Data;
using TerziTuran.Web.DTOs;
using TerziTuran.Web.Services;
using TerziTuran.Web.ViewModels;

namespace TerziTuran.Web.ApiControllers;

[ApiController]
[Route("api/bag-receipts")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "StaffOrAdmin")]
public class BagReceiptsApiController(AppDbContext context, IBagReceiptService bagReceiptService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(ApiResponse<object>.Ok(await bagReceiptService.GetRecentAsync(), "Teslim fisleri getirildi."));

    [HttpPost]
    public async Task<IActionResult> Create(BagReceiptCreateViewModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ApiResponse<object>.Fail("Gecersiz teslim fisi bilgileri.", ModelState));

        try
        {
            var receipt = await bagReceiptService.CreateAsync(model.OrderId, model.BagCount, model.Note);
            return Ok(ApiResponse<object>.Ok(receipt, "Teslim fisi siparise atandi."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPut("{id:int}/toggle-delivered")]
    public async Task<IActionResult> ToggleDelivered(int id)
    {
        var receipt = await context.BagReceipts.FindAsync(id);
        if (receipt is null) return NotFound(ApiResponse<object>.Fail("Teslim fisi bulunamadi."));

        receipt.IsDelivered = !receipt.IsDelivered;
        receipt.DeliveredAt = receipt.IsDelivered ? DateTime.Now : null;
        await context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(receipt, "Teslim fisi durumu guncellendi."));
    }
}
