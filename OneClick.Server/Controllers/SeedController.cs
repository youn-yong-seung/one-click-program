using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneClick.Server.Data;
using OneClick.Server.Models;

namespace OneClick.Server.Controllers;

[ApiController]
[Route("api/seed")]
public class SeedController : ControllerBase
{
    private readonly AppDbContext _context;

    public SeedController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> SeedData()
    {
        // 1. SNS 카테고리 확인 및 추가
        var snsCat = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryName == "SNS");
        if (snsCat == null)
        {
            snsCat = new Category { CategoryName = "SNS", Description = "소셜 미디어 자동화 도구" };
            _context.Categories.Add(snsCat);
            await _context.SaveChangesAsync();
        }

        // 2. 모듈 추가
        if (!await _context.Modules.AnyAsync(m => m.ModuleName == "카카오톡 자동 발송기"))
        {
            _context.Modules.Add(new Module
            {
                ModuleName = "카카오톡 자동 발송기", 
                Description = "친구 목록을 기반으로 개인화된 메시지를 자동으로 발송합니다.",
                CategoryId = snsCat.Id,
                ExecutionType = "ServerAutomation",
                ModuleKey = "KakaoBot",
                IconClass = "fa-solid fa-comment",
                IsActive = true
            });
            await _context.SaveChangesAsync();
        }

        return Ok(new { message = "데이터가 성공적으로 추가되었습니다! (SNS & 카카오톡봇)" });
    }

    [HttpGet("check")]
    public async Task<IActionResult> CheckData()
    {
        var cats = await _context.Categories.ToListAsync();
        var mods = await _context.Modules.ToListAsync();

        return Ok(new 
        { 
            CategoryCount = cats.Count, 
            Categories = cats.Select(c => new { c.Id, c.CategoryName }),
            ModuleCount = mods.Count,
            Modules = mods.Select(m => new { m.Id, m.ModuleName, m.CategoryId })
        });
    }
}
