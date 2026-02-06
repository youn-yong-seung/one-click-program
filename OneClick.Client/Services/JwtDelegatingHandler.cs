using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using OneClick.Shared.DTOs;

using Microsoft.AspNetCore.Components;

namespace OneClick.Client.Services;

public class JwtDelegatingHandler : DelegatingHandler
{
    private readonly TokenService _tokenService;

    public JwtDelegatingHandler(TokenService tokenService)
    {
        _tokenService = tokenService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // 0. Ensure tokens are loaded
        if (string.IsNullOrEmpty(_tokenService.AccessToken))
        {
            await _tokenService.LoadTokensAsync();
        }

        // 1. Attach AccessToken if exists
        if (!string.IsNullOrEmpty(_tokenService.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenService.AccessToken);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // 2. If 401 Unauthorized, try to Refresh Token
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(_tokenService.RefreshToken))
        {
            // Avoid infinite loop if refresh endpoint itself returns 401
            if (request.RequestUri?.AbsolutePath.Contains("refresh") == true)
            {
                _tokenService.ClearTokens(); // This triggers event
                return response;
            }

            var newTokens = await RefreshTokensAsync();
            if (newTokens)
            {
                // Retry request with new token
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenService.AccessToken);
                
                // We need to clone the request because it can't be sent twice
                var newRequest = await CloneHttpRequestMessageAsync(request);
                response = await base.SendAsync(newRequest, cancellationToken);
            }
            else
            {
                // Refresh failed -> Logout
                _tokenService.ClearTokens();
            }
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            // 401 but no refresh token
             _tokenService.ClearTokens();
        }

        return response;
    }

    private async Task<bool> RefreshTokensAsync()
    {
        try
        {
            var cleanClient = new HttpClient(); // Use a clean client to bypass handlers
            cleanClient.BaseAddress = new Uri("http://localhost:5000/"); // Hardcoded for simplified logic, should use config

            var refreshRequest = new RefreshTokenRequestDto
            {
                AccessToken = _tokenService.AccessToken ?? "",
                RefreshToken = _tokenService.RefreshToken ?? ""
            };

            var response = await cleanClient.PostAsJsonAsync("api/auth/refresh", refreshRequest);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
                if (result != null)
                {
                    await _tokenService.SaveTokensAsync(result.Token, result.RefreshToken);
                    return true;
                }
            }
        }
        catch
        {
            // Log error
        }
        return false;
    }

    // Helper to clone request
    private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage req)
    {
        var clone = new HttpRequestMessage(req.Method, req.RequestUri);

        // Copy content
        if (req.Content != null)
        {
            var ms = new MemoryStream();
            await req.Content.CopyToAsync(ms);
            ms.Position = 0;
            clone.Content = new StreamContent(ms);

            foreach (var h in req.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(h.Key, h.Value);
        }

        // Copy version
        clone.Version = req.Version;

        // Copy headers
        foreach (var h in req.Headers)
            clone.Headers.TryAddWithoutValidation(h.Key, h.Value);

        return clone;
    }
}
