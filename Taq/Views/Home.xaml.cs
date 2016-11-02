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

namespace Taq.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Home : Page
    {
        public string [] aqHeaders { get { return new[] { "siteName", "Pm2_5" }; } }
        private Shared shared = new Shared();
        public ObservableCollection<Site> sites = new ObservableCollection<Site>();
        private IEnumerable<XElement> currData;

        public Home()
        {
            this.InitializeComponent();
            checkAndDownloadDataXml();
            this.DataContext = this;
        }

        public async Task<int> refreshSites()
        {
            try
            {
                statusTextBlock.Text = "Download start.";
                await shared.downloadDataXml();
                statusTextBlock.Text = "Download finish.";
                reloadDataX();
                await shared.loadCurrSite();
                changeSelSiteListItem();
                shared.updateLiveTile();
                shared.sendNotify();
            }
            catch (Exception ex)
            {
                //LogException("Download Error", ex);
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
            var currDataFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(shared.currDataXmlFile, CreationCollisionOption.ReplaceExisting);
            using (var c = await currDataFile.OpenStreamForWriteAsync())
            {
                currXd.Save(c);
            }

            shared.updateLiveTile();
        }

        public async void checkAndDownloadDataXml()
        {
            await shared.downloadDataXml();
            reloadDataX();
            await shared.loadCurrSite();
            changeSelSiteListItem();
            shared.updateLiveTile();
        }

        public int reloadDataX()
        {
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

                /*
                BackgroundTaskBuilder taskBuilder2 = new BackgroundTaskBuilder();
                taskBuilder2.Name = "TaqBackTask2";
                taskBuilder2.TaskEntryPoint = taskEntryPoint;
                taskBuilder2.SetTrigger(at);
                var registration2 = taskBuilder2.Register();*/
            }
        }

        //private ApplicationTrigger at = new ApplicationTrigger();

        private const string taskName = "TaqBackTask";
        private const string taskEntryPoint = "TaqBackTask.TaqBackTask";

        private async void button_Click(Object sender, RoutedEventArgs e)
        {
            await refreshSites();
        }
    }
}
