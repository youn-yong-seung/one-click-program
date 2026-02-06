using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OneClick.Server.Services.Automation;

namespace OneClick.Server.Services;

public class AutomationService
{
    private readonly IEnumerable<IAutomationModule> _modules;
    private readonly ILogger<AutomationService> _logger;

    public AutomationService(IEnumerable<IAutomationModule> modules, ILogger<AutomationService> logger)
    {
        _modules = modules;
        _logger = logger;
    }

    public async Task<string> RunModuleAsync(string moduleName, string? parameters = null)
    {
        var module = _modules.FirstOrDefault(m => m.ModuleName == moduleName);
        
        if (module == null)
        {
            _logger.LogWarning($"[Automation] 알 수 없는 모듈 요청: {moduleName}");
            throw new ArgumentException($"지원하지 않는 모듈입니다: {moduleName}");
        }

        _logger.LogInformation($"[Automation] 모듈 실행 위임: {moduleName}");
        return await module.ExecuteAsync(parameters);
    }
}
