using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

using Taq.Views;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using System.Collections.Generic;
using Windows.System;
using System.Threading.Tasks;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.Devices.Geolocation;
using Windows.ApplicationModel.Background;
using Microsoft.Advertising.WinRT.UI;
using Windows.UI.Popups;
using Windows.System.Profile;

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

        // AD constants.
        private const int WAD_WIDTH = 160;
        private const int WAD_HEIGHT = 600;
        private const int MAD_WIDTH = 320;
        private const int MAD_HEIGHT = 50;
        private const string WAPPLICATIONID = "aec60690-4a2f-4285-94cf-9995b35e5b84";
        private const string WADUNITID = "11655289";
        private const string MAPPLICATIONID = "1cdf4ecb-9f80-4cde-a32b-4256cc5bfa04";
        private const string MADUNITID = "11655298";

        // This is an error handler for the interstitial ad.
        private void OnErrorOccurred(object sender, AdErrorEventArgs e)
        {
            var md = new MessageDialog($"An error occurred. {e.ErrorCode}: {e.ErrorMessage}");
            md.ShowAsync();
        }        

        // This is an event handler for the ad control. It's invoked when the ad is refreshed.
        private void OnAdRefreshed(object sender, RoutedEventArgs e)
        {
            var md = new MessageDialog($"Advertisement");
            md.ShowAsync();
        }

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

            // AD.
            if ("Windows.Mobile" == AnalyticsInfo.VersionInfo.DeviceFamily)
            {
                ad1.ApplicationId = MAPPLICATIONID;
                ad1.AdUnitId = MADUNITID;
                ad1.Width = MAD_WIDTH;
                ad1.Height = MAD_HEIGHT;
                frame.Margin = new Thickness(0, MAD_HEIGHT+5, 0, 117);
            }
            else
            {
                // Test
                //ad1.ApplicationId = "d25517cb-12d4-4699-8bdc-52040c712cab";
                //ad1.AdUnitId = "10043058";

                ad1.ApplicationId = WAPPLICATIONID;
                ad1.AdUnitId = WADUNITID;
                ad1.Width = WAD_WIDTH;
                ad1.Height = WAD_HEIGHT;
            }
            ad1.IsAutoRefreshEnabled = true;

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
            var saveFile = await app.vm.m.saveUi2Png("screenshot.png", mainPage);

            var storageItems = new List<IStorageItem>();
            storageItems.Add(saveFile);
            // 3. Share it
            //request.Data.SetText("TAQ");
            request.Data.Properties.Title = Windows.ApplicationModel.Package.Current.DisplayName;
            // Facebook app supports SetStorageItems, not SetBitmap.
            request.Data.SetStorageItems(storageItems);
            deferral.Complete();
        }

        public async Task<int> downloadAndReload()
        {
            try
            {
#if DEBUG
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
            app.vm.m.sendNotification("AQI: " + app.vm.m.currSiteStrDict["AQI"], "AQI");
            app.vm.m.sendNotification("PM 2.5即時濃度: " + app.vm.m.currSiteStrDict["PM2.5"], "PM2.5");
#else*/
            app.vm.m.sendNotifications();
//#endif
            await backTaskUpdateTiles();
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
            // Unused???
            if (selSite == null)
            {
                return;
            }
            localSettings.Values["subscrSite"] = selSite.siteName;
            app.vm.loadSubscrSiteId();
            await app.vm.m.loadCurrSite(true);
            await backTaskUpdateTiles();
            app.vm.currSite2AqView();
        }

        // Update live tiles by a background task.
        // Don't directly call app.vm.m.updateLiveTile,
        // which might fail to draw tile images with UI context.
        private async Task<int> backTaskUpdateTiles()
        {
            ApplicationTrigger trigger = new ApplicationTrigger();
            await app.vm.RegisterBackgroundTask("BackTaskUpdateTiles", "TaqBackTask.BackTaskUpdateTiles", trigger);
            var result = await trigger.RequestAsync();
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
            if (e.Key == VirtualKey.Enter || e.Key == VirtualKey.Space)
            {
                frame.Navigate(typeof(About));
            }
        }
    }
}
