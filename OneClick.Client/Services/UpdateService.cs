using Velopack;
using Velopack.Sources;

namespace OneClick.Client.Services;

/// <summary>
/// Service for managing application updates via Velopack and GitHub Releases
/// </summary>
public class UpdateService
{
    private readonly UpdateManager _updateManager;
    private const string GitHubRepoUrl = "https://github.com/youn-yong-seung/one-click-program";

    public UpdateService()
    {
        // Initialize UpdateManager with GitHub as the update source
        _updateManager = new UpdateManager(
            new GithubSource(GitHubRepoUrl, null, false)
        );
    }

    /// <summary>
    /// Check if a new version is available on GitHub Releases
    /// </summary>
    /// <returns>UpdateInfo if update available, null otherwise</returns>
    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        try
        {
            var updateInfo = await _updateManager.CheckForUpdatesAsync();
            return updateInfo;
        }
        catch (Exception ex)
        {
            // Log the error (in production, use proper logging)
            Console.WriteLine($"Update check failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Download the update package in the background
    /// </summary>
    /// <param name="updateInfo">Update information from CheckForUpdatesAsync</param>
    public async Task DownloadUpdatesAsync(UpdateInfo updateInfo)
    {
        try
        {
            await _updateManager.DownloadUpdatesAsync(updateInfo);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Update download failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Apply the downloaded update and restart the application
    /// </summary>
    /// <param name="updateInfo">Update information from CheckForUpdatesAsync</param>
    public void ApplyUpdatesAndRestart(UpdateInfo updateInfo)
    {
        try
        {
            _updateManager.ApplyUpdatesAndRestart(updateInfo);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Update application failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Get the current application version
    /// </summary>
    /// <returns>Version string (e.g., "1.0.0")</returns>
    public string GetCurrentVersion()
    {
        return _updateManager.CurrentVersion?.ToString() ?? "Unknown";
    }

    /// <summary>
    /// Check if an update is currently pending (downloaded but not applied)
    /// </summary>
    public bool IsUpdatePendingRestart()
    {
        return _updateManager.IsUpdatePendingRestart;
    }
}
