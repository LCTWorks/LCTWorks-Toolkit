using LCTWorks.Workshop.ViewModels.Items;
using Microsoft.UI.Xaml.Controls;
using System;

namespace LCTWorks.Workshop.Items;

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
        //img.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri("https://fastly.picsum.photos/id/63/5000/2813.jpg?hmac=HvaeSK6WT-G9bYF_CyB2m1ARQirL8UMnygdU9W6PDvM"));
        //img.Source = new Uri("https://fastly.picsum.photos/id/63/5000/2813.jpg?hmac=HvaeSK6WT-G9bYF_CyB2m1ARQirL8UMnygdU9W6PDvM");
    }
}