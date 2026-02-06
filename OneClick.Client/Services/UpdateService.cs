using System;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;
using Microsoft.Extensions.Logging;

namespace OneClick.Client.Services
{
    public class UpdateService
    {
        private readonly ILogger<UpdateService> _logger;
        private UpdateManager? _mgr;
        private UpdateInfo? _updateInfo;

        public string CurrentVersion => _mgr?.CurrentVersion?.ToString() ?? "Unknown";

        public UpdateService(ILogger<UpdateService> logger, Microsoft.Extensions.Configuration.IConfiguration config)
        {
            _logger = logger;

            // Read from appsettings.json
            var updateUrl = config["ApiSettings:UpdateUrl"] ?? "http://localhost:5000/";

            try
            {
                // We wrap this in try-catch because if not deployed/packaged, it might throw or behave oddly
                _mgr = new UpdateManager(new SimpleWebSource(updateUrl));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize UpdateManager");
            }
        }

        public bool IsUpdatePending { get; private set; } = false;

        public async Task<string?> CheckForUpdatesAsync()
        {
            if (_mgr == null) return null;
            
            // If we already have an update pending, return that version
            if (IsUpdatePending && _updateInfo != null)
            {
                return _updateInfo.TargetFullRelease.Version.ToString();
            }

            try
            {
                // Check for new version
                _updateInfo = await _mgr.CheckForUpdatesAsync();
                
                if (_updateInfo == null)
                {
                    return null; // No updates available
                }

                return _updateInfo.TargetFullRelease.Version.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for updates");
                return null;
            }
        }

        public async Task DownloadAndRestartAsync()
        {
            if (_mgr == null || _updateInfo == null) return;

            try
            {
                await _mgr.DownloadUpdatesAsync(_updateInfo);
                _mgr.ApplyUpdatesAndRestart(_updateInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying updates");
                throw;
            }
        }

        public async Task DownloadAndPrepareUpdateAsync()
        {
            if (_mgr == null || _updateInfo == null) return;

            try
            {
                await _mgr.DownloadUpdatesAsync(_updateInfo);
                
                // Do NOT restart. Apply when app closes.
                _mgr.WaitExitThenApplyUpdates(_updateInfo);
                
                // Mark as pending so we know to restart on Refresh
                IsUpdatePending = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing updates");
                throw;
            }
        }
        
        public void RestartApp()
        {
            if (_mgr != null && _updateInfo != null)
            {
                _mgr.ApplyUpdatesAndRestart(_updateInfo);
            }
        }
    }
}
