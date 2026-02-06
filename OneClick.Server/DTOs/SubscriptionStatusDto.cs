namespace OneClick.Server.DTOs;

public class SubscriptionStatusDto
{
    public bool HasActiveSubscription { get; set; }
    public string PlanName { get; set; } = "무료 이용자";
    public DateTime? ExpiryDate { get; set; }
    public bool IsAllPass { get; set; }

    // User Profile Info
    public string Username { get; set; } = string.Empty;
    public DateTime JoinDate { get; set; }
}
