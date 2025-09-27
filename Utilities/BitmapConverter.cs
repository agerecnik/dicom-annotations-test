using System.Drawing;
using System.Windows.Media.Imaging;
using System.IO;

namespace DicomAnnotationsDrawing.Utilities
{
    public static class BitmapConverter
    {
        public static BitmapSource ToBitmapSource(Bitmap bitmap)
        {
            using MemoryStream memory = new MemoryStream();
            bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
            memory.Position = 0;

            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memory;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();

            return bitmapImage;
        }
    }
}





