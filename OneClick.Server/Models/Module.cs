using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OneClick.Server.Models;

public class Module
{
    [Key]
    public int Id { get; set; }

    public int CategoryId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string ModuleName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    // Execution Info
    public string ExecutionType { get; set; } = "ServerAutomation"; // ServerAutomation, LocalExe
    public string ModuleKey { get; set; } = ""; // Key to identify IAutomationModule (e.g., "KakaoBot")
    public string IconClass { get; set; } = "fa-solid fa-robot";

    public bool IsActive { get; set; } = true;

    [Column(TypeName = "jsonb")] // SQLite에서는 nvarchar 등으로 자동변환됨 (Postgres용일 수 있음)
    public string? UiSchema { get; set; } // UI configuration JSON

    [MaxLength(20)]
    public string? Version { get; set; }

    // Navigation Property
    public Category? Category { get; set; }
}
