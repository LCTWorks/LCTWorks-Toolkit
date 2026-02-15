using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using LCTWorks.WinUI.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace LCTWorks.WinUI.Extensions;

public static class UIExtensions
{
    public static T? FindControl<T>(this UIElement parent, string ControlName) where T : FrameworkElement
    {
        if (parent == null)
        {
            return null;
        }

        if (parent.GetType() == typeof(T) && ((T)parent).Name == ControlName)
        {
            return (T)parent;
        }
        T? result = null;
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = (UIElement)VisualTreeHelper.GetChild(parent, i);

            if (FindControl<T>(child, ControlName) != null)
            {
                result = FindControl<T>(child, ControlName);
                break;
            }
        }
        return result;
    }

    public static async Task<BitmapImage?> RenderAsync(this UIElement uiElement)
    {
        if (uiElement == null)
        {
            return null;
        }

        var renderTarget = new RenderTargetBitmap();
        await renderTarget.RenderAsync(uiElement);

        var pixels = await renderTarget.GetPixelsAsync();
        if (pixels.Length == 0)
        {
            return null;
        }

        using var stream = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            (uint)renderTarget.PixelWidth,
            (uint)renderTarget.PixelHeight,
            96, 96,
            pixels.ToArray());

        await encoder.FlushAsync();
        stream.Seek(0);

        var bitmap = new BitmapImage();
        await bitmap.SetSourceAsync(stream);
        return bitmap;
    }

    public static async Task<CachedBitmapImage?> RenderCachedAsync(this UIElement uiElement)
    {
        if (uiElement == null)
        {
            return null;
        }

        var renderTarget = new RenderTargetBitmap();
        await renderTarget.RenderAsync(uiElement);

        var pixels = await renderTarget.GetPixelsAsync();
        if (pixels.Length == 0)
        {
            return null;
        }

        using var stream = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            (uint)renderTarget.PixelWidth,
            (uint)renderTarget.PixelHeight,
            96, 96,
            pixels.ToArray());

        await encoder.FlushAsync();
        stream.Seek(0);

        // Read the encoded PNG bytes before consuming the stream
        var imageBytes = new byte[stream.Size];
        await stream.ReadAsync(imageBytes.AsBuffer(), (uint)stream.Size, InputStreamOptions.None);

        stream.Seek(0);

        var bitmap = new BitmapImage();
        await bitmap.SetSourceAsync(stream);

        return new CachedBitmapImage(bitmap, imageBytes);
    }
}