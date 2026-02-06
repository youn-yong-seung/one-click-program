using System.Threading.Tasks;

namespace OneClick.Server.Services.Automation;

public interface IAutomationModule
{
    // 모듈을 식별하는 고유 키 (예: "KakaoBot")
    string ModuleName { get; }

    // 실제 실행 로직
    Task<string> ExecuteAsync(string? parameters = null);
}
