using System;
using System.Runtime.InteropServices;
using LCTWorks.WinUI.Extensions;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace LCTWorks.WinUI.Helpers;

public static class TitleBarHelper
{
    private const int WAACTIVE = 0x01;
    private const int WAINACTIVE = 0x00;
    private const int WMACTIVATE = 0x0006;

    public static UIElement? AppTitleBar
    {
        get; private set;
    }

    public static void ApplySystemThemeToCaptionButtons()
    {
        var res = Application.Current.Resources;

        var frame = AppTitleBar as FrameworkElement;
        if (frame != null)
        {
            if (frame.ActualTheme == ElementTheme.Dark)
            {
                res["WindowCaptionForeground"] = Colors.White;
            }
            else
            {
                res["WindowCaptionForeground"] = Colors.Black;
            }

            UpdateTitleBar(frame.ActualTheme);
        }
    }

    public static void Extend(
        UIElement appTitleBar,
        TextBlock appTitleBarText,
        string appDisplayName,
        IAppExtended? app = null)
    {
        app ??= Application.Current.AsAppExtended();
        if (app == null)
        {
            return;
        }

        app.MainWindow.ExtendsContentIntoTitleBar = true;
        app.MainWindow.SetTitleBar(appTitleBar);
        app.MainWindow.Activated += (object sender, WindowActivatedEventArgs args) =>
        {
            var resource = args.WindowActivationState == WindowActivationState.Deactivated ? "WindowCaptionForegroundDisabled" : "WindowCaptionForeground";

            appTitleBarText.Foreground = (SolidColorBrush)Application.Current.Resources[resource];
            AppTitleBar = appTitleBarText;
        };
        appTitleBarText.Text = appDisplayName;
    }

    public static void UpdateTitleBar(ElementTheme theme, IAppExtended? app = null)
    {
        app ??= Application.Current.AsAppExtended();
        if (app == null)
        {
            return;
        }
        if (app.MainWindow.ExtendsContentIntoTitleBar)
        {
            if (theme == ElementTheme.Default)
            {
                var uiSettings = new UISettings();
                var background = uiSettings.GetColorValue(UIColorType.Background);

                theme = background == Colors.White ? ElementTheme.Light : ElementTheme.Dark;
            }

            if (theme == ElementTheme.Default)
            {
                theme = Application.Current.RequestedTheme == ApplicationTheme.Light ? ElementTheme.Light : ElementTheme.Dark;
            }

            var foreground = theme switch
            {
                ElementTheme.Dark => new SolidColorBrush(Colors.White),
                ElementTheme.Light => new SolidColorBrush(Colors.Black),
                _ => new SolidColorBrush(Colors.White)
            };
            var bgPointedOver = theme switch
            {
                ElementTheme.Dark => new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)),
                ElementTheme.Light => new SolidColorBrush(Color.FromArgb(0x33, 0x00, 0x00, 0x00)),
                _ => new SolidColorBrush(Colors.Transparent)
            };
            var bgPressed = theme switch
            {
                ElementTheme.Dark => new SolidColorBrush(Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF)),
                ElementTheme.Light => new SolidColorBrush(Color.FromArgb(0x66, 0x00, 0x00, 0x00)),
                _ => new SolidColorBrush(Colors.Transparent)
            };

            Application.Current.Resources["WindowCaptionForeground"] = foreground;

            Application.Current.Resources["WindowCaptionForegroundDisabled"] = theme switch
            {
                ElementTheme.Dark => new SolidColorBrush(Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF)),
                ElementTheme.Light => new SolidColorBrush(Color.FromArgb(0x66, 0x00, 0x00, 0x00)),
                _ => new SolidColorBrush(Colors.Transparent)
            };

            Application.Current.Resources["WindowCaptionButtonBackgroundPointerOver"] = bgPointedOver;

            Application.Current.Resources["WindowCaptionButtonBackgroundPressed"] = bgPressed;

            Application.Current.Resources["WindowCaptionButtonStrokePointerOver"] = theme switch
            {
                ElementTheme.Dark => new SolidColorBrush(Colors.White),
                ElementTheme.Light => new SolidColorBrush(Colors.Black),
                _ => new SolidColorBrush(Colors.Transparent)
            };

            Application.Current.Resources["WindowCaptionButtonStrokePressed"] = theme switch
            {
                ElementTheme.Dark => new SolidColorBrush(Colors.White),
                ElementTheme.Light => new SolidColorBrush(Colors.Black),
                _ => new SolidColorBrush(Colors.Transparent)
            };

            Application.Current.Resources["WindowCaptionBackground"] = new SolidColorBrush(Colors.Transparent);
            Application.Current.Resources["WindowCaptionBackgroundDisabled"] = new SolidColorBrush(Colors.Transparent);

            var titleBar = app.MainWindow.AppWindow?.TitleBar;
            if (titleBar != null)
            {
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonForegroundColor = foreground.Color;
                titleBar.ButtonHoverForegroundColor = foreground.Color;
                titleBar.ButtonHoverBackgroundColor = bgPointedOver.Color;
                titleBar.ButtonPressedBackgroundColor = bgPressed.Color;
            }

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(app.MainWindow);
            if (hwnd == GetActiveWindow())
            {
                SendMessage(hwnd, WMACTIVATE, WAINACTIVE, IntPtr.Zero);
                SendMessage(hwnd, WMACTIVATE, WAACTIVE, IntPtr.Zero);
            }
            else
            {
                SendMessage(hwnd, WMACTIVATE, WAACTIVE, IntPtr.Zero);
                SendMessage(hwnd, WMACTIVATE, WAINACTIVE, IntPtr.Zero);
            }
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, IntPtr lParam);
}