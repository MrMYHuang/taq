using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

using Taq.Views;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage;
using System.Collections.Generic;
using Windows.Graphics.Display;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Taq
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 

    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            Windows.ApplicationModel.DataTransfer.DataTransferManager.GetForCurrentView().DataRequested += MainPage_DataRequested;
            frame.Navigate(typeof(Home));
        }

        async void MainPage_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            DataRequest request = args.Request;
            RenderTargetBitmap bitmap = new RenderTargetBitmap();

            DataRequestDeferral deferral = request.GetDeferral();
            await bitmap.RenderAsync(mainPage);
            IBuffer pixelBuffer = await bitmap.GetPixelsAsync();
            byte[] pixels = WindowsRuntimeBufferExtensions.ToArray(pixelBuffer, 0, (int)pixelBuffer.Length);            

            var saveFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("screenshot.png", CreationCollisionOption.ReplaceExisting); ;
            // Encode the image to the selected file on disk 
            using (var fileStream = await saveFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, DisplayInformation.GetForCurrentView().LogicalDpi, DisplayInformation.GetForCurrentView().LogicalDpi,
                pixels);
                await encoder.FlushAsync();
            }

            var storageItems = new List<IStorageItem>();
            storageItems.Add(saveFile);
            // 3. Share it
            //request.Data.SetText("TAQ");
            request.Data.Properties.Title = Windows.ApplicationModel.Package.Current.DisplayName;
            // Facebook app supports SetStorageItems, not SetBitmap.
            request.Data.SetStorageItems(storageItems);
            deferral.Complete();
        }

        private void HamburgerButton_Click(Object sender, RoutedEventArgs e)
        {
            MySplitView.IsPaneOpen = !MySplitView.IsPaneOpen;
        }

        private void aboutButton_Click(Object sender, RoutedEventArgs e)
        {
            frame.Navigate(typeof(About));
        }

        private void homeButton_Click(Object sender, RoutedEventArgs e)
        {
            frame.Navigate(typeof(Home));
        }

        private void verButton_Click(Object sender, TappedRoutedEventArgs e)
        {
            frame.Navigate(typeof(Ver));
        }

        private void mapButton_Click(Object sender, TappedRoutedEventArgs e)
        {
            frame.Navigate(typeof(AqSiteMap));
        }

        private void shareBtn_Click(Object sender, TappedRoutedEventArgs e)
        {
            Windows.ApplicationModel.DataTransfer.DataTransferManager.ShowShareUI();
        }

        private void setButton_Click(Object sender, TappedRoutedEventArgs e)
        {
            frame.Navigate(typeof(Settings));
        }

        private void listButton_Click(Object sender, TappedRoutedEventArgs e)
        {
            frame.Navigate(typeof(AqList));
        }
    }
}
