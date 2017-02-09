using Microsoft.Practices.Prism.Mvvm;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Taq.Uwp.Views;
using Taq.Shared.Models;
using Taq.Shared.ModelViews;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.Devices.Geolocation;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.ApplicationModel.Resources;
using Windows.Security.Authentication.Web;
using Windows.Data.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Windows.Storage;
using System.Threading;
using Windows.Web.Http;
using Windows.Web.Http.Headers;
using Windows.Web.Http.Filters;
using Windows.Storage.Streams;
using Windows.Security.Authentication.Web.Core;
using Auth0.LoginClient;

namespace Taq.Uwp.ViewModels
{
    public class TaqViewModel : BindableBase
    {
        public TaqModel m = new TaqJsonModel();
        // Partial sites AQ information. Contain properties for data bindings (from AqView).
        public ObservableCollection<SiteViewModel> sites = new ObservableCollection<SiteViewModel>();

        public ObservableCollection<SiteViewModel> subscrSiteViews = new ObservableCollection<SiteViewModel>();

        public ResourceLoader resLoader = new ResourceLoader();

        public TaqViewModel()
        {

        }

        public bool Loggined
        {
            get
            {
                return (bool)m.localSettings.Values["Loggined"];
            }

            set
            {
                if (value != (bool)m.localSettings.Values["Loggined"])
                {
                    if (value == true)
                    {
                        authLogin();
                    }
                    else
                    {
                        authLogout();
                    }
                }
            }
        }

        public async Task<int> authLogin()
        {
            try
            {
                StatusText = m.resLoader.GetString("logining");
                var auth0User = await authLoginAux();
                await extractFbAuthResData(auth0User);
                m.localSettings.Values["Loggined"] = true;
                StatusText = m.resLoader.GetString("loginSuccess");
            }
            catch (Exception ex)
            {
                m.localSettings.Values["Loggined"] = false;
                StatusText = m.resLoader.GetString("loginFail") + ": " + ex.Message;
            }
            finally
            {
                OnPropertyChanged("Loggined");
            }
            return 0;
        }

        async Task<Auth0User> authLoginAux()
        {
            var auth0 = new Auth0Client(Params.auth0Domain, Params.auth0ClientId);
            var user = await auth0.LoginAsync();
            if (user == null)
                throw new Exception("Authentication cancelled!");
            return user;
        }

        public void authLogout()
        {
            // Clear login infos.
            UserName = "";
            m.localSettings.Values["UserPwd"] = "";
            var auth0 = new Auth0Client(Params.auth0Domain, Params.auth0ClientId);
            auth0.Logout();
            m.localSettings.Values["Loggined"] = false;
            OnPropertyChanged("Loggined");
        }

        public string getUserProfile(JObject profile, string field)
        {
            JToken t;
            profile.TryGetValue(field, out t);
            if (t == null)
            {
                return "";
            }
            return t.ToString();
        }

        private async Task extractFbAuthResData(Auth0User u)
        {
            string access_token = u.Auth0AccessToken;
            var email = getUserProfile(u.Profile, "email");

            // Register at TAQ server.
            var jTaqReq = new JObject();
            jTaqReq.Add("userToken", access_token);
            jTaqReq.Add("email", email);
            var content = new HttpStringContent(JsonConvert.SerializeObject(jTaqReq), UnicodeEncoding.Utf8, "application/json");

            var cts = new CancellationTokenSource();
            var ct = cts.Token;
            cts.CancelAfter(10000);

            var taqRegResMsg = await m.hc.PostAsync(new Uri(Params.uriHost + "userReg"), content).AsTask(ct);
            var taqResStr = await taqRegResMsg.Content.ReadAsStringAsync();
            if (!taqRegResMsg.IsSuccessStatusCode)
                throw new Exception(m.resLoader.GetString("taqServerDown") + taqResStr);
            var jTaqRegRes = JsonValue.Parse(taqResStr).GetObject();
            var err = jTaqRegRes.GetNamedString("error");
            if (err != "")
            {
                throw new Exception(err);
            }
            UserName = getUserProfile(u.Profile, "name");
            m.localSettings.Values["UserId"] = getUserProfile(u.Profile, "user_id");
            m.localSettings.Values["UserPwd"] = jTaqRegRes.GetNamedString("pwd");
        }

        public string UserName
        {
            get
            {
                return (string)m.localSettings.Values["UserName"];
            }

            set
            {
                if (value != (string)m.localSettings.Values["UserName"])
                {
                    m.localSettings.Values["UserName"] = value;
                    OnPropertyChanged("UserName");
                }
            }
        }

        // Has to be run by UI context!
        public void loadDict2Sites(string aqName)
        {
            var isSiteUninit = sites.Count() == 0;
            // if sites is uninitialized.
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
                site.aqi = m.getValidAqVal(m.sitesStrDict[site.siteName]["AQI"]);
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
                var btr = await BackTaskReg.RegisterBackgroundTask("BackTaskUpdateTiles", "Taq.BackTask.BackTaskUpdateTiles", trigger);
                btr.Completed += Btr_Completed;
                var result = await trigger.RequestAsync();
            }
            return 0;
        }

        private void Btr_Completed(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            isUpdateCompleted = true;
        }

        private int selAqId = 0;
        public int SelAqId
        {
            get
            {
                return selAqId;
            }

            set
            {
                if (value != selAqId)
                {
                    SetProperty(ref selAqId, value);
                }
            }
        }

        public ObservableCollection<AqGridView> aqgvList = new ObservableCollection<AqGridView>();

        public ObservableCollection<ObservableCollection<AqViewModel>> aqvmsList = new ObservableCollection<ObservableCollection<AqViewModel>>();

        // Has to be run by UI context!
        public async Task<int> loadSiteAqViews()
        {
            await m.loadMainSite((string)m.localSettings.Values["MainSite"]);
            loadMainSiteId();
            await m.loadSubscrSiteXml();
            // Create mode
            if (aqgvList.Count == 0)
            {
                var id = 0;
                foreach (var siteName in m.subscrSiteList)
                {
                    aqgvList.Add(new AqGridView(loadSite2dAqvm(siteName), id));
                    id++;
                }
            }
            // Update mode.
            else
            {
                for (var id = 0; id < aqgvList.Count; id++)
                {
                    updateAqgv(id);
                }
            }
            return 0;
        }

        // id == 0 stands for main site aqgv.
        public void updateAqgv(int id)
        {
            var siteName = m.subscrSiteList[id];
            var aqvms = aqvmsList[id];

            var i = 0;
            foreach (var k in StaticTaqModel.fieldNames.Keys)
            {
                var aqLevel = m.getAqLevel(siteName, k);
                var textColor = StaticTaqModelView.getTextColor(aqLevel);
                aqvms[i].CircleColor = StaticTaqModel.aqColors[k][aqLevel];
                aqvms[i].CircleText = StaticTaqModel.fieldNames[k] + "\n" + m.sitesStrDict[siteName][k];
                aqvms[i].TextColor = textColor;
                i++;
            }
        }

        public void addSecSiteAndAqView(string siteName)
        {
            aqgvList.Add(new AqGridView(loadSite2dAqvm(siteName), aqgvList.Count));
        }

        public void delSecSiteAndAqView(string siteName)
        {
            // Main site is still possible to be the same as one secondary site
            // after auto positioning. If so, delete the secondary one.
            var delId = m.subscrSiteList.LastIndexOf(siteName);
            Debug.Assert(delId != -1);
            aqgvList.RemoveAt(delId);
            aqvmsList.RemoveAt(delId);
            // Update IDs after delID.
            for (var id = delId; id < aqgvList.Count; id++)
            {
                aqgvList[id].id = id;
            }
        }

        // Convert site info to AqViewModel for for data bindings.
        public ObservableCollection<UIElement> loadSite2dAqvm(string siteName)
        {
            var aqvms = new ObservableCollection<AqViewModel>();
            var aqgvis = new ObservableCollection<UIElement>();

            foreach (var k in StaticTaqModel.fieldNames.Keys)
            {
                var aqLevel = m.getAqLevel(siteName, k);
                var textColor = StaticTaqModelView.getTextColor(aqLevel);
                var aqvm = new AqViewModel
                {
                    CircleColor = StaticTaqModel.aqColors[k][aqLevel], // default border background color
                    CircleText = StaticTaqModel.fieldNames[k] + "\n" + m.sitesStrDict[siteName][k],
                    TextColor = textColor
                };
                aqvms.Add(aqvm);

                // Create UIElement for GridViewItem by k value.
                switch (k)
                {
                    case "PM2.5":
                    case "PM2.5_AVG":
                        aqgvis.Add(new GridPm2_5(aqvm));
                        break;
                    case "PM10":
                    case "PM10_AVG":
                        aqgvis.Add(new GridPm10(aqvm));
                        break;

                    case "O3":
                    case "O3_8hr":
                        aqgvis.Add(new Grid1(aqvm, "O", "3", "ppb"));
                        break;

                    case "CO":
                    case "CO_8hr":
                        aqgvis.Add(new Grid1(aqvm, "CO", "", "ppm"));
                        break;

                    case "SO2":
                        aqgvis.Add(new Grid1(aqvm, "SO", "2", "ppb"));
                        break;

                    case "NO2":
                        aqgvis.Add(new Grid1(aqvm, "NO", "2", "ppb"));
                        break;

                    case "NOx":
                        aqgvis.Add(new Grid1(aqvm, "NO", "x", "ppb"));
                        break;

                    case "NO":
                        aqgvis.Add(new Grid1(aqvm, "NO", "", "ppb"));
                        break;

                    case "WindSpeed":
                        aqgvis.Add(new Grid1(aqvm, "", "", "m/s"));
                        break;

                    case "WindDirec":
                        aqgvis.Add(new Grid1(aqvm, "", "", "°"));
                        break;

                    default:
                        aqgvis.Add(new Grid1(aqvm, "", "", ""));
                        break;
                }
            }
            aqvmsList.Add(aqvms);
            return aqgvis;
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
                if (value != mainSiteId)
                {
                    SetProperty(ref mainSiteId, value);
                }
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
        // Init to -1 such that assign BgUpdatePeriodId to 0 will init localSettings.Values["BgUpdatePeriod"].
        private int bgUpdatePeriodId = -1;
        public int BgUpdatePeriodId
        {
            get
            {
                return bgUpdatePeriodId;
            }
            set
            {
                if (value != bgUpdatePeriodId)
                {
                    m.localSettings.Values["BgUpdatePeriod"] = bgUpdatePeriods[value];
                    SetProperty(ref bgUpdatePeriodId, value);
                }
            }
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
                if (value != (bool)m.localSettings.Values["TileClearSty"])
                {
                    m.localSettings.Values["TileClearSty"] = value;
                    OnPropertyChanged("TileClearSty");
                }
            }
        }

        public int TileBackColorAqId
        {
            get
            {
                return (int)m.localSettings.Values["TileBackColorAqId"];
            }

            set
            {
                if (value != (int)m.localSettings.Values["TileBackColorAqId"])
                {
                    m.localSettings.Values["TileBackColorAqId"] = value;
                    OnPropertyChanged("TileBackColorAqId");
                }
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
                if (value != (bool)m.localSettings.Values["SecondSitesNotify"])
                {
                    m.localSettings.Values["SecondSitesNotify"] = value;
                    OnPropertyChanged("SecondSitesNotify");
                }
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
                if (value != (bool)m.localSettings.Values["WarnStateChangeMode"])
                {
                    m.localSettings.Values["WarnStateChangeMode"] = value;
                    OnPropertyChanged("WarnStateChangeMode");
                }
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
                if (value != (bool)m.localSettings.Values["MapColor"])
                {
                    m.localSettings.Values["MapColor"] = value;
                    OnPropertyChanged("MapColor");
                }
            }
        }

        public bool AutoPos
        {
            get
            {
                return (bool)m.localSettings.Values["AutoPos"];
            }

            set
            {
                if (value == (bool)m.localSettings.Values["AutoPos"])
                    return;

                if (value == true)
                {
                    // Can not and do not await in a property!
                    reqLocAccessAsync();
                }
                else
                {
                    m.localSettings.Values["AutoPos"] = false;
                    OnPropertyChanged("AutoPos");
                }
            }
        }

        async Task<int> reqLocAccessAsync()
        {
            try
            {
                m.locAccStat = await Geolocator.RequestAccessAsync();
            }
            catch (Exception ex)
            {
                StatusText = ex.Message;
            }

            switch (m.locAccStat)
            {
                case GeolocationAccessStatus.Allowed:
                    m.localSettings.Values["AutoPos"] = true;
                    break;
                default:
                    var cd = new ContentDialog { Title = m.resLoader.GetString("enableGpsFail") };
                    var txt = new TextBlock { Text = m.resLoader.GetString("enableGpsFailMsg"), TextWrapping = TextWrapping.Wrap };
                    cd.Content = txt;
                    cd.PrimaryButtonText = "OK";
                    cd.PrimaryButtonClick += async (sender, e) =>
                    {
                        await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-location"));
                    };
                    await cd.ShowAsync();

                    m.localSettings.Values["AutoPos"] = false;
                    break;
            }
            OnPropertyChanged("AutoPos");
            return 0;
        }

        public bool MapAutoPos
        {
            get
            {
                return (bool)m.localSettings.Values["MapAutoPos"];
            }

            set
            {
                if (value != (bool)m.localSettings.Values["MapAutoPos"])
                {
                    m.localSettings.Values["MapAutoPos"] = value;
                    OnPropertyChanged("MapAutoPos");
                }
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
                if (value != (bool)m.localSettings.Values["BgMainSiteAutoPos"])
                {
                    m.localSettings.Values["BgMainSiteAutoPos"] = value;
                    OnPropertyChanged("BgMainSiteAutoPos");
                }
            }
        }

        string statusText = "";
        public string StatusText
        {
            get
            {
                return statusText;
            }

            set
            {
                if (value != statusText)
                {
                    SetProperty(ref statusText, value);
                }
            }

        }
    }
}
