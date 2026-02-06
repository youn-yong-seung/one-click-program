using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace OneClick.Client.Services;

public class CategoryApiClient
{
    private readonly HttpClient _http;

    public CategoryApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<CategoryDto>>("api/category");
            return result ?? new List<CategoryDto>();
        }
        catch
        {
            return new List<CategoryDto>();
        }
    }
}

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fa-solid fa-folder";
    public bool IsSubscribed { get; set; }
    public int ModuleCount { get; set; }
}
