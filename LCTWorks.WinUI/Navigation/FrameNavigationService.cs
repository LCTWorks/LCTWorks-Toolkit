using LCTWorks.WinUI.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Diagnostics.CodeAnalysis;

namespace LCTWorks.WinUI.Navigation;

public class FrameNavigationService
{
    private Frame? _frame;
    private object? _lastParameterUsed;

    public event NavigatedEventHandler? Navigated;

    [MemberNotNullWhen(true, nameof(Frame), nameof(_frame))]
    public bool CanGoBack => Frame != null && Frame.CanGoBack;

    public Frame? Frame
    {
        get
        {
            if (_frame == null)
            {
                _frame = Application.Current.GetContentAsFrame();
                RegisterFrameEvents();
            }

            return _frame;
        }

        set
        {
            UnregisterFrameEvents();
            _frame = value;
            RegisterFrameEvents();
        }
    }

    public bool GoBack()
    {
        if (CanGoBack)
        {
            var navObject = GetFrameContentsNavigationObject();
            _frame.GoBack();
            navObject?.OnNavigatedFrom();

            var newNavObject = GetFrameContentsNavigationObject();
            if (newNavObject != null)
            {
                newNavObject?.OnNavigatedTo(null);
            }

            return true;
        }

        return false;
    }

    public bool NavigateTo(string? pageKey, object? parameter = null, bool clearNavigation = false, bool checkLastParameter = false)
    {
        if (_frame == null)
        {
            return false;
        }
        if (pageKey == null)
        {
            _frame.Content = null;
            return true;
        }
        var pageType = NavigationPageMap.GetPageType(pageKey);

        var shouldNavigate =
           _frame.Content?.GetType() != pageType ||
           !Equals(parameter, _lastParameterUsed) ||
           !checkLastParameter;

        if (!shouldNavigate)
        {
            return false;
        }

        var navigationArgs = new FrameNavigationArgs(clearNavigation, parameter);
        var navObject = GetFrameContentsNavigationObject();
        var navigated = _frame.Navigate(pageType, navigationArgs);
        if (navigated)
        {
            _lastParameterUsed = parameter;
            navObject?.OnNavigatedFrom();
        }

        return navigated;
    }

    protected virtual void OnFrameNavigated(Frame sender, NavigationEventArgs e)
    {
    }

    private INavigationObject? GetFrameContentsNavigationObject()
    {
        if (Frame?.Content is INavigationObjectContainer container)
        {
            return container.NavigationObject;
        }
        return Frame?.Content as INavigationObject;
    }

    private void OnNavigated(object sender, NavigationEventArgs e)
    {
        if (sender is Frame frame)
        {
            var parameter = e.Parameter;
            if (e.Parameter is FrameNavigationArgs args)
            {
                parameter = args.Parameter;
                if (args.ClearNavigation)
                {
                    frame.BackStack.Clear();
                }
            }
            var navObject = GetFrameContentsNavigationObject();
            navObject?.OnNavigatedTo(e.Parameter);

            OnFrameNavigated(frame, e);

            Navigated?.Invoke(sender, e);
        }
    }

    private void RegisterFrameEvents()
    {
        if (_frame != null)
        {
            _frame.Navigated += OnNavigated;
        }
    }

    private void UnregisterFrameEvents()
    {
        if (_frame != null)
        {
            _frame.Navigated -= OnNavigated;
        }
    }
}