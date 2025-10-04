using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCTWorks.Common.WinUI.Services
{
    public class DialogService
    {
        private ContentDialog? _currentContentDialog;
        private bool _suppressEscKey;

        public ElementTheme Theme { get; set; } = ElementTheme.Default;

        public XamlRoot? XamlRoot { get; set; }

        public void HideCurrentContentDialog()
        {
            _suppressEscKey = false;
            _currentContentDialog?.Hide();
        }

        public Task<ContentDialogResult> ShowDialogAsync(ContentDialog dialog, bool supressEscKey = false, XamlRoot? xamlRoot = null)
            => RegisterAndShowAsync(dialog, supressEscKey, xamlRoot);

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

        private async Task<ContentDialogResult> RegisterAndShowAsync(ContentDialog dialog, bool suppressEscKey = false, XamlRoot? xamlRoot = null)
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
    }
}