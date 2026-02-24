using LCTWorks.WinUI.Gallery.ViewModels.Items;
using Microsoft.UI.Xaml.Controls;
using System;

namespace LCTWorks.WinUI.Gallery.Views.Items;

public sealed partial class AdaptiveImagePage : Page
{
    public AdaptiveImagePage()
    {
        ViewModel = App.GetService<AdaptiveImageViewModel>();
        InitializeComponent();
    }

    public AdaptiveImageViewModel? ViewModel { get; private set; }

    private void Button_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        //img.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/Icons/Home.svg"));
        img.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri("https://image2url.com/r2/default/images/1771954519751-4cbb50d6-32ec-4506-89fc-5a5d94f1c508.png"));
    }
}