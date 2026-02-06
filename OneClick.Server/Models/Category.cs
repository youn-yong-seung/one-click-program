using System.ComponentModel.DataAnnotations;

namespace OneClick.Server.Models;

public class Category
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    public string? Description { get; set; }
    
    [MaxLength(50)]
    public string IconClass { get; set; } = "fa-solid fa-folder";

    // Navigation Property
    public ICollection<Module> Modules { get; set; } = new List<Module>();
}
