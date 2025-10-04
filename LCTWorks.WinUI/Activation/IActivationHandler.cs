using System.Threading.Tasks;

namespace LCTWorks.Common.WinUI.Activation;

public interface IActivationHandler
{
    bool CanHandle(object args);

    Task HandleAsync(object args);
}