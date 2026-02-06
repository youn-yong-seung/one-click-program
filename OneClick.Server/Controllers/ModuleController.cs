using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneClick.Server.Data;
using OneClick.Server.DTOs;
using System.Security.Claims;

namespace OneClick.Server.Controllers;

[Authorize] // 로그인한 사용자만 접근 가능
[ApiController]
[Route("api/[controller]")]
public class ModuleController : ControllerBase
{
    private readonly AppDbContext _context;

    public ModuleController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<ModuleDto>>> GetModules([FromQuery] int? categoryId = null)
    {
        // 1. Get current user ID
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
        {
            return Unauthorized();
        }

        // 2. Refresh active subscriptions
        // We need to know if user has ALL pass (CategoryId == null) or Single pass
        var activeSubs = await _context.Subscriptions
            .Where(s => s.UserId == userId && s.IsActive && s.ExpiryDate > DateTime.UtcNow)
            .ToListAsync();

        bool hasAllPass = activeSubs.Any(s => s.CategoryId == null);
        var allowedCategories = activeSubs
            .Where(s => s.CategoryId != null)
            .Select(s => s.CategoryId!.Value)
            .ToHashSet();

        // 3. Fetch active modules (filtered by category if provided)
        var query = _context.Modules
            .Include(m => m.Category)
            .Where(m => m.IsActive);
            
        if (categoryId.HasValue)
        {
            query = query.Where(m => m.CategoryId == categoryId.Value);
        }

        var modules = await query.ToListAsync();

        // 4. Map to DTO with Lock logic
        var result = modules.Select(m => new ModuleDto
        {
            Id = m.Id,
            Title = m.ModuleName, // Model name is ModuleName
            Description = m.Description,
            CategoryName = m.Category?.CategoryName ?? "",
            IconClass = m.IconClass ?? "",
            ExecutionType = m.ExecutionType,
            TargetPath = m.ModuleKey, 
            UiSchema = !string.IsNullOrEmpty(m.UiSchema) ? m.UiSchema : "{}",
            ModuleKey = m.ModuleKey,
            
            // Lock Logic:
            // Unlocked if: Has All Pass OR Module's Category is in Allowed List
            IsLocked = !(hasAllPass || allowedCategories.Contains(m.CategoryId))
        }).ToList();

        return Ok(result);
    }
}
