using LCTWorks.Core.Extensions;
using LCTWorks.WinUI.Extensions;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace LCTWorks.WinUI.Helpers
{
    public static class PickerHelper
    {
        public static async Task<IReadOnlyList<StorageFile>> OpenMultipleFilesAsync(
            IEnumerable<string> fileTypeFilter,
            string? commitButtonText = null,
            PickerViewMode? viewMode = null,
            object? targetWindowHandler = null)
        {
            var picker = new FileOpenPicker();

            picker.FileTypeFilter.AddRange(fileTypeFilter);
            if (!string.IsNullOrWhiteSpace(commitButtonText))
            {
                picker.CommitButtonText = commitButtonText;
            }
            if (viewMode != null)
            {
                picker.ViewMode = viewMode.Value;
            }
            if (targetWindowHandler == null)
            {
                var exApp = Application.Current.AsAppExtended();
                if (exApp != null)
                {
                    targetWindowHandler = exApp.MainWindow;
                }
                else
                {
                    throw new Exception("No targetWindowHandler provided and could not find the main window.");
                }
            }
            SetInteropParameters(targetWindowHandler, picker);
            return await picker.PickMultipleFilesAsync();
        }

        private static void SetInteropParameters(object? targetWindowHandler, object picker)
        {
            if (targetWindowHandler == null)
            {
                var exApp = Application.Current.AsAppExtended();
                if (exApp != null)
                {
                    targetWindowHandler = exApp.MainWindow;
                }
                else
                {
                    throw new Exception("No targetWindowHandler provided and could not find the main window.");
                }
            }
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(targetWindowHandler);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        }
    }
}