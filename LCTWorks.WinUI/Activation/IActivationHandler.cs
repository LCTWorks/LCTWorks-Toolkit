using System.Threading.Tasks;

namespace LCTWorks.WinUI.Activation;

public interface IActivationHandler
{
    bool CanHandle(object args);

    Task HandleAsync(object args);
}