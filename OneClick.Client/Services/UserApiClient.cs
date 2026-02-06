using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace OneClick.Client.Services;

public class UserApiClient
{
    private readonly HttpClient _http;

    public UserApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<SubscriptionStatusDto> GetSubscriptionStatusAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<SubscriptionStatusDto>("api/subscription/me");
            return result ?? new SubscriptionStatusDto();
        }
        catch (Exception ex)
        {
            return new SubscriptionStatusDto { PlanName = $"오류: {ex.Message}", HasActiveSubscription = false };
        }
    }
}

public class SubscriptionStatusDto
{
    public bool HasActiveSubscription { get; set; }
    public string PlanName { get; set; } = "무료 이용자";
    public DateTime? ExpiryDate { get; set; }
    public bool IsAllPass { get; set; }
    public string Username { get; set; } = "사용자";
    public DateTime JoinDate { get; set; }
}
