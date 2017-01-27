using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Taq.Uwp.Views;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using System.Collections.Generic;
using Windows.System;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.Devices.Geolocation;
using Taq.Shared.ModelViews;
using Windows.ApplicationModel.Background;
using Taq.BackTask;
using Windows.ApplicationModel.Core;
using Taq.Shared.Models;
using System.Diagnostics;
using Taq.Shared.Views;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Taq.Uwp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 

    public sealed partial class MainPage : Page
    {
        public App app;

        public MainPage()
        {
            BackgroundExecutionManager.RemoveAccess();
            app = App.Current as App;

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
                Debug.WriteLine(ex.Message);
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
            if (app.vm.m.localSettings.Values["BgUpdatePeriod"] == null)
            {
                app.vm.BgUpdatePeriodId = 0;
            }
            else
            {
                app.vm.BgUpdatePeriodId = app.vm.bgUpdatePeriods.FindIndex(x => x == (int)app.vm.m.localSettings.Values["BgUpdatePeriod"]);
            }
            if (app.vm.m.localSettings.Values["BgMainSiteAutoPos"] == null)
            {
                app.vm.m.localSettings.Values["BgMainSiteAutoPos"] = false;
            }
            await BackTaskReg.RegisterBackgroundTask("AfterUpdateBackTask", "Taq.BackTask.AfterUpdate", new SystemTrigger(SystemTriggerType.ServicingComplete, false));
            //await BackTaskReg.RegisterBackgroundTask("UserPresentBackTask", "Taq.BackTask.UserPresentBackTask", new SystemTrigger(SystemTriggerType.UserPresent, false));
            // Update if user aways.
            //await BackTaskReg.RegisterBackgroundTask("UserAwayBackTask", "Taq.BackTask.UserAwayBackTask", new SystemTrigger(SystemTriggerType.UserAway, false));
            await UserPresentTaskReg(Convert.ToUInt32(app.vm.m.localSettings.Values["BgUpdatePeriod"]));
            return 0;
        }

        public async Task<int> UserPresentTaskReg(uint timerPeriod)
        {
            // Update by timer.
            var btr = await BackTaskReg.backUpdateReg("Timer", new TimeTrigger(timerPeriod, false));
            btr.Completed += Btr_Completed;
            // Update if the Internet is available.
            /*
            var btr2 = await BackTaskReg.backUpdateReg("HasNet", new SystemTrigger(SystemTriggerType.InternetAvailable, false));
            btr2.Completed += Btr_Completed;
            var btr3 = await BackTaskReg.backUpdateReg("UserPresent", new SystemTrigger(SystemTriggerType.UserPresent, false));
            btr3.Completed += Btr_Completed;*/
            return 0;
        }

        private async void Btr_Completed(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await ReloadXmlAndSitesData();
                app.vm.m.lastUpdateTime = DateTime.Now;
                statusTextBlock.Text = app.vm.m.getLastUpdateTime() + " " + app.vm.m.resLoader.GetString("updateFinish");
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
                Debug.WriteLine(ex.Message);
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
            // Because the function continues even if downloadAqData throw an exceptions, we have to record the download failure state by a variable.
            bool downloadSuccess = false;
            try
            {
#if DEBU
                // Do nothing.
#else
                await app.vm.m.loadSubscrSiteXml();

                if ((string)app.vm.m.localSettings.Values["UserPwd"] == "")
                {
                    statusTextBlock.Text = app.vm.m.resLoader.GetString("logining");
                    await app.vm.fbLogin();
                    statusTextBlock.Text = app.vm.m.resLoader.GetString("loginSuccess");
                }

                statusTextBlock.Text = app.vm.m.resLoader.GetString("downloading");
                await app.vm.m.downloadAqData();
                downloadSuccess = true;
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
                statusTextBlock.Text = ex.Message;
                // Ignore.
            }

            await updateSitesData();
            /*#if DEBUG
            #else*/
            app.vm.m.sendSubscrSitesNotifications();
            //#endif
            await app.vm.backTaskUpdateTiles();

            if (downloadSuccess)
            {
                app.vm.m.lastUpdateTime = DateTime.Now;
                statusTextBlock.Text = app.vm.m.getLastUpdateTime() + " " + app.vm.m.resLoader.GetString("updateFinish");
            }
            return 0;
        }

        private async Task<int> ReloadXmlAndSitesData()
        {
            try
            {
                await app.vm.m.loadAq2Dict();
            }
            catch
            {
                statusTextBlock.Text = app.vm.m.resLoader.GetString("updateFailTryManualUpdate");
            }

            try
            {
                await updateSitesData();
            }
            catch (Exception ex)
            {
                statusTextBlock.Text = "updateSitesData failed:" + ex.Message;
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
            catch
            {
                statusTextBlock.Text = app.vm.m.resLoader.GetString("updateFailTryManualUpdate");
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
                MySplitView.IsPaneOpen = false;
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
