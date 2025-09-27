using Dicom.Imaging;
using DicomAnnotationsDrawing.Utilities;
using System.Drawing;
using System.Windows.Media.Imaging;

namespace DicomAnnotationsDrawing.Services
{
    public static class DicomLoader
    {
        public static (DicomImage Image, BitmapSource BitmapSource) Load(string dicomPath)
        {
            if (string.IsNullOrWhiteSpace(dicomPath))
            {
                throw new ArgumentException("DICOM path is empty.", nameof(dicomPath));
            }

            var dicomImage = new DicomImage(dicomPath);
            using var bitmap = dicomImage.RenderImage().As<Bitmap>();
            var bitmapSource = BitmapConverter.ToBitmapSource(bitmap);

            return (dicomImage, bitmapSource);
        }
    }
}
