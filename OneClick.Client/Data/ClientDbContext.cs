using System;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace OneClick.Client.Data;

// 1. 모델 정의
public class LocalModuleSetting
{
    public int Id { get; set; }
    
    public string ModuleKey { get; set; } = string.Empty; // e.g., "KakaoBot"
    public string Key { get; set; } = string.Empty;       // e.g., "Delay"
    public string Value { get; set; } = string.Empty;     // e.g., "500"
    
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

// 2. DbContext 정의
public class ClientDbContext : DbContext
{
    public DbSet<LocalModuleSetting> Settings { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // 로컬 AppData 폴더에 DB 파일 저장 (사용자별 격리)
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        var dbPath = Path.Join(path, "oneclick_client.db");

        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }
}
