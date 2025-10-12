using LCTWorks.WinUI.Extensions;
using LCTWorks.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace LCTWorks.WinUI.Dialogs;

public class DialogService
{
    private ContentDialog? _currentContentDialog;
    private bool _suppressEscKey;
    private XamlRoot? _xamlRoot;

    public XamlRoot? XamlRoot
    {
        get => _xamlRoot ?? Application.Current.GetXamlRoot();
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

        var xamlRootToUse = (xamlRoot ?? XamlRoot) ?? throw new Exception("No XamlRoot available to show the dialog.");
        _suppressEscKey = suppressEscKey;
        _currentContentDialog = dialog;
        _currentContentDialog.XamlRoot = xamlRootToUse;
        _currentContentDialog.Closing += OnCurrentContentDialogClosing;
        _currentContentDialog.RequestedTheme = ThemeSelectorHelper.Theme;

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