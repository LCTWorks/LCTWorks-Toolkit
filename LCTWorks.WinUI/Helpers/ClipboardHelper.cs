using Windows.ApplicationModel.DataTransfer;

namespace LCTWorks.WinUI.Helpers;

public static class ClipboardHelper
{
    public static void CopyText(string text, bool flush = true)
    {
        var package = new DataPackage();
        package.SetText(text);
        Clipboard.SetContent(package);
        if (flush)
        {
            Clipboard.Flush();
        }
    }
}