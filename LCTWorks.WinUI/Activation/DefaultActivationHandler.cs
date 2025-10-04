using LCTWorks.WinUI.Navigation;
using Microsoft.UI.Xaml;
using System.Threading.Tasks;

namespace LCTWorks.WinUI.Activation;

public class DefaultActivationHandler(FrameNavigationService navigationService) : ActivationHandler<LaunchActivatedEventArgs>
{
    private readonly FrameNavigationService _navigationService = navigationService;

    protected override bool CanHandleInternal(LaunchActivatedEventArgs args)
    {
        // None of the ActivationHandlers has handled the activation.
        return _navigationService.Frame?.Content == null;
    }

    protected override async Task HandleInternalAsync(LaunchActivatedEventArgs args)
    {
        await Task.CompletedTask;
    }
}