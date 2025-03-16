namespace LCTWorks.Common.Services.Telemetry
{
    public class DisposableTrace(string key, Action<Exception?> disposeAction)
    {
        public Action<Exception?>? DisposeAction { get; private set; } = disposeAction;

        public string Key { get; } = key;

        public void Dispose() => Finish(null);

        public void Finish(Exception? e)
        {
            DisposeAction?.Invoke(e);
            DisposeAction = null;
        }
    }
}