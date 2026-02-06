using System.IO;
using System.IO.IsolatedStorage;
using System.Text.Json;

namespace OneClick.Client.Services;

public class TokenService
{
    private const string TokenFileName = "auth_tokens.json";
    
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public bool IsJsonObject => !string.IsNullOrEmpty(AccessToken);

    public async Task SaveTokensAsync(string accessToken, string refreshToken)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;

        var tokens = new { AccessToken, RefreshToken };
        var json = JsonSerializer.Serialize(tokens);

        using var isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);
        using var stream = new IsolatedStorageFileStream(TokenFileName, FileMode.Create, isoStore);
        using var writer = new StreamWriter(stream);
        await writer.WriteAsync(json);
    }

    public async Task<bool> LoadTokensAsync()
    {
        try
        {
            using var isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);
            if (!isoStore.FileExists(TokenFileName))
                return false;

            using var stream = new IsolatedStorageFileStream(TokenFileName, FileMode.Open, isoStore);
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            
            var tokens = JsonSerializer.Deserialize<TokenData>(json);
            if (tokens != null)
            {
                AccessToken = tokens.AccessToken;
                RefreshToken = tokens.RefreshToken;
                return true;
            }
        }
        catch 
        {
            // Failed to load
        }
        return false;
    }

    public event Action? LogoutRequested;

    public void ClearTokens()
    {
        AccessToken = null;
        RefreshToken = null;

        try
        {
            using var isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);
            if (isoStore.FileExists(TokenFileName))
            {
                isoStore.DeleteFile(TokenFileName);
            }
        }
        catch { }

        LogoutRequested?.Invoke();
    }

    private class TokenData
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
    }
}
