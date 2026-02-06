using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneClick.Server.Services;
using OneClick.Server.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace OneClick.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ExecutionController : ControllerBase
{
    private readonly AutomationService _automation;
    private readonly AppDbContext _context;

    public ExecutionController(AutomationService automation, AppDbContext context)
    {
        _automation = automation;
        _context = context;
    }

    [HttpPost("run/{moduleId}")]
    public async Task<IActionResult> RunModule(int moduleId, [FromBody] System.Text.Json.JsonElement parameters)
    {
        // 1. Check User & Subscription
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

        var module = await _context.Modules.FindAsync(moduleId);
        if (module == null) return NotFound("모듈을 찾을 수 없습니다.");

        // (간단하게) 구독 확인 로직은 생략하거나 ModuleController와 동일하게 구현
        // 여기서는 일단 실행만 집중
        
        try
        {
            // JsonElement.ToString() returns the JSON string representation
            string jsonParams = parameters.ToString();

            // Use ModuleKey directly to find the automation module
            var result = await _automation.RunModuleAsync(module.ModuleKey, jsonParams);
            return Ok(new { message = result, success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"실행 실패: {ex.Message}", success = false });
        }
    }
}
