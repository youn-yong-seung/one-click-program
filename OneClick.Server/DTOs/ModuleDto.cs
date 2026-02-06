namespace OneClick.Server.DTOs;

public class ModuleDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string IconClass { get; set; } = string.Empty;
    
    // 실행 정보
    public string ExecutionType { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;

    // 권한 상태 (핵심)
    public bool IsLocked { get; set; } = true;
    public string? UiSchema { get; set; }
    public string? ModuleKey { get; set; }
}
