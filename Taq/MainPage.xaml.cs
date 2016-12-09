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
using Windows.UI.Core;
using Windows.Devices.Geolocation;
using TaqShared;

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

            // Notice: these lines must be executed before InitializeComponent.
            // Otherwise, some UI controls are not loaded completely after InitializeComponent.
            // Don't put these lines into initAux, because these lines must be executed by UI context. Putting them in initAux may result in deadlock!
            app.vm.SelAqId = 1;
            app.vm.currSite2AqView();

            this.InitializeComponent();
            frame.Navigate(typeof(Home));
            initPeriodicTimer();
            DataTransferManager.GetForCurrentView().DataRequested += MainPage_DataRequested;
            initPos();
        }

        // This auxiliary method is used to wait for app.vm ready,
        // before InitializeComponent. Otherwise, some UIs may throw exceptions with partial
        // initialized app.vm.
        async Task<int> initAux()
        {
            try
            {
                await app.vm.m.downloadDataXml(false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Ignore.
            }
            try
            {
                await app.vm.m.loadAqXml(false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Ignore.
            }
            app.vm.m.convertXDoc2Dict();
            await app.vm.m.loadCurrSite(false).ConfigureAwait(false);
            return 0;
        }

        async void initPos()
        {
            app.vm.locAccStat = await Geolocator.RequestAccessAsync();
            // The first time to set Values["MapAutoPos"] on a device.
            if (app.vm.m.localSettings.Values["MapAutoPos"] == null && app.vm.locAccStat == GeolocationAccessStatus.Allowed)
            {
                app.vm.MapAutoPos = true;
            }
            else
            {
                app.vm.MapAutoPos = false;
            }
        }

        async void MainPage_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            DataRequest request = args.Request;

            DataRequestDeferral deferral = request.GetDeferral();
            RenderTargetBitmap bitmap = new RenderTargetBitmap();
            await bitmap.RenderAsync(mainPage);
            IBuffer pixelBuffer = await bitmap.GetPixelsAsync();
            byte[] pixels = WindowsRuntimeBufferExtensions.ToArray(pixelBuffer, 0, (int)pixelBuffer.Length);            

            var saveFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("screenshot.png", CreationCollisionOption.ReplaceExisting);
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
                await app.vm.m.downloadDataXml();
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
                await app.vm.m.loadAqXml();
            }
            catch (Exception ex)
            {
                // Ignore.
            }

            await updateListView();
#if DEBUG
            app.vm.m.sendNotification("AQI: " + app.vm.m.currSiteStrDict["AQI"], "AQI");
            app.vm.m.sendNotification("PM 2.5即時濃度: " + app.vm.m.currSiteStrDict["PM2.5"], "PM2.5");
#else
            app.vm.m.sendNotifications();
#endif
            await updateLiveTile();
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
                app.vm.m.convertXDoc2Dict();
                var selAqId = aqComboBox.SelectedIndex;
                app.vm.SelAqId = selAqId;
                await app.vm.m.loadCurrSite();
                app.vm.currSite2AqView();
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
                await app.vm.m.loadAqXml();
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
            var selSite = (SiteViewModel)((ComboBox)sender).SelectedItem;
            // sites reloading can trigger this event handler and results in null.
            if (selSite == null)
            {
                return;
            }
            localSettings.Values["subscrSite"] = selSite.siteName;
            app.vm.loadSubscrSiteId();
            await app.vm.m.loadCurrSite(true);
            await updateLiveTile();
            app.vm.currSite2AqView();
        }

        private async Task<int> updateLiveTile()
        {
            var medTile = new MedTile();
            var wideTile = new WideTile();
            this.contentGrid.Children.Add(medTile);
            this.contentGrid.Children.Add(wideTile);
            await app.vm.m.getMedTile(medTile, wideTile);
            this.contentGrid.Children.Remove(medTile);
            this.contentGrid.Children.Remove(wideTile);
            await app.vm.m.updateLiveTile();
            return 0;
        }

        private void aqComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selAqId = ((ComboBox)sender).SelectedIndex;
            if (selAqId == -1)
            {
                return;
            }
            app.vm.SelAqId = selAqId;
        }

        private async void Page_Loaded(Object sender, RoutedEventArgs e)
        {
            await ReloadXdAndUpdateList();
            subscrComboBox.SelectedIndex = app.vm.SubscrSiteId;
        }

        private async void refreshButton_Click(Object sender, RoutedEventArgs e)
        {
            await downloadAndReload();
        }
    }
}
