using Microsoft.Practices.Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Taq.Views;
using TaqShared.Models;
using TaqShared.ModelViews;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Taq
{
    public class TaqViewModel : BindableBase
    {
        public TaqModel m = new TaqJsonModel();
        // Partial sites AQ information. Contain properties for data bindings (from AqView).
        public ObservableCollection<SiteViewModel> sites = new ObservableCollection<SiteViewModel>();

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
                        aqi = m.getValidAqVal(siteDict["AQI"]),
                        twd97Lat = double.Parse(siteDict["TWD97Lat"]),
                        twd97Lon = double.Parse(siteDict["TWD97Lon"]),
                    });
                }
            }

            foreach (var site in sites)
            {
                var aqLevel = m.getAqLevel(site.siteName, aqName);
                site.CircleColor = StaticTaqModel.aqColors[aqName][aqLevel];
                site.CircleText = site.siteName + "\n" + m.sitesStrDict[site.siteName][aqName];
                site.ListText = m.sitesStrDict[site.siteName][aqName];
                site.TextColor = StaticTaqModelView.getTextColor(aqLevel);
            }
        }

        public async Task<int> loadSubscrSiteViewModel()
        {
            await m.loadSubscrSiteXml();
            subscrSiteViews.Clear();
            foreach (var siteName in m.subscrSiteList.GetRange(1, m.subscrSiteList.Count - 1))
            {
                subscrSiteViews.Add(new SiteViewModel { siteName = siteName, CircleColor = "Green", CircleText = siteName, TextColor = new SolidColorBrush(Colors.White) });
            }
            //await m.saveSubscrXd();
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

        // In foreground, updating live tiles by a background task.
        // Don't directly call app.vm.m.updateLiveTile,
        // which might fail to draw tile images with UI context.
        bool isUpdateCompleted = true;
        public async Task<int> backTaskUpdateTiles()
        {
            if (isUpdateCompleted == true)
            {
                isUpdateCompleted = false;
                ApplicationTrigger trigger = new ApplicationTrigger();
                var btr = await RegisterBackgroundTask("BackTaskUpdateTiles", "TaqBackTask.BackTaskUpdateTiles", trigger);
                btr.Completed += Btr_Completed;
                var result = await trigger.RequestAsync();
            }
            return 0;
        }

        private void Btr_Completed(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            isUpdateCompleted = true;
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
                if (value != -1)
                {
                    loadDict2Sites(m.aqList[value]);
                }
                SetProperty(ref selAqId, value);
            }
        }

        public ObservableCollection<AqGridView> aqgvList = new ObservableCollection<AqGridView>();

        // Has to be run by UI context!
        public async Task<int> loadSiteAqViews()
        {
            await m.loadMainSite((string)m.localSettings.Values["MainSite"]);
            loadMainSiteId();
            await m.loadSubscrSiteXml();
            // Create mode
            if (aqgvList.Count == 0)
            {
                foreach (var siteName in m.subscrSiteList)
                {
                    aqgvList.Add(new AqGridView(loadMainSite2dAqView(siteName), siteName));
                }
            }
            // Update mode.
            else
            {
                var i = 0;
                foreach (var aqgv in aqgvList)
                {
                    updateAqgv(m.subscrSiteList[i], aqgv);
                    i++;
                }
            }
            return 0;
        }

        public void updateAqgv(string siteName, AqGridView aqgv)
        {
            var i = 0;
            foreach (var k in StaticTaqModel.fieldNames.Keys)
            {
                var aqLevel = m.getAqLevel(siteName, k);
                var textColor = StaticTaqModelView.getTextColor(aqLevel);
                aqgv.aqvms[i].CircleColor = StaticTaqModel.aqColors[k][aqLevel];
                aqgv.aqvms[i].CircleText = StaticTaqModel.fieldNames[k] + "\n" + m.sitesStrDict[siteName][k];
                aqgv.aqvms[i].TextColor = textColor;
                i++;
            }
        }

        public void addSecSiteAndAqView(string siteName)
        {
            aqgvList.Add(new AqGridView(loadMainSite2dAqView(siteName), siteName));
        }

        public void delSecSiteAndAqView(string siteName)
        {
            // Main site is still possible to be the same as one secondary site
            // after auto positioning. If so, delete the secondary one.
            var delId = m.subscrSiteList.LastIndexOf(siteName);
            Debug.Assert(delId != -1);
            aqgvList.RemoveAt(delId);
        }

        public ObservableCollection<AqViewModel> loadMainSite2dAqView(string siteName)
        {
            // Current site info converted to for data bindings through AqView.
            ObservableCollection<AqViewModel> aqvms = new ObservableCollection<AqViewModel>();

            foreach (var k in StaticTaqModel.fieldNames.Keys)
            {
                var aqLevel = m.getAqLevel(siteName, k);
                var textColor = StaticTaqModelView.getTextColor(aqLevel);
                aqvms.Add(new AqViewModel
                {
                    CircleColor = StaticTaqModel.aqColors[k][aqLevel], // default border background color
                    CircleText = StaticTaqModel.fieldNames[k] + "\n" + m.sitesStrDict[siteName][k],
                    TextColor = textColor
                });
            }
            return aqvms;
        }

        private int mainSiteId;
        public int MainSiteId
        {
            get
            {
                return mainSiteId;
            }
            set
            {
                SetProperty(ref mainSiteId, value);
            }
        }

        public void loadMainSiteId()
        {
            var mainSiteName = (string)m.localSettings.Values["MainSite"];
            var i = 0;
            foreach (var k in m.sitesStrDict.Keys)
            {
                if (mainSiteName == k)
                {
                    MainSiteId = i;
                    break;
                }
                i++;
            }
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
            }
        }

        public int unregisterBackTask(string taskName)
        {
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == taskName)
                {
                    task.Value.Unregister(true);
                }
            }
            return 0;
        }

        public async Task<BackgroundTaskRegistration> RegisterBackgroundTask(string taskName, string taskEntryPoint, IBackgroundTrigger trigger)
        {
            var backgroundAccessStatus = await BackgroundExecutionManager.RequestAccessAsync();
            if (backgroundAccessStatus == BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity ||
                backgroundAccessStatus == BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity ||
                backgroundAccessStatus == BackgroundAccessStatus.AllowedSubjectToSystemPolicy ||
                backgroundAccessStatus == BackgroundAccessStatus.AlwaysAllowed)
            {
                unregisterBackTask(taskName);

                BackgroundTaskBuilder taskBuilder = new BackgroundTaskBuilder();
                taskBuilder.Name = taskName;
                taskBuilder.TaskEntryPoint = taskEntryPoint;
                taskBuilder.SetTrigger(trigger);
                //taskBuilder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
                try
                {
                    var btr = taskBuilder.Register();
                    return btr;
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
            return null;
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

        public bool SecondSitesNotify
        {
            get
            {
                return (bool)m.localSettings.Values["SecondSitesNotify"];
            }

            set
            {
                m.localSettings.Values["SecondSitesNotify"] = value;
                OnPropertyChanged("SecondSitesNotify");
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

        public bool AutoPos
        {
            get
            {
                if (m.localSettings.Values["AutoPos"] == null)
                {
                    return false;
                }
                return (bool)m.localSettings.Values["AutoPos"];
            }

            set
            {
                if (value == true)
                {
                    switch (m.locAccStat)
                    {
                        case GeolocationAccessStatus.Allowed:
                            // Subscribe to the PositionChanged event to get location updates.
                            //geoLoc.PositionChanged += OnPositionChanged;
                            m.localSettings.Values["AutoPos"] = true;
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
                            m.localSettings.Values["AutoPos"] = false;
                            break;
                    }
                }
                else
                {
                    m.localSettings.Values["AutoPos"] = value;
                }
                OnPropertyChanged("AutoPos");
            }
        }

        public bool MapAutoPos
        {
            get
            {
                return (bool)m.localSettings.Values["MapAutoPos"];
            }

            set
            {
                m.localSettings.Values["MapAutoPos"] = value;
                OnPropertyChanged("MapAutoPos");
            }
        }

        public bool BgMainSiteAutoPos
        {
            get
            {
                return (bool)m.localSettings.Values["BgMainSiteAutoPos"];
            }

            set
            {
                m.localSettings.Values["BgMainSiteAutoPos"] = value;
                OnPropertyChanged("BgMainSiteAutoPos");
            }
        }
    }
}
