using LCTWorks.Telemetry;
using LCTWorks.WinUI.Extensions;
using LCTWorks.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LCTWorks.WinUI.Activation;

public class ActivationService(ActivationHandler<LaunchActivatedEventArgs> defaultHandler, IEnumerable<IActivationHandler> activationHandlers)
{
    private readonly IEnumerable<IActivationHandler> _activationHandlers = activationHandlers;
    private readonly ActivationHandler<LaunchActivatedEventArgs> _defaultHandler = defaultHandler;
    private UIElement? _shell = null;

    public async Task ActivateAsync(object activationArgs, UIElement? shellPage)
    {
        await InitializeAsync();

        var mainWindow = Application.Current.GetMainWindow();
        if (mainWindow == null)
        {
            return;
        }
        if (mainWindow.Content == null)
        {
            _shell = shellPage ?? new Frame();
            mainWindow.Content = _shell;
        }

        await HandleActivationAsync(activationArgs);
        mainWindow.Activate();
        await StartupAsync();
    }

    private static async Task InitializeAsync()
    {
        ThemeSelectorHelper.Initialize();
        await Task.CompletedTask;
    }

    private static async Task StartupAsync()
    {
        await ThemeSelectorHelper.SetRequestedThemeAsync();
    }

    private async Task HandleActivationAsync(object activationArgs)
    {
        var activationHandler = _activationHandlers.FirstOrDefault(h => h.CanHandle(activationArgs));

        if (activationHandler != null)
        {
            await activationHandler.HandleAsync(activationArgs);
        }

        if (_defaultHandler.CanHandle(activationArgs))
        {
            await _defaultHandler.HandleAsync(activationArgs);
        }
    }
}