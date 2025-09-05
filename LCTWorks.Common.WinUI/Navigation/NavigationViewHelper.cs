using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCTWorks.Common.WinUI.Navigation;

public static class NavigationViewHelper
{
    private static FrameNavigationService? _navigationService;

    private static NavigationView? _navigationView;

    private static bool _supressSelectionChangedNavigation;

    public static IList<object>? MenuItems => _navigationView?.MenuItems;

    public static FrameNavigationService? NavigationService
    {
        get => _navigationService;
        private set
        {
            if (_navigationService != null)
            {
                _navigationService.Navigated -= NavServiceNavigated;
            }
            _navigationService = value;
            if (_navigationService != null)
            {
                _navigationService.Navigated += NavServiceNavigated;
            }
        }
    }

    public static NavigationView? NavigationView
    {
        get => _navigationView;
        private set
        {
            if (_navigationView != null)
            {
                _navigationView.BackRequested -= OnBackRequested;
                _navigationView.SelectionChanged -= OnSelectionChanged;
            }
            _navigationView = value;
            if (_navigationView != null)
            {
                _navigationView.BackRequested += OnBackRequested;
                _navigationView.SelectionChanged += OnSelectionChanged;
            }
        }
    }

    public static object? SettingsItem => NavigationView?.SettingsItem;

    public static string? SettingsPageNavigationKey
    {
        get;
        set;
    }

    public static void Configure(FrameNavigationService navigationService, NavigationView control, string settingsPageNavigationKey)
    {
        NavigationService = navigationService;
        NavigationView = control;
        SettingsPageNavigationKey = settingsPageNavigationKey;
    }

    public static void Deselect()
    {
        if (NavigationView == null)
        {
            return;
        }
        NavigationView.SelectedItem = null;
    }

    public static NavigationViewItem? GetSelectedItem(Type pageType)
    {
        if (_navigationView != null)
        {
            return GetSelectedItem(_navigationView.MenuItems, pageType) ?? GetSelectedItem(_navigationView.FooterMenuItems, pageType);
        }

        return null;
    }

    public static object? GetSelectedItem()
    {
        if (NavigationView == null)
        {
            return default;
        }
        return NavigationView.SelectedItem;
    }

    public static async void SelectItem(object? item = null)
    {
        //Sometimes there's a racing condition in which the
        //SelectedItem is the one just removed and the page
        //selection doesn't update.
        await Task.Delay(300);

        if (NavigationView != null
            && NavigationView.SelectedItem == null
            && NavigationView.MenuItemsSource is IList list)
        {
            if (list.Count > 0)
            {
                var selectedItem = item ?? list[0];
                NavigationView.SelectedItem = selectedItem;
            }
            else
            {
                NavigationService?.NavigateTo(null);
            }
        }
    }

    private static NavigationViewItem? GetSelectedItem(IEnumerable<object> menuItems, Type pageType)
    {
        foreach (var item in menuItems.OfType<NavigationViewItem>())
        {
            if (IsMenuItemForPageType(item, pageType))
            {
                return item;
            }

            var selectedChild = GetSelectedItem(item.MenuItems, pageType);
            if (selectedChild != null)
            {
                return selectedChild;
            }
        }

        return null;
    }

    private static bool IsMenuItemForPageType(NavigationViewItem menuItem, Type sourcePageType)
    {
        if (menuItem.GetValue(NavigationHelper.NavigateToProperty) is string pageKey)
        {
            return NavigationPageMap.GetPageType(pageKey) == sourcePageType;
        }

        return false;
    }

    private static void NavServiceNavigated(object sender, NavigationEventArgs e)
    {
        if (_supressSelectionChangedNavigation)
        {
            return;
        }
        _supressSelectionChangedNavigation = true;

        if (_navigationView != null && _navigationView.MenuItemsSource is IList list)
        {
            var itemToSelect = list
                .OfType<INavigationItemComparable>()
                .FirstOrDefault(item => item.IsMatch(e.Parameter));
            if (itemToSelect != null)
            {
                _navigationView.SelectedItem = itemToSelect;
            }
        }
        _supressSelectionChangedNavigation = false;
    }

    private static void OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        => NavigationService?.GoBack();

    private static void OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (_supressSelectionChangedNavigation || NavigationService == null)
        {
            return;
        }
        _supressSelectionChangedNavigation = true;
        if (args.IsSettingsSelected)
        {
            //typeof(SettingsViewModel).FullName!
            NavigationService.NavigateTo(SettingsPageNavigationKey);
        }
        else
        {
            var selectedItem = args.SelectedItemContainer as NavigationViewItem;

            if (selectedItem?.GetValue(NavigationHelper.NavigateToProperty) is string pageKey)
            {
                NavigationService.NavigateTo(pageKey, selectedItem.Tag);
                if (args.SelectedItem is INavigationObject vm)
                {
                    vm.OnNavigatedTo(args.SelectedItem);
                }
            }
        }
        _supressSelectionChangedNavigation = false;
    }
}