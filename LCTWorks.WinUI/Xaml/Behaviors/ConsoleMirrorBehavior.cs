using CommunityToolkit.WinUI.Behaviors;
using LCTWorks.Core.Interops;
using LCTWorks.WinUI.Xaml.Behaviors.Internal;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using System;
using System.Threading.Tasks;
using Windows.UI;

namespace LCTWorks.WinUI.Xaml.Behaviors;

public class ConsoleMirrorBehavior : BehaviorBase<RichTextBlock>
{
    public static readonly DependencyProperty AutoScrollProperty =
        DependencyProperty.Register(nameof(AutoScroll), typeof(bool), typeof(ConsoleMirrorBehavior),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ScrollViewerProperty =
                DependencyProperty.Register(nameof(ScrollViewer), typeof(ScrollViewer), typeof(ConsoleMirrorBehavior),
            new PropertyMetadata(default));

    private readonly TextWriterNotifier _notifier;
    private SolidColorBrush _colorBrush;
    private ConsoleColor? _lastConsoleColor;

    public ConsoleMirrorBehavior()
    {
        _notifier = new TextWriterNotifier();
        _notifier.TextWritten += TextWritten;
        _colorBrush = new SolidColorBrush(Colors.White);
        Console.SetOut(_notifier);
        ConsoleInterops.ClearConsoleRequested += OnConsoleClearRequested;
    }

    public bool AutoScroll
    {
        get => (bool)GetValue(AutoScrollProperty);
        set => SetValue(AutoScrollProperty, value);
    }

    public ScrollViewer ScrollViewer
    {
        get => (ScrollViewer)GetValue(ScrollViewerProperty);
        set => SetValue(ScrollViewerProperty, value);
    }

    private static Color ConvertConsoleColorToColor(ConsoleColor consoleColor)
    {
        return consoleColor switch
        {
            ConsoleColor.Black => Colors.Black,
            ConsoleColor.White => Colors.White,
            ConsoleColor.Yellow => Colors.Yellow,
            ConsoleColor.Red => Colors.Red,
            ConsoleColor.DarkBlue => Colors.DarkBlue,
            ConsoleColor.DarkGreen => Colors.DarkGreen,
            ConsoleColor.DarkCyan => Colors.DarkCyan,
            ConsoleColor.DarkRed => Colors.DarkRed,
            ConsoleColor.DarkMagenta => Colors.DarkMagenta,
            ConsoleColor.DarkYellow => Colors.Olive,
            ConsoleColor.Gray => Colors.Gray,
            ConsoleColor.DarkGray => Colors.DarkGray,
            ConsoleColor.Blue => Colors.Blue,
            ConsoleColor.Green => Colors.Green,
            ConsoleColor.Cyan => Colors.Cyan,
            ConsoleColor.Magenta => Colors.Magenta,
            _ => Colors.White,
        };
    }

    private void OnConsoleClearRequested(object? sender, EventArgs e)
    {
        AssociatedObject?.Blocks.Clear();
    }

    private void SetColor()
    {
        var ccolor = ConsoleInterops.ForegroundColor;
        if (ccolor != _lastConsoleColor)
        {
            _lastConsoleColor = ccolor;
            _colorBrush = new SolidColorBrush(ConvertConsoleColorToColor(ccolor));
        }
    }

    private async void TextWritten(object? sender, TextWriterEventArgs e)
    {
        try
        {
            if (AssociatedObject != null)
            {
                SetColor();
                var p = new Paragraph();
                var r = new Run { Text = e.Text, Foreground = _colorBrush };
                p.Inlines.Add(r);
                AssociatedObject.Blocks.Add(p);
                if (AutoScroll)
                {
                    await Task.Delay(100);
                    ScrollViewer?.ChangeView(null, double.MaxValue, null, true);
                }
            }
        }
        catch (Exception)
        {
        }
    }
}