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
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;

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
            BackgroundExecutionManager.RemoveAccess();
            app = App.Current as App;
            localSettings = ApplicationData.Current.LocalSettings;

            this.InitializeComponent();
            initAux();
            frame.Navigate(typeof(Home));
            initBackTask();
            DataTransferManager.GetForCurrentView().DataRequested += MainPage_DataRequested;
        }

        // This auxiliary method is used to wait for app.vm ready,
        // before InitializeComponent. Otherwise, some UIs may throw exceptions with partial
        // initialized app.vm.
        async Task<int> initAux()
        {
            try
            {
                await app.vm.m.downloadDataXml(5000);
            }
            catch (Exception ex)
            {
                // Ignore.
            }
            try
            {
                await app.vm.m.loadAqXml();
            }
            catch (Exception ex)
            {
                // Ignore.
            }
            app.vm.m.convertXDoc2Dict();
            await app.vm.mainSite2AqView();

            // Notice: these lines must be executed before InitializeComponent.
            // Otherwise, some UI controls are not loaded completely after InitializeComponent.
            // Don't put these lines into initAux, because these lines must be executed by UI context. Putting them in initAux may result in deadlock!
            app.vm.SelAqId = 0;

            // * Must be called after this.InitializeComponent!
            // * Must be called by async, not sync. Otherwise,
            // the app can't pass Windows App Cert Kit!
            await initPos();

            await app.vm.loadSubscrSiteViewModel();
            return 0;
        }

        async Task<int> initPos()
        {
            try
            {
                app.vm.m.locAccStat = await Geolocator.RequestAccessAsync().AsTask().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Ignore.
            }

            // The first time to set Values["AutoPos"] on a device.
            if (app.vm.m.localSettings.Values["AutoPos"] == null)
            {
                if (app.vm.m.locAccStat == GeolocationAccessStatus.Allowed)
                {
                    app.vm.AutoPos = true;
                }
                else
                {
                    app.vm.AutoPos = false;
                }
            }
            return 0;
        }

        async Task<int> initBackTask()
        {
            if (localSettings.Values["BgUpdatePeriod"] == null)
            {
                app.vm.BgUpdatePeriodId = 0;
            }
            else
            {
                app.vm.BgUpdatePeriodId = app.vm.bgUpdatePeriods.FindIndex(x => x == (int)localSettings.Values["BgUpdatePeriod"]);
            }
            if (localSettings.Values["BgMainSiteAutoPos"] == null)
            {
                localSettings.Values["BgMainSiteAutoPos"] = true;
            }
            await backTaskReg();
            return 0;
        }

        public async Task<int> backTaskReg()
        {
            var btr = await app.vm.RegisterBackgroundTask("TaqBackTask", "TaqBackTask.TaqBackTask", new TimeTrigger(Convert.ToUInt32(localSettings.Values["BgUpdatePeriod"]), false));
            btr.Completed += (s, e) =>
            {
                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    statusTextBlock.Text = DateTime.Now.ToString("HH:mm:ss tt") + "更新";
                    ReloadXmlAndSitesData();
                });
            };
            return 0;
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

            await updateSitesData();
            /*#if DEBUG
            #else*/
            app.vm.m.sendSubscrSitesNotifications();
            //#endif
            await app.vm.backTaskUpdateTiles();
            return 0;
        }

        private async Task<int> ReloadXmlAndSitesData()
        {
            try
            {
                await app.vm.m.loadAqXml();
                await updateSitesData();
            }
            catch (Exception ex)
            {
                statusTextBlock.Text = "自動更新失敗。請嘗試手動更新。";
            }
            return 0;
        }

        public async Task<int> updateSitesData()
        {
            try
            {
                app.vm.m.convertXDoc2Dict();
                app.vm.loadMainSiteId();
                await app.vm.mainSite2AqView();
            }
            catch (Exception ex)
            {
                statusTextBlock.Text = "更新失敗，請試手動更新。";
            }
            return 0;
        }

        private async void Page_Loaded(Object sender, RoutedEventArgs e)
        {
            //await ReloadXmlAndSitesData();
        }

        private async void refreshButton_Click(Object sender, RoutedEventArgs e)
        {
            await downloadAndReload();
        }

        // Used by AqList and AqSiteMap.
        public void aqComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selAqId = ((ComboBox)sender).SelectedIndex;
            if (selAqId == -1)
            {
                return;
            }
            app.vm.SelAqId = selAqId;
        }

        // Trivial codes
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
