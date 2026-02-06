namespace OneClick.Server.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Login Persistence
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiryTime { get; set; }

    // Relationships
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
