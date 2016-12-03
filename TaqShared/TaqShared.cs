using Microsoft.Toolkit.Uwp.Notifications;
using NotificationsExtensions.TileContent;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using TaqShared.Models;
using Windows.Devices.Geolocation;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.UI.Notifications;

namespace TaqShared
{
    public class DownloadException : Exception
    {

    }
    public class OldXmlException : Exception
    {

    }

    public class Shared : INotifyPropertyChanged
    {
        private Windows.Storage.ApplicationDataContainer localSettings;
        public const string dataXmlFile = "taqi.xml";
        public Uri source = new Uri("http://YourTaqServerIp/taq/" + dataXmlFile);
        public string currDataXmlFile = "currData.xml";
        public XDocument xd = new XDocument();
        public XDocument siteGeoXd = new XDocument();
        public ObservableCollection<Site> sites = new ObservableCollection<Site>();
        public Dictionary<string, Dictionary<string, string>> sitesDict = new Dictionary<string, Dictionary<string, string>>();
        public Site currSite = new Site { siteName = "N/A", Pm2_5 = "0" };
        public ObservableCollection<string[]> currSiteViews = new ObservableCollection<string[]>();
        public Dictionary<string, string> currSiteDict;

        public Site oldSite = new Site { siteName = "N/A", Pm2_5 = "0" };
        public Dictionary<string, string> oldSiteDict;

        // The order of keys is meaningful.
        // The display order of AQ items in Home.xaml follows this order of keys.
        public Dictionary<string, string> fieldNames = new Dictionary<string, string>
        {
            { "PublishTime", "發佈時間"},
            { "SiteName", "觀測站" },
            { "County", "縣市"},
            { "AQI", "空氣品質指標"},
            { "Status", "狀態"},
            { "Pollutant", "污染指標物"},
            { "PM2.5", "PM 2.5"},
            { "PM2.5_AVG", "PM2.5_AVG"},
            { "PM10", "PM 10"},
            { "PM10_AVG", "PM10_AVG"},
            { "O3", "O3"},
            { "O3_8hr", "O3_8hr"},
            { "CO", "CO"},
            { "CO_8hr", "CO_8hr"},
            { "SO2", "SO2"},
            { "NO2", "NO2"},
            { "NOx", "NOx"},
            { "NO", "NO"},
            { "WindSpeed", "風速"},
            { "WindDirec", "風向"},
        };

        // Notice: a color list has one more element than a limit list!
        public static List<int> defaultLimits = new List<int> { 0 };
        public static List<string> defaultColors = new List<string> { "#31cf00", "#31cf00" };

        public static List<int> pm2_5_concens = new List<int> { 11, 23, 35, 41, 47, 53, 58, 64, 70 };
        public static List<string> pm2_5_colors = new List<string> { "#9cff9c", "#31ff00", "#31cf00", "#ffff00", "#ffcf00", "#ff9a00", "#ff6464", "#ff0000", "#990000", "#ce30ff" };

        public static List<int> aqiLimits = new List<int> { 50, 100, 150, 200, 300, 400, 500 };
        public static List<string> aqiBgColors = new List<string> { "#00ff00", "#ffff00", "#ff7e00", "#ff0000", "#800080", "#633300", "#633300", "#633300" };

        public Dictionary<string, List<string>> aqColors = new Dictionary<string, List<string>>
        {
            { "PublishTime", defaultColors},
            { "SiteName", defaultColors },
            { "County", defaultColors},
            { "Pollutant", defaultColors},
            { "AQI", aqiBgColors},
            { "Status", defaultColors},
            { "PM2.5", pm2_5_colors},
            { "PM2.5_AVG", pm2_5_colors},
            { "PM10", defaultColors},
            { "PM10_AVG", defaultColors},
            { "O3", defaultColors},
            { "O3_8hr", defaultColors},
            { "CO", defaultColors},
            { "CO_8hr", defaultColors},
            { "SO2", defaultColors},
            { "NO2", defaultColors},
            { "NOx", defaultColors},
            { "NO", defaultColors},
            { "WindSpeed", defaultColors},
            { "WindDirec", defaultColors},
        };

        public Dictionary<string, List<int>> aqLimits = new Dictionary<string, List<int>>
        {
            { "PublishTime", defaultLimits},
            { "SiteName", defaultLimits },
            { "County", defaultLimits},
            { "Pollutant", defaultLimits},
            { "AQI", aqiLimits},
            { "Status", defaultLimits},
            { "PM2.5", pm2_5_concens},
            { "PM2.5_AVG", pm2_5_concens},
            { "PM10", defaultLimits},
            { "PM10_AVG", defaultLimits},
            { "O3", defaultLimits},
            { "O3_8hr", defaultLimits},
            { "CO", defaultLimits},
            { "CO_8hr", defaultLimits},
            { "SO2", defaultLimits},
            { "NO2", defaultLimits},
            { "NOx", defaultLimits},
            { "NO", defaultLimits},
            { "WindSpeed", defaultLimits},
            { "WindDirec", defaultLimits},
        };

        public List<string> aqList = new List<string>
        {"AQI", "Status", "PM2.5", "PM2.5_AVG", "PM10", "PM10_AVG", "O3", "O3_8hr", "CO", "CO_8hr", "SO2", "NO2", "NOx", "NO", "WindSpeed", "WindDirec"};

        public Shared()
        {
            localSettings =
       Windows.Storage.ApplicationData.Current.LocalSettings;
        }

        public async Task<int> downloadDataXml()
        {
            // Download may fail, so we create a temp StorageFile.
            var dlFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("Temp" + dataXmlFile, CreationCollisionOption.ReplaceExisting);

            BackgroundDownloader downloader = new BackgroundDownloader();
            DownloadOperation download = downloader.CreateDownload(source, dlFile);

            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

            cts.CancelAfter(5000);
            try
            {
                // Pass the token to the task that listens for cancellation.
                await download.StartAsync().AsTask(token);
                // file is downloaded in time
                // Copy download file to dataXmlFile.
                var dataXml = await ApplicationData.Current.LocalFolder.CreateFileAsync(dataXmlFile, CreationCollisionOption.ReplaceExisting);
                await dlFile.CopyAndReplaceAsync(dataXml);
            }
            catch (Exception ex)
            {
                // timeout is reached, downloadOperation is cancled
                throw new DownloadException();
            }
            finally
            {
                // Releases all resources of cts
                cts.Dispose();
            }

            return 0;
        }

        public int loadSiteGeoXd()
        {
            /*
            var dataXml = await ApplicationData.Current.LocalFolder.GetFileAsync("Assets/SiteGeo.xml");
            using (var s = await dataXml.OpenStreamForReadAsync())
            {
                // Reload to xd.
                siteGeoXd = XDocument.Load(s);
            }*/
            //http://opendata.epa.gov.tw/ws/Data/AQXSite/?format=xml
            siteGeoXd = XDocument.Load("Assets/SiteGeo.xml");

            return 0;
        }

        // Reload air quality XML files.
        public async Task<int> reloadXd()
        {
            var dataXml = await ApplicationData.Current.LocalFolder.GetFileAsync(dataXmlFile);

            if (dataXml.IsAvailable)
            {
                try
                {
                    using (var s = await dataXml.OpenStreamForReadAsync())
                    {
                        // Reload to xd.
                        xd = XDocument.Load(s);
                    }
                }
                catch (Exception ex)
                {
                    xd = XDocument.Load("Assets/" + dataXmlFile);
                    throw new OldXmlException();
                }
            }
            else
            {
                xd = XDocument.Load("Assets/" + dataXmlFile);
                throw new OldXmlException();
            }

            return 0;
        }

        public async Task<int> reloadDataX()
        {
            var dataX = from data in xd.Descendants("Data")
                        select data;
            var geoDataX = from data in siteGeoXd.Descendants("Data")
                           select data;
            
            sitesDict.Clear();
            var isSiteUninit = sites.Count() == 0;
            foreach (var d in dataX.OrderBy(x => x.Element("County").Value))
            {
                var siteName = d.Descendants("SiteName").First().Value;
                var geoD = from gd in geoDataX
                           where gd.Descendants("SiteName").First().Value == siteName
                           select gd;

                var siteDict = d.Elements().ToDictionary(x => x.Name.LocalName, x => x.Value);
                sitesDict.Add(siteName, siteDict);

                // Add mode.
                if (isSiteUninit)
                {
                    sites.Add(new Site
                    {
                        siteName = siteName,
                        County = siteDict["County"],
                        Aqi = siteDict["AQI"],
                        Pm2_5 = siteDict["PM2.5"],
                        twd97Lat = double.Parse(geoD.Descendants("TWD97Lat").First().Value),
                        twd97Lon = double.Parse(geoD.Descendants("TWD97Lon").First().Value),
                    });
                }
                // Update mode.
                else
                {
                    var site = sites.Where(s => s.siteName == siteName).First();
                    site.Aqi = siteDict["AQI"];
                    site.Pm2_5 = siteDict["PM2.5"];
                }
            }
            updateMapIconsAndList("AQI");

            reloadSubscrSiteId();
            return 0;
        }

        public void updateMapIconsAndList(string aqName)
        {
            foreach (var site in sites)
            {
                var aqLevel = getAqLevel(site, aqName);
                site.CircleColor = aqColors[aqName][aqLevel];
                site.CircleText = site.siteName + "\n" + sitesDict[site.siteName][aqName];
                site.ListText = sitesDict[site.siteName][aqName];
            }
        }

        public async Task<int> loadCurrSite()
        {
            try
            {
                // Load the old site.
                XDocument loadOldXd = new XDocument();
                var loadOldXml = await ApplicationData.Current.LocalFolder.GetFileAsync("OldSite.xml");
                using (var s = await loadOldXml.OpenStreamForReadAsync())
                {
                    loadOldXd = XDocument.Load(s);
                }
                var oldSiteX = loadOldXd.Descendants("Data").First();
                oldSiteDict = oldSiteX.Elements().ToDictionary(x => x.Name.LocalName, x => x.Value);
                oldSite = new Site
                {
                    siteName = oldSiteX.Descendants("SiteName").First().Value,
                    Aqi = oldSiteX.Descendants("AQI").First().Value,
                    Pm2_5 = oldSiteX.Descendants("PM2.5").First().Value,
                };
            }
            catch (Exception ex)
            {
                // Ignore.
            }

            try
            {
                // Get new site from the setting.
                var newSiteName = (string)localSettings.Values["subscrSite"];
                var currSiteX = from d in xd.Descendants("Data")
                                where d.Descendants("SiteName").First().Value == newSiteName
                                select d;

                currSiteDict = currSiteX.Elements().ToDictionary(x => x.Name.LocalName, x => x.Value);
                currSite = new Site
                {
                    siteName = currSiteDict["SiteName"],
                    Aqi = currSiteDict["AQI"],
                    Pm2_5 = currSiteDict["PM2.5"]
                };

                // Save the current site as old site.
                XDocument saveOldXd = new XDocument();
                saveOldXd.Add(currSiteX);
                var saveOldXml = await ApplicationData.Current.LocalFolder.CreateFileAsync("OldSite.xml", CreationCollisionOption.ReplaceExisting);
                using (var s = await saveOldXml.OpenStreamForWriteAsync())
                {
                    saveOldXd.Save(s);
                }

                Site2Coll();
            }
            catch (Exception ex)
            {

            }
            return 0;
        }

        public void Site2Coll()
        {
            // Don't remove all elements by new.
            // Otherwise, data bindings would be problematic.
            currSiteViews.Clear();
            foreach (var k in fieldNames.Keys)
            {
                var aqLevel = getAqLevel(currSite, k);
                currSiteViews.Add(new string[]
                {
                    aqColors[k][aqLevel], // default border background color
                    fieldNames[k] + "\n" + currSiteDict[k],
                    "Black", // default text color
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
                subscrSiteId = value;
                NotifyPropertyChanged();
            }
        }

        public void reloadSubscrSiteId()
        {
            var subscrSiteName = (string)localSettings.Values["subscrSite"];
            var subscrSiteElem = from s in sites
                                 where s.siteName == subscrSiteName
                                 select s;
            SubscrSiteId = sites.IndexOf(subscrSiteElem.First());
        }

        public void updateLiveTile()
        {
            // create the instance of Tile Updater, which enables you to change the appearance of the calling app's tile
            var updater = TileUpdateManager.CreateTileUpdaterForApplication();
            // enables the tile to queue up to five notifications 
            updater.EnableNotificationQueue(true);
            updater.Clear();
            
            var pm2_5_Str = "PM 2.5：" + currSite.Pm2_5;
            var siteStr = "觀測站：" + currSite.siteName;
            var timeStr = currSiteDict["PublishTime"].Substring(11, 5);
            // get the XML content of one of the predefined tile templates, so that, you can customize it
            // create the wide template
            var wideContent = TileContentFactory.CreateTileWide310x150Text01();
            wideContent.TextHeading.Text = "空氣品質：" + currSiteDict["AQI"];
            wideContent.TextBody1.Text = fieldNames["Status"] + "：" + currSiteDict["Status"];
            wideContent.TextBody2.Text = pm2_5_Str;
            wideContent.TextBody3.Text = siteStr;
            wideContent.TextBody4.Text = "發佈時間：" + timeStr;
            //wideContent.Image.Src = "ms-appx:///Assets/Wide310x150Logo.scale-200.png";

            // create the square template and attach it to the wide template 
            var squareContent = TileContentFactory.CreateTileSquare150x150Text01();
            squareContent.TextHeading.Text = "AQI：" + currSiteDict["AQI"]; ;
            squareContent.TextBody1.Text = pm2_5_Str;
            squareContent.TextBody2.Text = siteStr;
            squareContent.TextBody3.Text = "時間：" + timeStr;
            wideContent.Square150x150Content = squareContent;

            // Create a new tile notification.
            updater.Update(new TileNotification(wideContent.GetXml()));
        }

        public void sendNotifications()
        {
            int aqi_LimitId = (int)localSettings.Values["Aqi_LimitId"];
            if (oldSiteDict["AQI"] != currSiteDict["AQI"] && aqiLimits.FindLastIndex(x => currSite.aqi_int <= x) > aqi_LimitId)
            {
                sendNotification("AQI: " + currSiteDict["AQI"], "AQI");
            }

            int pm2_5_LimitId = (int)localSettings.Values["Pm2_5_LimitId"];
            if (oldSiteDict["PM2.5"] != currSiteDict["PM2.5"] && pm2_5ConcensToId(currSite.pm2_5_int) > pm2_5_LimitId)
            {
                sendNotification("PM 2.5濃度: " + currSiteDict["PM2.5"], "PM2.5");
            }

#if DEBUG
            sendNotification("AQI: " + currSiteDict["AQI"], "AQI");
            sendNotification("PM 2.5濃度: " + currSiteDict["PM2.5"], "PM2.5");
#endif
        }

        public void sendNotification(string title, string tag)
        {
            var content = "觀測站: " + currSite.siteName;
            // Now we can construct the final toast content
            ToastContent toastContent = new ToastContent()
            {
                Visual = getNotifyVisual(title, content)
            };

            // And create the toast notification
            var toast = new ToastNotification(toastContent.GetXml());
            toast.ExpirationTime = DateTime.Now.AddHours(1);
            toast.Tag = tag;
            toast.Group = "wallPosts";
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        string logo = "Assets/StoreLogo.png";
        private ToastVisual getNotifyVisual(string title, string content)
        {
            // Construct the visuals of the toast
            return new ToastVisual()
            {
                BindingGeneric = new ToastBindingGeneric()
                {
                    Children =
                    {
                        new AdaptiveText()
                        {
                            Text = title
                        },

                        new AdaptiveText()
                        {
                            Text = content
                        }
                    },
                    AppLogoOverride = new ToastGenericAppLogo()
                    {
                        Source = logo,
                        HintCrop = ToastGenericAppLogoCrop.Circle
                    }
                }
            };
        }

        public int getAqLevel(Site site, string aqName)
        {
            var val = 0;
            int.TryParse(sitesDict[site.siteName][aqName], out val);
            var aqLevel = aqLimits[aqName].FindIndex(x => val <= x);
            if (aqLevel == -1)
            {
                aqLevel = aqLimits[aqName].Count;
            }
            return aqLevel;
        }

        public int pm2_5ConcensToId(int concens)
        {
            var i = 0;
            for (; i < pm2_5_concens.Count; i++)
            {
                if (concens <= pm2_5_concens[i])
                {
                    break;
                }
            }
            return i + 1;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
