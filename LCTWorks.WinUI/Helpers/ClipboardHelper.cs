using Windows.ApplicationModel.DataTransfer;

namespace LCTWorks.Common.WinUI.Helpers;

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