using LCTWorks.WinUI.Activation;
using LCTWorks.WinUI.Dialogs;
using LCTWorks.Workshop.Services;
using LCTWorks.Workshop.ViewModels;
using LCTWorks.Workshop.ViewModels.Items;
using LCTWorks.WinUI.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;
using LCTWorks.WinUI;
using LCTWorks.Workshop.Views;

namespace LCTWorks.Workshop;

public partial class App : Application, IAppExtended
{
    public App()
    {
        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host
           .CreateDefaultBuilder()
           .UseContentRoot(AppContext.BaseDirectory)
           .ConfigureServices((context, services) =>
           {
               services
               //Activation
               .AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>()
               //Services:
               .AddSingleton<ActivationService>()
               .AddSingleton<DialogService>()
               .AddSingleton<FrameNavigationService>()
               .AddSingleton<DocsService>()
               //ViewModels:
               .AddSingleton<ShellViewModel>()
               .AddSingleton<SettingsViewModel>()
               .AddSingleton<HomeViewModel>()
               .AddSingleton<AdaptiveImageViewModel>()
               //Views:
               .AddSingleton<ShellPage>()
               .AddSingleton<SettingsPage>()
               ;
           }).Build();

        InitializePageHelper();

        UnhandledException += App_UnhandledException;
        AppDomain.CurrentDomain.UnhandledException += AppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    public static Window MainWindow { get; } = new MainWindow();

    public IHost Host
    {
        get;
    }

    Window IAppExtended.MainWindow => MainWindow;

    public static T? GetService<T>()
        where T : class
    {
        try
        {
            if ((Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
            {
                throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
            }
            return service;
        }
        catch
        {
            return default;
        }
    }

    public void AppDomainUnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            exception.Data["AppExType"] = "AppDomainUnhandledException";
            //Send report here.
        }
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        var shellPage = GetService<ShellPage>();
        var actSvc = GetService<ActivationService>();
        if (actSvc != null)
        {
            await actSvc.ActivateAsync(args, shellPage);
        }
    }

    private static void InitializePageHelper()
    {
        NavigationPageMap.Configure<SettingsViewModel, SettingsPage>();
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        //Send report here.
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs? e)
    {
        if (e?.Exception == null)
        {
            return;
        }
        var flattenedExceptions = e.Exception.Flatten().InnerExceptions;
        foreach (var _ in flattenedExceptions)
        {
            //Send reports here.
        }
        e.SetObserved();
    }
}