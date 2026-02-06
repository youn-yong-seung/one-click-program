using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration; // Important for AddJsonFile
using OneClick.Client.Services;
using OneClick.Client.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.WebView; // For UrlLoadingEventArgs

namespace OneClick.Client;

public partial class MainWindow : Window
{
    // Fix lint warning: Initialize properties or use constructors appropriately
    public static ServiceProvider Provider { get; private set; } = null!;
    public ServiceCollection Services { get; } = new ServiceCollection();

    public MainWindow()
    {
        // Disable GPU to prevent rendering crashes
        Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", "--disable-gpu");
        
        InitializeComponent();

        // 0. Configuration Setup (appsettings.json)
        var exePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        var configPath = System.IO.Path.Combine(exePath!, "appsettings.json");

        var builder = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddJsonFile(configPath, optional: true, reloadOnChange: true);
            
        var config = builder.Build();
        Services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(config);

        // 1. WPF Blazor Essential Setup
        Services.AddWpfBlazorWebView();
        
        // 2. Developer Tools
#if DEBUG
        Services.AddBlazorWebViewDeveloperTools();
#endif
        // 2.5 Logging
        Services.AddLogging();

        // 3. Auth & Token Services
        Services.AddSingleton<TokenService>();
        
        // Local Storage (SQLite)
        Services.AddDbContext<OneClick.Client.Data.ClientDbContext>();
        Services.AddScoped<OneClick.Client.Services.LocalSettingsService>();


        Services.AddScoped<JwtDelegatingHandler>(); // Need Scoped for NavigationManager injection
        Services.AddScoped<PaymentService>(); 

        // 4. Register ApiClients with JWT Handler attached
        // Read from appsettings.json
        var baseUrl = config["ApiSettings:BaseUrl"] ?? "http://localhost:5000/";
        
        void ConfigureApi(HttpClient client) => client.BaseAddress = new Uri(baseUrl);

        Services.AddHttpClient<AuthApiClient>(ConfigureApi).AddHttpMessageHandler<JwtDelegatingHandler>();
        Services.AddHttpClient<ModuleApiClient>(ConfigureApi).AddHttpMessageHandler<JwtDelegatingHandler>();
        Services.AddHttpClient<UserApiClient>(ConfigureApi).AddHttpMessageHandler<JwtDelegatingHandler>();
        Services.AddHttpClient<CategoryApiClient>(ConfigureApi).AddHttpMessageHandler<JwtDelegatingHandler>();

        // 5. Default HttpClient for Components (optional, legacy)
        Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("DefaultAPI"));
        Services.AddHttpClient("DefaultAPI", ConfigureApi).AddHttpMessageHandler<JwtDelegatingHandler>();

        // 6. Update Service (Velopack)
        Services.AddSingleton<UpdateService>();

        // 7. Automation Services
        Services.AddScoped<OneClick.Client.Modules.KakaoSenderStateService>();
        Services.AddScoped<OneClick.Client.Modules.KakaoTargetSenderAutomation>();


        // Build Service Provider
        Provider = Services.BuildServiceProvider();
        
        // Register output provider as resource for XAML
        Resources.Add("Services", Provider);
        
        // Auto-Update Check on Startup & Background
        Loaded += async (s, e) => 
        {
            try 
            {
                var updateService = Provider.GetRequiredService<UpdateService>();
                
                // 1. Check immediately on startup
                await CheckAndApplyUpdate(updateService);

                // 2. Start background timer (every 10 minutes)
                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromMinutes(10);
                timer.Tick += async (sender, args) => await CheckAndPrepareUpdateBackground(updateService);
                timer.Start();
            }
            catch
            {
                // Ignore startup update errors
            }
        };
    }

    // Flag to ensure we only prompt once per session
    private bool _hasUpdatePrompted = false;

    // Startup: Prompt update
    private async Task CheckAndApplyUpdate(UpdateService updateService)
    {
        try
        {
            var newVersion = await updateService.CheckForUpdatesAsync();
            if (newVersion != null)
            {
                // Mark as prompted so we don't ask again in background
                _hasUpdatePrompted = true;

                // Ask user if they want to update
                var result = MessageBox.Show($"새로운 버전({newVersion})이 있습니다. 지금 업데이트하시겠습니까?", 
                                             "업데이트 알림", 
                                             MessageBoxButton.YesNo, 
                                             MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await updateService.DownloadAndRestartAsync();
                }
                // If No, do nothing. Background timer will handle it later (but silent).
            }
        }
        catch { }
    }

    // Background: Silent download then Prompt
    private async Task CheckAndPrepareUpdateBackground(UpdateService updateService)
    {
        try
        {
            var newVersion = await updateService.CheckForUpdatesAsync();
            if (newVersion != null)
            {
                // Background: Download and wait for exit
                await updateService.DownloadAndPrepareUpdateAsync();
                
                // Only notify if we haven't prompted yet in this session
                if (!_hasUpdatePrompted)
                {
                    _hasUpdatePrompted = true;
                    
                    // Notify user and ask to restart
                    // Use Dispatcher to ensure UI thread
                    Dispatcher.Invoke(() => 
                    {
                        var result = MessageBox.Show($"새로운 버전({newVersion}) 업데이트가 준비되었습니다.\n지금 재시작하여 적용하시겠습니까?", 
                                                     "업데이트 알림", 
                                                     MessageBoxButton.YesNo, 
                                                     MessageBoxImage.Question);
                                                     
                        if (result == MessageBoxResult.Yes)
                        {
                            updateService.RestartApp();
                        }
                    });
                }
            }
        }
        catch { }
    }

    private void BlazorWebView_BlazorWebViewInitializing(object sender, Microsoft.AspNetCore.Components.WebView.BlazorWebViewInitializingEventArgs e)
    {
        var blazorWebView = sender as Microsoft.AspNetCore.Components.WebView.Wpf.BlazorWebView;
        if (blazorWebView == null) return;

        blazorWebView.WebView.CoreWebView2InitializationCompleted += (s, args) =>
        {
            if (args.IsSuccess)
            {
                var webView2 = blazorWebView.WebView.CoreWebView2;
                
                // Allow scripts/popups
                webView2.Settings.AreDefaultScriptDialogsEnabled = true;
                webView2.Settings.IsScriptEnabled = true;

                // 1. Handle New Window Requests (Toss Payment Popup)
                // Force them to open in the CURRENT WebView to prevent crashes and ensure "In-App" experience
                webView2.NewWindowRequested += (ws, wargs) =>
                {
                    wargs.Handled = true; // Prevent default popup
                    webView2.Navigate(wargs.Uri); // Navigate current view
                };
            }
        };
    }

    private void BlazorWebView_UrlLoading(object sender, Microsoft.AspNetCore.Components.WebView.UrlLoadingEventArgs e)
    {
        var urlStr = e.Url.ToString();

        // 2. Intercept "Fake" Success/Fail URLs from payment.js
        if (urlStr.StartsWith("http://localhost/payment/"))
        {
            // Cancel the external network request
            e.UrlLoadingStrategy = UrlLoadingStrategy.OpenInWebView; 
            // WAIT! OpenInWebView will try to fetch "http://localhost/..." which fails.
            
            // We need to Redirect to internal app URL.
            // The internal URL for Blazor Hybrid is usually "https://0.0.0.0/"
            var internalUrl = urlStr.Replace("http://localhost/", "https://0.0.0.0/");
            
            var blazorWebView = sender as Microsoft.AspNetCore.Components.WebView.Wpf.BlazorWebView;
            if (blazorWebView != null)
            {
                // We cannot cancel and navigate immediately here easily without infinite loop if strategy is OpenInWebView.
                // If we use OpenInWebView, it will show 404.
                
                // Strategy: Let it fail? No.
                // Strategy: Use 'OpenInWebView' but hook into CoreWebView2.NavigationStarting?
                // Actually, if we just set the URL to the internal one? No, Url property is read-only.
                
                // HACK: Use the dispatcher to navigate slightly later?
                blazorWebView.Dispatcher.InvokeAsync(() => 
                {
                   blazorWebView.WebView.CoreWebView2.Navigate(internalUrl);
                });
            }
            // We let this one pass as "OpenInWebView" (which might start 404), but our explicit Navigate above will override it instantly.
            return;
        }

        // 3. Handle Other External Links
        if (e.Url.Host != "0.0.0.0" && e.Url.Host != "localhost")
        {
            e.UrlLoadingStrategy = UrlLoadingStrategy.OpenInWebView; // Keep external links inside app (as requested)
        }
    }
}