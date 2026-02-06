using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneClick.Server.Data;
using OneClick.Server.Models;
using System.Security.Claims;

namespace OneClick.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CouponController : ControllerBase
{
    private readonly AppDbContext _context;

    public CouponController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("redeem")]
    public async Task<IActionResult> RedeemCoupon([FromBody] string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest("쿠폰 코드를 입력해주세요.");

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdStr == null || !int.TryParse(userIdStr, out int userId))
            return Unauthorized();

        code = code.ToUpper().Trim();

        // 1. 쿠폰 조회
        var coupon = await _context.Coupons
            .FirstOrDefaultAsync(c => c.Code == code);

        if (coupon == null)
            return BadRequest("유효하지 않은 쿠폰 코드입니다.");

        // 2. 유효성 검사
        if (coupon.ExpiresAt.HasValue && coupon.ExpiresAt.Value < DateTime.UtcNow)
            return BadRequest("만료된 쿠폰입니다.");

        // (선택) 일회용 쿠폰인데 이미 사용된 경우 처리 로직 필요 시 추가
        // 지금은 "누구나 쓸 수 있는 프로모션 코드"라고 가정하거나, 
        // User-Coupon 매핑 테이블을 만들어 중복 사용을 막아야 함.
        // 여기서는 간단하게 "FREE" 같은 공용 코드라고 가정하고 중복 체크 생략.

        // 3. 구독 생성/연장
        // 이미 해당 카테고리(혹은 전체) 구독 중인지 확인

        if (coupon.Type == CouponType.AllPass)
        {
            // AllPass Logic: CategoryId is NULL
            var existingSub = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.CategoryId == null && s.ExpiryDate > DateTime.UtcNow);
            
            if (existingSub != null)
            {
                // Extend
                existingSub.ExpiryDate = existingSub.ExpiryDate.AddDays(coupon.DurationDays);
            }
            else
            {
                // [Clean Up] 만료된 이전 구독들 비활성화 처리 (정리)
                var expiredSubs = await _context.Subscriptions
                    .Where(s => s.UserId == userId && s.CategoryId == null && s.IsActive)
                    .ToListAsync();
                
                foreach (var sub in expiredSubs) sub.IsActive = false;

                // Create New
                _context.Subscriptions.Add(new Subscription
                {
                    UserId = userId,
                    CategoryId = null, // All Pass
                    ExpiryDate = DateTime.UtcNow.AddDays(coupon.DurationDays),
                    IsActive = true
                });
            }
        }
        else // Single Category
        {
            if (coupon.TargetCategoryId == null)
                return BadRequest("잘못된 쿠폰 데이터입니다 (TargetCategory Missing).");

            var existingSub = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.CategoryId == coupon.TargetCategoryId && s.ExpiryDate > DateTime.UtcNow);

            if (existingSub != null)
            {
                existingSub.ExpiryDate = existingSub.ExpiryDate.AddDays(coupon.DurationDays);
            }
            else
            {
                 // [Clean Up] 동일 카테고리 만료된 구독 정리
                var expiredSubs = await _context.Subscriptions
                    .Where(s => s.UserId == userId && s.CategoryId == coupon.TargetCategoryId && s.IsActive)
                    .ToListAsync();
                foreach (var sub in expiredSubs) sub.IsActive = false;

                _context.Subscriptions.Add(new Subscription
                {
                    UserId = userId,
                    CategoryId = coupon.TargetCategoryId,
                    ExpiryDate = DateTime.UtcNow.AddDays(coupon.DurationDays),
                    IsActive = true
                });
            }
        }

        await _context.SaveChangesAsync();
        return Ok($"쿠폰이 적용되었습니다! ({coupon.DurationDays}일)");
    }
}
