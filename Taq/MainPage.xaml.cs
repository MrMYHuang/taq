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
using Windows.System;
using System.Threading.Tasks;
using Windows.System.Threading;
using TaqShared;
using Windows.UI.Core;
using TaqShared.Models;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Taq
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 

    public sealed partial class MainPage : Page
    {
        public App app;
        ThreadPoolTimer periodicTimer;
        public ApplicationDataContainer localSettings;

        public MainPage()
        {
            app = App.Current as App;
            localSettings =
       ApplicationData.Current.LocalSettings;            
            try
            {
                initAux().Wait();
            }
            catch(Exception)
            {
                // Ignore.
            }
            app.shared.updateMapIconsAndList("AQI");
            app.shared.Site2Coll();
            this.InitializeComponent();
            //app.shared.updateLiveTile();
            frame.Navigate(typeof(Home));
            initPeriodicTimer();
            Windows.ApplicationModel.DataTransfer.DataTransferManager.GetForCurrentView().DataRequested += MainPage_DataRequested;
        }

        // This auxiliary method is used to wait for app.shared ready,
        // before InitializeComponent. Otherwise, some UIs may throw exceptions with partial
        // initialized app.shared.
        async Task<int> initAux()
        {
            try
            {
                await app.shared.downloadDataXml(false).ConfigureAwait(false);
                await app.shared.reloadXd(false).ConfigureAwait(false);
                app.shared.reloadDataX();
                await app.shared.loadCurrSite(false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Ignore.
            }
            return 0;
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

        private void HamburgerButton_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter || e.Key == VirtualKey.Space)
            {
                MySplitView.IsPaneOpen = !MySplitView.IsPaneOpen;
            }
        }

        private void setButton_Click(Object sender, TappedRoutedEventArgs e)
        {
            frame.Navigate(typeof(Settings));
        }

        private void setButton_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter || e.Key == VirtualKey.Space)
            {
                frame.Navigate(typeof(Settings));
            }
        }

        private void homeButton_Click(Object sender, RoutedEventArgs e)
        {
            frame.Navigate(typeof(Home));
        }

        private void homeButton_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter || e.Key == VirtualKey.Space)
            {
                frame.Navigate(typeof(Home));
            }
        }

        private void listButton_Click(Object sender, TappedRoutedEventArgs e)
        {
            frame.Navigate(typeof(AqList));
        }

        private void listButton_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter || e.Key == VirtualKey.Space)
            {
                frame.Navigate(typeof(AqList));
            }
        }

        private void mapButton_Click(Object sender, TappedRoutedEventArgs e)
        {
            frame.Navigate(typeof(AqSiteMap));
        }

        private void mapButton_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter || e.Key == VirtualKey.Space)
            {
                frame.Navigate(typeof(AqSiteMap));
            }
        }

        private void shareBtn_Click(Object sender, TappedRoutedEventArgs e)
        {
            Windows.ApplicationModel.DataTransfer.DataTransferManager.ShowShareUI();
        }

        private void shareButton_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter || e.Key == VirtualKey.Space)
            {
                Windows.ApplicationModel.DataTransfer.DataTransferManager.ShowShareUI();
            }
        }

        private void verButton_Click(Object sender, TappedRoutedEventArgs e)
        {
            frame.Navigate(typeof(Ver));
        }

        private void verButton_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter || e.Key == VirtualKey.Space)
            {
                frame.Navigate(typeof(Ver));
            }
        }

        private void aboutButton_Click(Object sender, RoutedEventArgs e)
        {
            frame.Navigate(typeof(About));
        }

        private void aboutButton_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if(e.Key == VirtualKey.Enter || e.Key == VirtualKey.Space)
            {
                frame.Navigate(typeof(About));
            }
        }

        public async Task<int> downloadAndReload()
        {
            try
            {
                statusTextBlock.Text = "Download start.";
                await app.shared.downloadDataXml();
                statusTextBlock.Text = "Download finish.";
            }
            catch (DownloadException ex)
            {
                statusTextBlock.Text = "資料庫下載失敗。請檢查網路，再嘗試手動更新。";
            }
            catch (Exception ex)
            {
                statusTextBlock.Text = "錯誤，請嘗試手動更新。";
            }

            try
            {
                await app.shared.reloadXd();
            }
            catch (Exception ex)
            {
                // Ignore.
            }

            await updateListView();
            app.shared.updateLiveTile();
            // Because reloadXd default loads Site.Circle* to "AQI..."
            aqComboBox.SelectedIndex = 0;
            return 0;
        }

        private void initPeriodicTimer()
        {

#if DEBUG
            TimeSpan delay = TimeSpan.FromSeconds(3e3);
#else
            TimeSpan delay = TimeSpan.FromSeconds(60);
#endif
            periodicTimer = ThreadPoolTimer.CreatePeriodicTimer(async (source) =>
            {
                // TODO: Work

                // Update the UI thread by using the UI core dispatcher.
                await Dispatcher.RunAsync(CoreDispatcherPriority.High,
                        async () =>
                        {
                            await ReloadXdAndUpdateList();
                        }
                    );

            }, delay);
        }

        public async Task<int> updateListView()
        {
            try
            {
                app.shared.reloadDataX();
                app.shared.updateMapIconsAndList("AQI");
                await app.shared.loadCurrSite();
                app.shared.Site2Coll();
            }
            catch (Exception ex)
            {
                statusTextBlock.Text = "更新失敗，請試手動更新。";
            }
            return 0;
        }

        private async Task<int> ReloadXdAndUpdateList()
        {
            try
            {
                await app.shared.reloadXd();
                await updateListView();
            }
            catch (Exception ex)
            {
                statusTextBlock.Text = "自動更新失敗。請嘗試手動更新。";
            }
            return 0;
        }

        private async void subscrComboBox_SelectionChanged(Object sender, SelectionChangedEventArgs e)
        {
            var selSite = (Site)((ComboBox)sender).SelectedItem;
            // sites reloading can trigger this event handler and results in null.
            if (selSite == null)
            {
                return;
            }
            localSettings.Values["subscrSite"] = selSite.siteName;
            app.shared.reloadSubscrSiteId();
            await app.shared.loadCurrSite(true);
            app.shared.Site2Coll();
            app.shared.updateLiveTile();
        }

        private void aqComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selAq = (string)((ComboBox)sender).SelectedValue;
            if (selAq == null)
            {
                return;
            }
            app.shared.updateMapIconsAndList(selAq);
        }

        private async void Page_Loaded(Object sender, RoutedEventArgs e)
        {
            await ReloadXdAndUpdateList();
            if (aqComboBox.SelectedIndex == -1)
            {
                aqComboBox.SelectedIndex = 0;
            }
            else
            {
                // Force trigger an update to map icons through bindings.
                var origId = aqComboBox.SelectedIndex;
                aqComboBox.SelectedIndex = -1;
                aqComboBox.SelectedIndex = origId;
            }
            subscrComboBox.SelectedIndex = app.shared.SubscrSiteId;
        }

        private async void refreshButton_Click(Object sender, RoutedEventArgs e)
        {
            await downloadAndReload();
        }
    }
}
