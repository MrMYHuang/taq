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
using Windows.UI.Core;
using Windows.Devices.Geolocation;
using TaqShared.ModelViews;
using Windows.ApplicationModel.Background;
using TaqBackTask;
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
        public ApplicationDataContainer localSettings;

        public MainPage()
        {
            BackgroundExecutionManager.RemoveAccess();
            app = App.Current as App;
            localSettings = ApplicationData.Current.LocalSettings;

            this.InitializeComponent();
            initAux();
            initBackTask();
            DataTransferManager.GetForCurrentView().DataRequested += MainPage_DataRequested;

            SystemNavigationManager.GetForCurrentView().BackRequested += App_BackRequested;
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
           AppViewBackButtonVisibility.Visible;
        }

        // This auxiliary method is used to wait for app.vm ready,
        // before InitializeComponent. Otherwise, some UIs may throw exceptions with partial
        // initialized app.vm.
        async Task<int> initAux()
        {
            await downloadAndReload();

            // * Must be called after this.InitializeComponent!
            // * Must be called by async, not sync. Otherwise,
            // the app can't pass Windows App Cert Kit!
            await initPos();
            frame.Navigate(typeof(Home), app.tappedSiteName);
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
                localSettings.Values["BgMainSiteAutoPos"] = false;
            }
            await BackTaskReg.RegisterBackgroundTask("AfterUpdateBackTask", "TaqBackTask.AfterUpdate", new SystemTrigger(SystemTriggerType.ServicingComplete, false));
            //await BackTaskReg.RegisterBackgroundTask("UserPresentBackTask", "TaqBackTask.UserPresentBackTask", new SystemTrigger(SystemTriggerType.UserPresent, false));
            // Update if user aways.
            //await BackTaskReg.RegisterBackgroundTask("UserAwayBackTask", "TaqBackTask.UserAwayBackTask", new SystemTrigger(SystemTriggerType.UserAway, false));
            await UserPresentTaskReg(Convert.ToUInt32(localSettings.Values["BgUpdatePeriod"]));
            return 0;
        }

        public async Task<int> UserPresentTaskReg(uint timerPeriod)
        {
            // Update by timer.
            var btr = await BackTaskReg.backUpdateReg("Timer", new TimeTrigger(timerPeriod, false));
            btr.Completed += Btr_Completed;
            // Update if the Internet is available.
            //await backUpdateReg("HasNet", new SystemTrigger(SystemTriggerType.InternetAvailable, false));
            return 0;
        }

        private async void Btr_Completed(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                statusTextBlock.Text = DateTime.Now.ToString("HH:mm:ss tt") + "更新完成。";
                await ReloadXmlAndSitesData();
            });
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

        private void App_BackRequested(object sender, Windows.UI.Core.BackRequestedEventArgs e)
        {
            // Navigate back if possible, and if the event has not 
            // already been handled .
            if (frame.CanGoBack && e.Handled == false)
            {
                e.Handled = true;
                frame.GoBack();
            }
        }

        public async Task<int> downloadAndReload()
        {
            try
            {
#if DEBU
                // Do nothing.
#else
                await app.vm.m.loadSubscrSiteXml();
                statusTextBlock.Text = "下載開始...";
                await app.vm.m.downloadAqData();
                statusTextBlock.Text = DateTime.Now.ToString("HH:mm:ss tt") + "下載完成。";
#endif
            }
            catch (Exception ex)
            {
                statusTextBlock.Text = ex.Message;
            }

            try
            {
                await app.vm.m.loadAq2Dict();
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
                await app.vm.m.loadAq2Dict();
            }
            catch (Exception ex)
            {
                statusTextBlock.Text = "自動更新失敗。請嘗試手動更新。";
            }

            try
            {
                await updateSitesData();
            }
            catch (Exception ex)
            {
            }
            return 0;
        }

        // Run after loadAq2Dict.
        public async Task<int> updateSitesData()
        {
            try
            {
                await app.vm.loadSiteAqViews();
                // Force run loadDict2Sites by setting SelAqId to itself.
                app.vm.loadDict2Sites(app.vm.m.aqList[app.vm.SelAqId]);
            }
            catch (Exception ex)
            {
                statusTextBlock.Text = "更新失敗，請試手動更新。";
            }
            return 0;
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
            app.vm.loadDict2Sites(app.vm.m.aqList[app.vm.SelAqId]);
        }

        // Trivial codes
        private void HamburgerButton_Click(object sender, TappedRoutedEventArgs e)
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
            MySplitView.IsPaneOpen = false;
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
            MySplitView.IsPaneOpen = false;
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
            MySplitView.IsPaneOpen = false;
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
            MySplitView.IsPaneOpen = false;
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
            MySplitView.IsPaneOpen = false;
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
            MySplitView.IsPaneOpen = false;
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
            MySplitView.IsPaneOpen = false;
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
            MySplitView.IsPaneOpen = false;
        }

        private void colorMapsButton_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter || e.Key == VirtualKey.Space)
            {
                frame.Navigate(typeof(AqColorMaps));
            }
        }

        private async void refreshButton_Click(object sender, TappedRoutedEventArgs e)
        {
            MySplitView.IsPaneOpen = false;
            await downloadAndReload();
        }

        private async void refreshButton_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            await downloadAndReload();
        }
    }
}
