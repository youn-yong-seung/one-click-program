using System.Net.Http;
using System.Net.Http.Json;
using OneClick.Shared.DTOs; // We need to move DTO to Shared if possible, but for now we'll define a local DTO or use dynamic.
// Actually, let's just make a local DTO in Client for simplicity, matching the Server one.

namespace OneClick.Client.Services;

public class ModuleApiClient
{
    private readonly HttpClient _http;

    public ModuleApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<ModuleDto>> GetModulesAsync(int? categoryId = null)
    {
        try 
        {
            var url = "api/module";
            if (categoryId.HasValue)
            {
                url += $"?categoryId={categoryId.Value}";
            }

            var result = await _http.GetFromJsonAsync<List<ModuleDto>>(url);
            return result ?? new List<ModuleDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching modules: {ex.Message}");
            return new List<ModuleDto>();
        }
    }

    public async Task<string> RunModuleAsync(int moduleId, object? parameters = null)
    {
        try
        {
            var response = await _http.PostAsJsonAsync($"api/execution/run/{moduleId}", parameters ?? new { });
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ExecutionResult>();
                return result?.Message ?? "실행 완료!";
            }
            return $"오류: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            return $"실행 중 예외 발생: {ex.Message}";
        }
    }

    public class ExecutionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }
}

public class ModuleDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public string IconClass { get; set; } = "";
    public string ExecutionType { get; set; } = "";
    public string TargetPath { get; set; } = "";
    public bool IsLocked { get; set; }
    public string? UiSchema { get; set; } // Added for Dynamic UI
}
