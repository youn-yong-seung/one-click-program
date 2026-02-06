using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
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

        // 1. WPF Blazor Essential Setup
        Services.AddWpfBlazorWebView();
        
        // 2. Developer Tools
#if DEBUG
        Services.AddBlazorWebViewDeveloperTools();
#endif

        // 3. Auth & Token Services
        Services.AddSingleton<TokenService>();
        
        // Update Service (Velopack)
        Services.AddSingleton<UpdateService>();
        
        // Local Storage (SQLite)
        Services.AddDbContext<OneClick.Client.Data.ClientDbContext>();
        Services.AddScoped<OneClick.Client.Services.LocalSettingsService>();


        Services.AddScoped<JwtDelegatingHandler>(); // Need Scoped for NavigationManager injection
        Services.AddScoped<PaymentService>(); 

        // 4. Register ApiClients with JWT Handler attached
        void ConfigureApi(HttpClient client) => client.BaseAddress = new Uri("http://localhost:5000/");

        Services.AddHttpClient<AuthApiClient>(ConfigureApi).AddHttpMessageHandler<JwtDelegatingHandler>();
        Services.AddHttpClient<ModuleApiClient>(ConfigureApi).AddHttpMessageHandler<JwtDelegatingHandler>();
        Services.AddHttpClient<UserApiClient>(ConfigureApi).AddHttpMessageHandler<JwtDelegatingHandler>();
        Services.AddHttpClient<CategoryApiClient>(ConfigureApi).AddHttpMessageHandler<JwtDelegatingHandler>();

        // 5. Default HttpClient for Components (optional, legacy)
        Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("DefaultAPI"));
        Services.AddHttpClient("DefaultAPI", ConfigureApi).AddHttpMessageHandler<JwtDelegatingHandler>();

        // Build Service Provider
        Provider = Services.BuildServiceProvider();
        
        // Register output provider as resource for XAML
        Resources.Add("Services", Provider);
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