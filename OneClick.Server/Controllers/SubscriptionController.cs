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
public class SubscriptionController : ControllerBase
{
    private readonly AppDbContext _context;

    public SubscriptionController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("me")]
    public async Task<ActionResult<SubscriptionStatusDto>> GetMySubscription()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
        {
            return Unauthorized();
        }

        // Fetch user info
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return Unauthorized();

        // Get active subscriptions
        var activeSubs = await _context.Subscriptions
            .Include(s => s.Category)
            .Where(s => s.UserId == userId && s.IsActive && s.ExpiryDate > DateTime.UtcNow)
            .OrderByDescending(s => s.ExpiryDate)
            .ToListAsync();

        var dto = new SubscriptionStatusDto
        {
            Username = user.Username,
            JoinDate = user.CreatedAt
        };

        if (!activeSubs.Any())
        {
            dto.HasActiveSubscription = false;
            dto.PlanName = "무료 이용자";
            return Ok(dto);
        }

        // Check for All Pass first
        var allPass = activeSubs.FirstOrDefault(s => s.CategoryId == null);
        if (allPass != null)
        {
            dto.HasActiveSubscription = true;
            dto.PlanName = "💎 올인원 패스";
            dto.ExpiryDate = allPass.ExpiryDate;
            dto.IsAllPass = true;
            return Ok(dto);
        }

        // Single Pass
        var names = activeSubs.Select(s => s.Category?.CategoryName ?? "단일 카테고리").ToList();
        dto.HasActiveSubscription = true;
        dto.PlanName = string.Join(", ", names) + " 구독 중";
        dto.ExpiryDate = activeSubs.First().ExpiryDate;
        dto.IsAllPass = false;

        return Ok(dto);
    }
}
