using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

using TaqShared;
using TaqShared.Models;
using System.Xml.Linq;
using Windows.Storage;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.Background;
using System.Threading.Tasks;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.Devices.Geolocation;

namespace Taq.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Home : Page
    {
        public App app;
        // For updating UI after TaqBackTask downloads a new XML.
        ThreadPoolTimer periodicTimer;

        public Home()
        {
            this.InitializeComponent();
            app = App.Current as App;
            initAqData();
            initPeriodicTimer();
            this.DataContext = this;
        }

        public async void initAqData()
        {
            try
            {
                await app.shared.downloadDataXml();
                await updateListView();
                app.shared.updateLiveTile();
            }
            catch (DownloadException ex)
            {
                statusTextBlock.Text = "資料庫下載失敗。請檢查網路，再嘗試手動更新。";
            }
            catch (Exception ex)
            {
                statusTextBlock.Text = "初始化失敗。請檢查網路，再嘗試手動更新。";
            }
        }

        private void initPeriodicTimer()
        {

#if DEBUG
            TimeSpan delay = TimeSpan.FromSeconds(3e3);
#else
            TimeSpan delay = TimeSpan.FromSeconds(60);
#endif
            periodicTimer = ThreadPoolTimer.CreatePeriodicTimer((source) =>
            {
                // TODO: Work

                // Update the UI thread by using the UI core dispatcher.
                Dispatcher.RunAsync(CoreDispatcherPriority.High,
                    () =>
                    {
                        try
                        {
                            updateListView();
                        }
                        catch (Exception ex)
                        {
                            statusTextBlock.Text = "自動更新失敗。請嘗試手動更新。";
                        }
                    }
                );

            }, delay);
        }
        public async Task<int> updateListView()
        {
            await reloadDataX();
            await app.shared.loadCurrSite();
            return 0;
        }

        public async Task<int> refreshSites()
        {
            try
            {
                statusTextBlock.Text = "Download start.";
                await app.shared.downloadDataXml();
                statusTextBlock.Text = "Download finish.";
                await updateListView();
                app.shared.updateLiveTile();
                app.shared.sendNotify();
            }
            catch (DownloadException ex)
            {
                statusTextBlock.Text = "資料庫下載失敗。請檢查網路，再嘗試手動更新。";
            }
            catch (Exception ex)
            {
                statusTextBlock.Text = "手動更新失敗。";
            }
            return 0;
        }

        public async Task<int> reloadDataX()
        {
            await app.shared.reloadXd();
            var dataX = from data in app.shared.xd.Descendants("Data")
                        select data;
            await app.shared.loadSiteGeoXd();
            var geoDataX = from data in app.shared.siteGeoXd.Descendants("Data")
                           select data;

            if (app.sites.Count != 0)
            {
                removeAllSites();
            }
            
            foreach (var d in dataX.OrderBy(x => x.Element("County").Value))
            {
                var siteName = d.Descendants("SiteName").First().Value;
                var geoD = from gd in geoDataX
                           where gd.Descendants("SiteName").First().Value == siteName
                           select gd;
                app.sites.Add(new Site
                {
                    siteName = siteName,
                    County = d.Descendants("County").First().Value,
                    Pm2_5 = d.Descendants("PM2.5").First().Value,
                    twd97Lat = double.Parse(geoD.Descendants("TWD97Lat").First().Value),
                    twd97Lon = double.Parse(geoD.Descendants("TWD97Lon").First().Value),
                });
            }

            return 0;
        }

        // Removing all items before updating, because the new download data XML file
        // could have a different number of Data elements from the old one.
        public void removeAllSites()
        {
            for (var i = app.sites.Count() - 1; i >= 0; i--)
            {
                app.sites.RemoveAt(i);
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.RegisterBackgroundTask();
        }

        private const string taskName = "TaqBackTask";
        private const string taskEntryPoint = "TaqBackTask.TaqBackTask";
        private async void RegisterBackgroundTask()
        {
            var backgroundAccessStatus = await BackgroundExecutionManager.RequestAccessAsync();
            if (backgroundAccessStatus == BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity ||
                backgroundAccessStatus == BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity)
            {
                foreach (var task in BackgroundTaskRegistration.AllTasks)
                {
                    if (task.Value.Name == taskName)
                    {
                        task.Value.Unregister(true);
                    }
                }

                BackgroundTaskBuilder taskBuilder = new BackgroundTaskBuilder();
                taskBuilder.Name = taskName;
                taskBuilder.TaskEntryPoint = taskEntryPoint;
                taskBuilder.SetTrigger(new TimeTrigger(15, false));
                var registration = taskBuilder.Register();
            }
        }

        private async void button_Click(Object sender, RoutedEventArgs e)
        {
            await refreshSites();
        }
    }
}
