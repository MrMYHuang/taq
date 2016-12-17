using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using System.Linq;
using Taq.Views;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using System.Collections.Generic;
using Windows.System;
using System.Threading.Tasks;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.Devices.Geolocation;
using TaqShared.ModelViews;
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
            localSettings = ApplicationData.Current.LocalSettings;
            try
            {
                initAux().Wait();
            }
            catch (Exception)
            {
                // Ignore.
            }

            // Notice: these lines must be executed before InitializeComponent.
            // Otherwise, some UI controls are not loaded completely after InitializeComponent.
            // Don't put these lines into initAux, because these lines must be executed by UI context. Putting them in initAux may result in deadlock!
            app.vm.SelAqId = 1;
//            app.vm.loadDict2Sites(app.vm.m.aqList[0]);
            app.vm.currSite2AqView();

            this.InitializeComponent();
            app.vm.loadSubscrSiteViewModel();
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
                await app.vm.m.downloadDataXml(false, 5000).ConfigureAwait(false);
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
            if (app.vm.m.localSettings.Values["MapAutoPos"] == null)
            {
                if (app.vm.locAccStat == GeolocationAccessStatus.Allowed)
                {
                    app.vm.MapAutoPos = true;
                }
                else
                {
                    app.vm.MapAutoPos = false;
                }
            }
        }

        async void MainPage_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            DataRequest request = args.Request;

            DataRequestDeferral deferral = request.GetDeferral();
            try
            {
                var saveFile = await StaticTaqModelView.saveUi2Png("screenshot.png", mainPage);

                var storageItems = new List<IStorageItem>();
                storageItems.Add(saveFile);
                // 3. Share it
                //request.Data.SetText("TAQ");
                request.Data.Properties.Title = Windows.ApplicationModel.Package.Current.DisplayName;
                // Facebook app supports SetStorageItems, not SetBitmap.
                request.Data.SetStorageItems(storageItems);
            }
            catch (Exception ex)
            {
                // Ignore.
            }
            finally
            {
                deferral.Complete();
            }
        }

        public async Task<int> downloadAndReload()
        {
            try
            {
#if DEBU
                // Do nothing.
#else
                statusTextBlock.Text = "Download start.";
                await app.vm.m.downloadDataXml();
                statusTextBlock.Text = "Download finish.";
#endif
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
            /*#if DEBUG
            #else*/
            app.vm.m.sendSubscrSitesNotifications();
            //#endif
            await app.vm.backTaskUpdateTiles();
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
                //var selAqId = aqComboBox.SelectedIndex;
                //app.vm.SelAqId = selAqId;
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

        public void aqComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
        }

        private async void refreshButton_Click(Object sender, RoutedEventArgs e)
        {
            await downloadAndReload();
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
            DataTransferManager.ShowShareUI();
        }

        private void shareButton_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter || e.Key == VirtualKey.Space)
            {
                DataTransferManager.ShowShareUI();
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
            if (e.Key == VirtualKey.Enter || e.Key == VirtualKey.Space)
            {
                frame.Navigate(typeof(About));
            }
        }

        private void colorMapsButton_Click(object sender, TappedRoutedEventArgs e)
        {
            frame.Navigate(typeof(AqColorMaps));
        }

        private void colorMapsButton_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter || e.Key == VirtualKey.Space)
            {
                frame.Navigate(typeof(AqColorMaps));
            }
        }
    }
}
