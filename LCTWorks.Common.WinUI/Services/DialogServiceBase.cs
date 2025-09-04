using LCTWorks.Common.WinUI.Abstractions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace LCTWorks.Common.WinUI.Services;

public class DialogServiceBase
{
    private ContentDialog? _currentContentDialog;
    private bool _suppressEscKey;
    private XamlRoot? _xamlRoot;

    public ElementTheme Theme
    {
        get;
        set;
    }

    public XamlRoot? XamlRoot
    {
        get => _xamlRoot ?? (Application.Current as IAppExtended)?.MainWindow?.Content?.XamlRoot;
        set => _xamlRoot = value;
    }

    public void HideCurrentContentDialog()
    {
        _suppressEscKey = false;
        _currentContentDialog?.Hide();
    }

    public Task<ContentDialogResult> ShowDialogAsync(ContentDialog dialog, bool supressEscKey = false, XamlRoot? xamlRoot = null)
        => RegisterAndShowAsync(dialog, supressEscKey, xamlRoot);

    protected async Task<ContentDialogResult> RegisterAndShowAsync(ContentDialog dialog, bool suppressEscKey = false, XamlRoot? xamlRoot = null)
    {
        HideCurrentContentDialog();

        var xamlRootToUse = xamlRoot ?? XamlRoot;
        if (xamlRootToUse == null)
        {
            return ContentDialogResult.None;
        }

        _suppressEscKey = suppressEscKey;
        _currentContentDialog = dialog;
        _currentContentDialog.XamlRoot = xamlRootToUse;
        _currentContentDialog.Closing += OnCurrentContentDialogClosing;
        _currentContentDialog.RequestedTheme = Theme;

        return await _currentContentDialog.ShowAsync();
    }

    private void OnCurrentContentDialogClosing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        if (args.Result == ContentDialogResult.None && _suppressEscKey)
        {
            args.Cancel = true;
        }
        else
        {
            sender.Closing -= OnCurrentContentDialogClosing;
            _currentContentDialog = null;
            _suppressEscKey = false;
        }
    }
}