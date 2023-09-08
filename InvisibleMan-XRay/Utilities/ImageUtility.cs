using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace InvisibleManXRay.Utilities;

public static class ImageUtility
{
    public static Bitmap ToBitmap(this BitmapImage bitmapImage)
    {
        BitmapSource bitmapSource = bitmapImage as BitmapSource;
        // Convert BitmapSource to Bitmap
        Bitmap bitmap = new Bitmap(bitmapSource.PixelWidth, bitmapSource.PixelHeight, PixelFormat.Format32bppPArgb);
        BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(System.Drawing.Point.Empty, bitmap.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
        bitmapSource.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
        bitmap.UnlockBits(data);
        return bitmap;
    }

    public static Icon ToIcon(this Bitmap bitmap)
    {
        return Icon.FromHandle(bitmap.GetHicon());
    }

    public static Icon ToIcon(this BitmapImage bitmapImage)
    {
        // Convert BitmapSource to Bitmap
        BitmapEncoder encoder = new BmpBitmapEncoder(); // You can choose a different encoder based on your needs
        encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
        using MemoryStream memoryStream = new MemoryStream();
        encoder.Save(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);
        // Now you have the image data in the memory stream
        // You can do further operations with it
        return new Icon(memoryStream);


        return Icon.FromHandle(bitmapImage.ToBitmap().GetHicon());
    }
}
