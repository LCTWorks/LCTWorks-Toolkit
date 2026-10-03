using System;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.ApplicationModel.DataTransfer;

namespace LCTWorks.WinUI.Helpers;

/// <summary>
/// Wraps <see cref="Clipboard"/> so that transient failures never surface as unhandled exceptions.
/// </summary>
public static class ClipboardHelper
{
    private const int MaxAttempts = 5;
    private const int RetryDelayMs = 20;

    public const int CLIPBRD_E_CANT_OPEN = unchecked((int)0x800401D0);

    /// <summary>
    /// Returns the current clipboard content, or <c>null</c> if it could not be read.
    /// </summary>
    public static DataPackageView? GetContent()
    {
        DataPackageView? view = null;
        TryRun(() => view = Clipboard.GetContent());
        return view;
    }

    /// <summary>
    /// Returns <c>true</c> if the clipboard currently holds text. Never throws.
    /// </summary>
    public static bool HasText()
    {
        try
        {
            var view = GetContent();
            return view != null && view.Contains(StandardDataFormats.Text);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Copies <paramref name="text"/> to the clipboard. Returns <c>false</c> if it could not be set.
    /// </summary>
    public static bool SetText(string? text, bool flush = true)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        var pack = new DataPackage();
        pack.SetText(text);

        return TryRun(() =>
        {
            Clipboard.SetContent(pack);
            if (flush)
            {
                // Flush can fail independently of SetContent (same lock). Content is already
                // on the clipboard at this point; losing the flush only means the data
                // disappears when the app exits, so swallow it.
                try { Clipboard.Flush(); } catch (COMException) { }
            }
        });
    }

    private static bool TryRun(Action action)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                action();
                return true;
            }
            catch (COMException ex) when (ex.HResult == CLIPBRD_E_CANT_OPEN && attempt < MaxAttempts)
            {
                // Another process owns the clipboard right now; back off and try again.
                Thread.Sleep(RetryDelayMs * attempt);
            }
            catch (COMException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}