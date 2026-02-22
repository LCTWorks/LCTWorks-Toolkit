using LCTWorks.WinUI.Helpers;
using Microsoft.UI.Xaml;
using System;
using System.IO;
using Windows.UI.ViewManagement;

namespace LCTWorks.WinUI.Gallery;

public sealed partial class MainWindow : Window
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;
    private readonly UISettings _settings;

    public MainWindow()
    {
        InitializeComponent();

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/WindowIcon.ico"));
        Content = null;

        _settings = new UISettings();
        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _settings.ColorValuesChanged += Settings_ColorValuesChanged;
    }

    private void Settings_ColorValuesChanged(UISettings sender, object args)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            TitleBarHelper.ApplySystemThemeToCaptionButtons();
        });
    }
}