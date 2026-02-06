using System.ComponentModel.DataAnnotations;

namespace OneClick.Server.Models;

public enum CouponType
{
    AllPass,
    SingleCategory
}

public class Coupon
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty; // e.g., "WELCOME2025"

    public CouponType Type { get; set; } // ALL or SINGLE

    public int? TargetCategoryId { get; set; } // If Single, which category?

    public int DurationDays { get; set; } = 30; // 30 days default

    public bool IsUsed { get; set; } = false; // 일회용 쿠폰 여부 (선택 사항)
    
    // 만약 사용 횟수 제한을 두고 싶다면:
    // public int MaxUsageCount { get; set; }
    // public int CurrentUsageCount { get; set; }

    public DateTime? ExpiresAt { get; set; } // 쿠폰 자체의 유효기간

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
