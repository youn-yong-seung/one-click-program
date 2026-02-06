using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace OneClick.Client.Services;

public class AuthApiClient
{
    private readonly HttpClient _http;

    public AuthApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<HttpResponseMessage> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        var request = new { CurrentPassword = currentPassword, NewPassword = newPassword };
        return await _http.PostAsJsonAsync("api/auth/change-password", request);
    }

    public async Task<HttpResponseMessage> LoginAsync(object loginModel)
    {
        return await _http.PostAsJsonAsync("api/auth/login", loginModel);
    }

    public async Task<HttpResponseMessage> RegisterAsync(object registerModel)
    {
        return await _http.PostAsJsonAsync("api/auth/register", registerModel);
    }
}
