using System.Windows;
using ClearFolder.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ClearFolder;

public partial class App
{
    readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                // Services
                services.AddTransient<IDialogService, DialogService>();
                services.AddTransient<IFolderCleaner, FolderCleaner>();
                services.AddTransient<IFolderRepository, FolderRepository>();
                services.AddTransient<IRecycleBinService, RecycleBinService>();
                // ViewModels
                services.AddSingleton<MainWindowViewModel>();
                // Windows
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();
        MainWindow mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        using (_host)
        {
            await _host.StopAsync();
        }

        base.OnExit(e);
    }
}