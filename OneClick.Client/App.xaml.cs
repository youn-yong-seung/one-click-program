using System.Configuration;
using System.Data;
using System.Windows;

namespace OneClick.Client;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Velopack: Handle install/uninstall/update events
        // This is the FIRST thing that must run.
        // Velopack: Handle install/uninstall/update events
        // This is the FIRST thing that must run.
        Velopack.VelopackApp.Build()
            .Run();

        base.OnStartup(e);
    }
}

