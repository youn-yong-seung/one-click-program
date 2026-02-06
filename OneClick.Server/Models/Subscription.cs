using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OneClick.Server.Models;

public class Subscription
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    // If CategoryId is null, it means "All Categories" (Full Subscription)
    public int? CategoryId { get; set; } 

    public DateTime ExpiryDate { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public User? User { get; set; }
    public Category? Category { get; set; }
}
