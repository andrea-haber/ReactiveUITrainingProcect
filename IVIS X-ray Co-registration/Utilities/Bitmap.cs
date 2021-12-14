using System.Windows;
using System.Windows.Media.Imaging;
using ReactiveDomain.Util;

namespace IVIS_X_ray_Co_registration.Utilities
{
    public static class Bitmap
    {
        public static void RedrawBitmap(WriteableBitmap bitmap)
        {
            Threading.RunOnUiThread(() => _redrawBitmap(bitmap));
        }

        private static void _redrawBitmap(WriteableBitmap bitmap)
        {
            try
            {
                if (bitmap == null) return;
                bitmap.Lock();
                bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
                bitmap.Unlock();
            }
            catch
            { /*just eat any exceptions*/}
        }
    }
}
