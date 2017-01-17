using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Taq.Shared.Models;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Taq.Shared.ModelViews
{
    public static class StaticTaqModelView
    {
        public async static Task<StorageFile> saveUi2Png(string fileName, UIElement ui)
        {
            RenderTargetBitmap bitmap = new RenderTargetBitmap();
            await bitmap.RenderAsync(ui);
            IBuffer pixelBuffer = await bitmap.GetPixelsAsync();

            var saveFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            // Encode the image to the selected file on disk 
            using (var fileStream = await saveFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, DisplayInformation.GetForCurrentView().LogicalDpi, DisplayInformation.GetForCurrentView().LogicalDpi,
                pixelBuffer.ToArray());
                await encoder.FlushAsync();
            }
            return saveFile;
        }

        public static Color html2RgbColor(string colorStr)
        {
            var colorStr2 = colorStr.Substring(1);
            var r = (byte)Convert.ToUInt32(colorStr2.Substring(0, 2), 16);
            var g = (byte)Convert.ToUInt32(colorStr2.Substring(2, 2), 16);
            var b = (byte)Convert.ToUInt32(colorStr2.Substring(4, 2), 16);
            return Color.FromArgb(0xff, r, g, b);
        }

        public static SolidColorBrush getTextColor(int aqLevel)
        {
            return aqLevel > StaticTaqModel.aqTextColorLimit ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);
        }
    }
}
