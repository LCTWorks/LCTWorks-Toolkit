using Microsoft.UI.Xaml.Media.Imaging;

namespace LCTWorks.WinUI.Experimental.Models;

public record class CachedBitmapImage(BitmapImage? BitmapImage, byte[] Content);