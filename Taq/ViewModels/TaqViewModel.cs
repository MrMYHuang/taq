using Microsoft.Practices.Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.Devices.Geolocation;
using Windows.System;
using Windows.UI;
using Windows.UI.StartScreen;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;

namespace Taq
{
    public class TaqViewModel : BindableBase
    {
        public TaqModel m = new TaqModel();
        // Partial sites AQ information. Contain properties for data bindings (from AqView).
        public ObservableCollection<SiteViewModel> sites = new ObservableCollection<SiteViewModel>();
        // Current site info converted to for data bindings through AqView.
        public ObservableCollection<AqViewModel> currSiteViews = new ObservableCollection<AqViewModel>();

        public ObservableCollection<SiteViewModel> subscrSiteViews = new ObservableCollection<SiteViewModel>();

        public TaqViewModel()
        {

        }

        // Has to be run by UI context!
        public void loadDict2Sites(string aqName)
        {
            var isSiteUninit = sites.Count() == 0;
            if (isSiteUninit)
            {
                foreach (var s in m.sitesStrDict)
                {
                    var siteDict = s.Value;
                    sites.Add(new SiteViewModel
                    {
                        siteName = s.Key,
                        county = s.Value["County"],
                        twd97Lat = double.Parse(siteDict["TWD97Lat"]),
                        twd97Lon = double.Parse(siteDict["TWD97Lon"]),
                    });
                }
            }

            foreach (var site in sites)
            {
                var aqLevel = m.getAqLevel(site.siteName, aqName);
                site.CircleColor = m.aqColors[aqName][aqLevel];
                site.CircleText = site.siteName + "\n" + m.sitesStrDict[site.siteName][aqName];
                site.ListText = m.sitesStrDict[site.siteName][aqName];
                site.TextColor = aqLevel > 3 ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);
            }
            loadSubscrSiteId();
        }

        public async Task<int> loadSubscrSiteViewModel()
        {
            await m.loadSubscrSiteXml();
            subscrSiteViews.Clear();
            foreach (var siteName in m.subscrSiteList)
            {
                subscrSiteViews.Add(new SiteViewModel { siteName = siteName, CircleColor = "Green", CircleText = siteName, TextColor = new SolidColorBrush(Colors.White) });
            }
            await m.saveSubscrXd();
            return 0;
        }


        public async Task<int> addSubscrSite(string siteName)
        {
            m.subscrXd.Root.Add(new XElement("SiteName", siteName));
            await m.saveSubscrXd();
            await loadSubscrSiteViewModel();
            return 0;
        }

        public async Task<int> delSubscrSite(object[] itemsSelected)
        {
            foreach (SiteViewModel item in itemsSelected)
            {
                m.subscrXd.Descendants("SiteName").Where(s => s.Value == item.siteName).First().Remove();
            }
            await m.saveSubscrXd();
            await loadSubscrSiteViewModel();
            return 0;
        }

        // Update live tiles by a background task.
        // Don't directly call app.vm.m.updateLiveTile,
        // which might fail to draw tile images with UI context.
        public async Task<int> backTaskUpdateTiles()
        {
            ApplicationTrigger trigger = new ApplicationTrigger();
            await RegisterBackgroundTask("BackTaskUpdateTiles", "TaqBackTask.BackTaskUpdateTiles", trigger);
            var result = await trigger.RequestAsync();
            return 0;
        }

        private int selAqId;
        public int SelAqId
        {
            get
            {
                return selAqId;
            }

            set
            {
                loadDict2Sites(m.aqList[value]);
                SetProperty(ref selAqId, value);
            }
        }

        // Has to be run by UI context!
        public void currSite2AqView()
        {
            // Don't remove all elements by new.
            // Otherwise, data bindings would be problematic.
            currSiteViews.Clear();
            foreach (var k in m.fieldNames.Keys)
            {
                var aqLevel = m.getAqLevel(m.currSiteStrDict["SiteName"], k);
                var textColor = aqLevel > 3 ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);
                currSiteViews.Add(new AqViewModel
                {
                    CircleColor = m.aqColors[k][aqLevel], // default border background color
                    CircleText = m.fieldNames[k] + "\n" + m.currSiteStrDict[k],
                    TextColor = textColor
                });
            }
        }

        private int subscrSiteId;
        public int SubscrSiteId
        {
            get
            {
                return subscrSiteId;
            }
            set
            {
                SetProperty(ref subscrSiteId, value);
            }
        }

        public void loadSubscrSiteId()
        {
            var subscrSiteName = (string)m.localSettings.Values["subscrSite"];
            var subscrSiteElem = from s in sites
                                 where s.siteName == subscrSiteName
                                 select s;
            SubscrSiteId = sites.IndexOf(subscrSiteElem.First());
        }

        public List<int> bgUpdatePeriods = new List<int> { 15, 20, 30, 60 };
        private int bgUpdatePeriodId;
        public int BgUpdatePeriodId
        {
            get
            {
                return bgUpdatePeriodId;
            }
            set
            {
                SetProperty(ref bgUpdatePeriodId, value);
                m.localSettings.Values["BgUpdatePeriod"] = bgUpdatePeriods[value];
                RegisterBackgroundTask("TaqBackTask", "TaqBackTask.TaqBackTask", new TimeTrigger(Convert.ToUInt32(m.localSettings.Values["BgUpdatePeriod"]), false));
            }
        }

        public async Task<int> RegisterBackgroundTask(string taskName, string taskEntryPoint, IBackgroundTrigger trigger)
        {
            var backgroundAccessStatus = await BackgroundExecutionManager.RequestAccessAsync();
            if (backgroundAccessStatus == BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity ||
                backgroundAccessStatus == BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity ||
                backgroundAccessStatus == BackgroundAccessStatus.AllowedSubjectToSystemPolicy ||
                backgroundAccessStatus == BackgroundAccessStatus.AlwaysAllowed)
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
                taskBuilder.SetTrigger(trigger);
                var registration = taskBuilder.Register();
            }
            return 0;
        }

        public string Version
        {
            get
            {
                return String.Format("{0}.{1}.{2}",
                    Package.Current.Id.Version.Major,
                    Package.Current.Id.Version.Minor,
                    Package.Current.Id.Version.Build);
            }
        }

        // Settings related properites.
        public bool TileClearSty
        {
            get
            {
                return (bool)m.localSettings.Values["TileClearSty"];
            }

            set
            {
                m.localSettings.Values["TileClearSty"] = value;
                OnPropertyChanged("TileClearSty");
            }
        }

        public bool WarnStateChangeMode
        {
            get
            {
                return (bool)m.localSettings.Values["WarnStateChangeMode"];
            }

            set
            {
                m.localSettings.Values["WarnStateChangeMode"] = value;
                OnPropertyChanged("WarnStateChangeMode");
            }
        }

        public bool MapColor
        {
            get
            {
                return (bool)m.localSettings.Values["MapColor"];
            }

            set
            {
                m.localSettings.Values["MapColor"] = value;
                OnPropertyChanged("MapColor");
            }
        }

        public GeolocationAccessStatus locAccStat;
        public Geolocator geoLoc;
        public bool MapAutoPos
        {
            get
            {
                return (bool)m.localSettings.Values["MapAutoPos"];
            }

            set
            {
                if (value == true)
                {
                    switch (locAccStat)
                    {
                        case GeolocationAccessStatus.Allowed:
                            // Subscribe to the PositionChanged event to get location updates.
                            //geoLoc.PositionChanged += OnPositionChanged;
                            m.localSettings.Values["MapAutoPos"] = true;
                            break;
                        default:
                            var cd = new ContentDialog { Title = "啟動定位失敗！" };
                            var txt = new TextBlock { Text = "您曾拒絕TAQ存取您的位置資訊。必須去系統設定修改准許TAQ存取，然後重啟TAQ。按下確認鈕將開啟系統位置設定頁面。", TextWrapping = TextWrapping.Wrap };
                            cd.Content = txt;
                            cd.PrimaryButtonText = "OK";
                            cd.PrimaryButtonClick += (sender, e) =>
                            {
                                Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-location"));
                            };
                            cd.ShowAsync();
                            //var md = new Windows.UI.Popups.MessageDialog("您曾拒絕TAQ存取您的位置資訊。必須去系統設定修改准許TAQ存取，然後重啟TAQ。若找不到該設定，可以嘗試重新安裝TAQ解決。", "啟動定位失敗！");

                            //md.ShowAsync();
                            m.localSettings.Values["MapAutoPos"] = false;
                            break;
                    }
                }
                else
                {
                    m.localSettings.Values["MapAutoPos"] = value;
                }
                OnPropertyChanged("MapAutoPos");
            }
        }
    }
}
