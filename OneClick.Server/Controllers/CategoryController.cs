using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneClick.Server.Data;
using OneClick.Server.DTOs;
using System.Security.Claims;

namespace OneClick.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CategoryController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategoryController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
        {
            return Unauthorized();
        }

        // 1. Get User's Active Subscriptions
        var activeSubs = await _context.Subscriptions
            .Where(s => s.UserId == userId && s.IsActive && s.ExpiryDate > DateTime.UtcNow)
            .ToListAsync();

        bool hasAllPass = activeSubs.Any(s => s.CategoryId == null);
        var subCatIds = activeSubs
            .Where(s => s.CategoryId != null)
            .Select(s => s.CategoryId!.Value)
            .ToHashSet();

        // 2. Get All Categories with Module Count
        var categories = await _context.Categories
            .Include(c => c.Modules)
            .ToListAsync();

        // 3. Map to DTO
        var result = categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.CategoryName,
            Description = c.Description!,
            IconClass = c.IconClass,
            ModuleCount = c.Modules.Count,
            IsSubscribed = hasAllPass || subCatIds.Contains(c.Id)
        }).ToList();

        return Ok(result);
    }
}
