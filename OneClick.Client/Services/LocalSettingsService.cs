using Microsoft.EntityFrameworkCore;
using OneClick.Client.Data;

namespace OneClick.Client.Services;

public class LocalSettingsService
{
    private readonly ClientDbContext _context;

    public LocalSettingsService(ClientDbContext context)
    {
        _context = context;
        // 앱 시작 시 테이블 생성 (마이그레이션 없이 간단히 처리)
        _context.Database.EnsureCreated();
    }

    // 설정값 가져오기 (없으면 기본값 반환)
    public async Task<string> GetSettingAsync(string moduleKey, string key, string defaultValue = "")
    {
        var setting = await _context.Settings
            .FirstOrDefaultAsync(s => s.ModuleKey == moduleKey && s.Key == key);
            
        return setting?.Value ?? defaultValue;
    }

    // 설정값 저장하기 (있으면 업데이트, 없으면 생성)
    public async Task SaveSettingAsync(string moduleKey, string key, string value)
    {
        var setting = await _context.Settings
            .FirstOrDefaultAsync(s => s.ModuleKey == moduleKey && s.Key == key);

        if (setting == null)
        {
            _context.Settings.Add(new LocalModuleSetting
            {
                ModuleKey = moduleKey,
                Key = key,
                Value = value,
                UpdatedAt = DateTime.Now
            });
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();
    }

    // 특정 모듈의 모든 설정 가져오기
    public async Task<Dictionary<string, string>> GetAllSettingsAsync(string moduleKey)
    {
        var settings = await _context.Settings
            .Where(s => s.ModuleKey == moduleKey)
            .ToDictionaryAsync(s => s.Key, s => s.Value);

        return settings;
    }
}
