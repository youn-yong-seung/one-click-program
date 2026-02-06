using System.Configuration;
using System.Data;
using System.Windows;
using Velopack;

namespace OneClick.Client;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Velopack initialization - handles pending updates
        // This must be called before any other application code
        VelopackApp.Build().Run();
        
        base.OnStartup(e);
    }
}

