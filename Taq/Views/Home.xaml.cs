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

namespace Taq.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Home : Page
    {
        public string[] aqHeaders { get { return new[] { "siteName", "Pm2_5" }; } }
        private Shared shared = new Shared();
        public ObservableCollection<Site> sites = new ObservableCollection<Site>();
        private IEnumerable<XElement> currData;
        // For updating UI after TaqBackTask downloads a new XML.
        ThreadPoolTimer periodicTimer;

        public Home()
        {
            this.InitializeComponent();
            initAqData();
            initPeriodicTimer();
            this.DataContext = this;
        }

        public async void initAqData()
        {
            try
            {
                await shared.downloadDataXml();
                await updateListView();
                shared.updateLiveTile();
            }
            catch (Exception ex)
            {
                statusTextBlock.Text = "初始化失敗。請檢查網路，再嘗試手動更新。";
            }
        }

        private void initPeriodicTimer()
        {
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

            }, TimeSpan.FromMinutes(1));
        }
        public async Task<int> updateListView()
        {
            await reloadDataX();
            await shared.loadCurrSite();
            changeSelSiteListItem();
            return 0;
        }

        public async Task<int> refreshSites()
        {
            try
            {
                statusTextBlock.Text = "Download start.";
                await shared.downloadDataXml();
                statusTextBlock.Text = "Download finish.";
                await updateListView();
                shared.updateLiveTile();
                shared.sendNotify();
            }
            catch (Exception ex)
            {
                statusTextBlock.Text = "手動更新失敗。";
            }
            return 0;
        }

        private async void listView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var site = (Site)e.ClickedItem;
            shared.currSite = site;

            currData = from data in shared.xd.Descendants("Data")
                       where data.Descendants("SiteName").First().Value == site.siteName
                       select data;
            var currXd = new XDocument();
            currXd.Add(currData.First());
            try
            {
                var currDataFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(shared.currDataXmlFile, CreationCollisionOption.ReplaceExisting);
                using (var c = await currDataFile.OpenStreamForWriteAsync())
                {
                    currXd.Save(c);
                }
            }
            catch (Exception ex)
            {
                statusTextBlock.Text = "檔案寫入失敗：" + shared.currDataXmlFile;
            }

            shared.updateLiveTile();
        }

        public async Task<int> reloadDataX()
        {
            await shared.reloadXd();
            var dataX = from data in shared.xd.Descendants("Data")
                        select data;

            if (sites.Count != 0)
            {
                removeAllSites();
            }

            var i = 0;
            foreach (var d in dataX)
            {
                sites.Add(new Site { siteName = d.Descendants("SiteName").First().Value, Pm2_5 = d.Descendants("PM2.5").First().Value });
                i++;
            }
            return 0;
        }

        void changeSelSiteListItem()
        {
            var selSite = 0;
            var i = 0;
            foreach (var d in shared.xd.Descendants("Data"))
            {
                if (d.Descendants("SiteName").First().Value == shared.currSite.siteName)
                {
                    selSite = i;
                }
                i++;
            }
            // Restore last selected site.
            // Ensure listView is initialized asynchronously.
            while (listView.Items.Count == 0) ;
            listView.SelectedIndex = selSite;
        }

        // Removing all items before updating, because the new download data XML file
        // could have a different number of Data elements from the old one.
        public void removeAllSites()
        {
            for (var i = sites.Count() - 1; i >= 0; i--)
            {
                sites.RemoveAt(i);
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
