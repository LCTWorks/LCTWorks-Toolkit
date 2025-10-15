using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.Xaml.Interactivity;
using System.Windows.Input;

namespace LCTWorks.WinUI.Xaml.Behaviors;

public sealed class TappedBehavior : Behavior<UIElement>
{
    #region DependencyProperties

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(
        nameof(CommandParameter),
        typeof(object),
        typeof(TappedBehavior),
        new PropertyMetadata(default));

    public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
        nameof(Command),
        typeof(ICommand),
        typeof(TappedBehavior),
        new PropertyMetadata(default(ICommand)));

    public static readonly DependencyProperty MarkHandledProperty =
        DependencyProperty.Register(
        nameof(MarkHandled),
        typeof(bool),
        typeof(TappedBehavior),
        new PropertyMetadata(false));

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => (object?)GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public bool MarkHandled
    {
        get => (bool)GetValue(MarkHandledProperty);
        set => SetValue(MarkHandledProperty, value);
    }

    #endregion DependencyProperties

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject != null)
        {
            AssociatedObject.Tapped += HandleTappedEvent;
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (AssociatedObject != null)
        {
            AssociatedObject.Tapped -= HandleTappedEvent;
        }
    }

    private void HandleTappedEvent(object sender, TappedRoutedEventArgs e)
    {
        if (Command is not ICommand command ||
            !command.CanExecute(CommandParameter))
        {
            return;
        }

        command.Execute(CommandParameter);
        if (MarkHandled)
        {
            e.Handled = true;
        }
    }
}