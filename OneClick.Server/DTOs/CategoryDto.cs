namespace OneClick.Server.DTOs;

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fa-solid fa-folder";
    
    // UI 상태
    public bool IsSubscribed { get; set; } // 내가 구독 중인가?
    public int ModuleCount { get; set; } // 이 안에 모듈이 몇 개 있는가?
}
