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
using Windows.Devices.Geolocation;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Notifications;
using Windows.UI.Xaml.Media;

namespace Taq
{
    public class DownloadException : Exception
    {

    }

    public class OldXmlException : Exception
    {

    }

    public class TaqModel
    {
        public ApplicationDataContainer localSettings;
        // The AQ data XML filename.
        public const string dataXmlFile = "taqi.xml";
        public Uri source = new Uri("http://YourTaqServerIp/taq/" + dataXmlFile);
        public XDocument xd = new XDocument();
        public XDocument siteGeoXd = new XDocument();
        // Full sites AQ information in Dictionary. Converted from XML.
        public Dictionary<string, Dictionary<string, string>> sitesStrDict = new Dictionary<string, Dictionary<string, string>>();
        // Current (subscribed) site information in Dictionary.
        public Dictionary<string, string> currSiteStrDict;
        // The previous currSiteStrDict from previous download dataXmlFile.
        public Dictionary<string, string> oldSiteStrDict;

        // XML AQ names to ordinary AQ names.
        // The order of keys is meaningful!
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

        // AQ level limits and corresponding colors lists.
        // Notice: a color list has one more element than a limit list!
        public static List<double> defaultLimits = new List<double> { 0 };
        public static List<string> defaultColors = new List<string> { "#31cf00", "#31cf00" };

        public static List<string> dirtyColors = new List<string> { "#808080", "#808080" };

        public static List<double> aqiLimits = new List<double> { 50, 100, 150, 200, 300, 400, 500 };
        public static List<string> aqiBgColors = new List<string> { "#00ff00", "#ffff00", "#ff7e00", "#ff0000", "#800080", "#633300", "#633300", "#633300" };

        public static List<double> pm2_5Limits = new List<double> { 50.4, 100.4, 150.4, 200.4, 300.4, 400.4, 500.4 };
        public static List<double> pm10Limits = new List<double> { 54, 125, 254, 354, 424, 504, 604 };
        public static List<double> o3Limits = new List<double> { 60, 125, 164, 204, 404, 504, 604 };
        // 201, 202, ...
        public static List<double> o3_8hrLimits = new List<double> { 54, 70, 85, 105, 200, 201, 202 };
        public static List<double> coLimits = new List<double> { 4.4, 9.4, 12.4, 15.4, 30.4, 40.4, 50.4 };
        public static List<double> so2Limits = new List<double> { 35, 75, 185, 304, 604, 804, 1004 };
        public static List<double> no2Limits = new List<double> { 53, 100, 360, 649, 1249, 1649, 2049 };

        // Combine color lists into Dictinoary.
        public Dictionary<string, List<string>> aqColors = new Dictionary<string, List<string>>
        {
            { "PublishTime", defaultColors},
            { "SiteName", defaultColors },
            { "County", defaultColors},
            { "Pollutant", dirtyColors},
            { "AQI", aqiBgColors},
            { "Status", aqiBgColors},
            { "PM2.5", aqiBgColors},
            { "PM2.5_AVG", aqiBgColors},
            { "PM10", aqiBgColors},
            { "PM10_AVG", aqiBgColors},
            { "O3", aqiBgColors},
            { "O3_8hr", aqiBgColors},
            { "CO", aqiBgColors},
            { "CO_8hr", aqiBgColors},
            { "SO2", aqiBgColors},
            { "NO2", aqiBgColors},
            { "NOx", dirtyColors},
            { "NO", dirtyColors},
            { "WindSpeed", defaultColors},
            { "WindDirec", defaultColors},
        };

        // Combine limit lists into Dictinoary.
        public Dictionary<string, List<double>> aqLimits = new Dictionary<string, List<double>>
        {
            { "PublishTime", defaultLimits},
            { "SiteName", defaultLimits },
            { "County", defaultLimits},
            { "Pollutant", defaultLimits},
            { "AQI", aqiLimits},
            { "Status", aqiLimits},
            { "PM2.5", pm2_5Limits},
            { "PM2.5_AVG", pm2_5Limits},
            { "PM10", pm10Limits},
            { "PM10_AVG", pm10Limits},
            { "O3", o3Limits},
            { "O3_8hr", o3_8hrLimits},
            { "CO", coLimits},
            { "CO_8hr", coLimits},
            { "SO2", so2Limits},
            { "NO2", no2Limits},
            { "NOx", defaultLimits},
            { "NO", defaultLimits},
            { "WindSpeed", defaultLimits},
            { "WindDirec", defaultLimits},
        };

        // AQ name list for MainPage aqComboBox.
        // Don't replace it by aqLimits.Keys! Not all names are used in aqComboBox.
        public List<string> aqList = new List<string>
        {"AQI", "Status", "PM2.5", "PM2.5_AVG", "PM10", "PM10_AVG", "O3", "O3_8hr", "CO", "CO_8hr", "SO2", "NO2", "NOx", "NO", "WindSpeed", "WindDirec"};

        public TaqModel()
        {
            localSettings =
       ApplicationData.Current.LocalSettings;
            loadSiteGeoXml();
        }

        public async Task<int> downloadDataXml(bool confAwait = true)
        {
            // Download may fail, so we create a temp StorageFile.
            var dlFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("Temp" + dataXmlFile, CreationCollisionOption.ReplaceExisting).AsTask().ConfigureAwait(confAwait);

            BackgroundDownloader downloader = new BackgroundDownloader();
            DownloadOperation download = downloader.CreateDownload(source, dlFile);

            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

            cts.CancelAfter(5000);
            try
            {
                // Pass the token to the task that listens for cancellation.
                await download.StartAsync().AsTask(token).ConfigureAwait(confAwait);
                // file is downloaded in time
                // Copy download file to dataXmlFile.
                var dataXml = await ApplicationData.Current.LocalFolder.CreateFileAsync(dataXmlFile, CreationCollisionOption.ReplaceExisting);
                await dlFile.CopyAndReplaceAsync(dataXml).AsTask().ConfigureAwait(confAwait);
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

        private int loadSiteGeoXml()
        {
            //http://opendata.epa.gov.tw/ws/Data/AQXSite/?format=xml
            siteGeoXd = XDocument.Load("Assets/SiteGeo.xml");

            return 0;
        }

        // Reload air quality XML files.
        public async Task<int> loadAqXml(bool confAwait = true)
        {
            try
            {
                var dataXml = await ApplicationData.Current.LocalFolder.GetFileAsync(dataXmlFile).AsTask().ConfigureAwait(confAwait);
                using (var s = await dataXml.OpenStreamForReadAsync().ConfigureAwait(confAwait))
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

            return 0;
        }

        public int convertXDoc2Dict()
        {
            var dataX = from data in xd.Descendants("Data")
                        select data;
            var geoDataX = from data in siteGeoXd.Descendants("Data")
                           select data;

            sitesStrDict.Clear();
            foreach (var d in dataX.OrderBy(x => x.Element("County").Value))
            {
                var siteName = d.Descendants("SiteName").First().Value;
                var geoD = from gd in geoDataX
                           where gd.Descendants("SiteName").First().Value == siteName
                           select gd;

                var siteDict = d.Elements().ToDictionary(x => x.Name.LocalName, x => x.Value);
                var geoDict = geoD.Elements().ToDictionary(x => x.Name.LocalName, x => x.Value);
                siteDict.Add("TWD97Lat", geoDict["TWD97Lat"]);
                siteDict.Add("TWD97Lon", geoDict["TWD97Lon"]);
                sitesStrDict.Add(siteName, siteDict);
            }

            return 0;
        }

        public async Task<int> loadCurrSite(bool confAwait = true)
        {
            // Load the old site.
            XDocument loadOldXd = new XDocument();
            try
            {
                var loadOldXml = await ApplicationData.Current.LocalFolder.GetFileAsync("OldSite.xml").AsTask().ConfigureAwait(confAwait);
                using (var s = await loadOldXml.OpenStreamForReadAsync().ConfigureAwait(confAwait))
                {
                    loadOldXd = XDocument.Load(s);
                }
                var oldSiteX = loadOldXd.Descendants("Data").First();
                oldSiteStrDict = oldSiteX.Elements().ToDictionary(x => x.Name.LocalName, x => x.Value);
            }
            catch (Exception ex)
            {
                // Ignore.
            }

            // Get new site from the setting.
            var newSiteName = (string)localSettings.Values["subscrSite"];
            var currSiteX = from d in xd.Descendants("Data")
                            where d.Descendants("SiteName").First().Value == newSiteName
                            select d;

            currSiteStrDict = currSiteX.Elements().ToDictionary(x => x.Name.LocalName, x => x.Value);

            // Save the current site as old site.
            XDocument saveOldXd = new XDocument();
            saveOldXd.Add(currSiteX);
            try
            {
                var saveOldXml = await ApplicationData.Current.LocalFolder.CreateFileAsync("OldSite.xml", CreationCollisionOption.ReplaceExisting).AsTask().ConfigureAwait(confAwait);
                using (var s = await saveOldXml.OpenStreamForWriteAsync().ConfigureAwait(confAwait))
                {
                    saveOldXd.Save(s);
                }
            }
            catch (Exception)
            {
                // Ignore.
            }

            return 0;
        }

        public void updateLiveTile()
        {
            // create the instance of Tile Updater, which enables you to change the appearance of the calling app's tile
            var updater = TileUpdateManager.CreateTileUpdaterForApplication();
            // enables the tile to queue up to five notifications 
            updater.EnableNotificationQueue(true);
            updater.Clear();

            var aqiStr = "AQI：" + currSiteStrDict["AQI"];
            var pm2_5_Str = "PM 2.5：" + currSiteStrDict["PM2.5"];
            var siteStr = "觀測站：" + currSiteStrDict["SiteName"];
            var timeStr = currSiteStrDict["PublishTime"].Substring(11, 5);
            // get the XML content of one of the predefined tile templates, so that, you can customize it
            // Large template
            var statusStr = fieldNames["Status"] + "：" + currSiteStrDict["Status"];
            var largeContent = TileContentFactory.CreateTileSquare310x310Text09();
            largeContent.TextHeadingWrap.Text = statusStr;
            largeContent.TextHeading1.Text = siteStr;
            largeContent.TextHeading2.Text = "發佈時間：" + timeStr;
            largeContent.TextBody1.Text = aqiStr;
            largeContent.TextBody2.Text = pm2_5_Str;

            // create the wide template
            var wideContent = TileContentFactory.CreateTileWide310x150Text01();
            wideContent.TextHeading.Text = statusStr;
            wideContent.TextBody1.Text = siteStr;
            wideContent.TextBody2.Text = "發佈時間：" + timeStr;
            wideContent.TextBody3.Text = aqiStr;
            wideContent.TextBody4.Text = pm2_5_Str;
            //wideContent.Image.Src = "ms-appx:///Assets/Wide310x150Logo.scale-200.png";

            // create the square template and attach it to the wide template 
            var squareContent = TileContentFactory.CreateTileSquare150x150Text01();
            squareContent.TextHeading.Text = "AQI：" + currSiteStrDict["AQI"]; ;
            squareContent.TextBody1.Text = pm2_5_Str;
            squareContent.TextBody2.Text = siteStr;
            squareContent.TextBody3.Text = "時間：" + timeStr;

            largeContent.Wide310x150Content = wideContent;
            wideContent.Square150x150Content = squareContent;

            // Create a new tile notification.
            updater.Update(new TileNotification(largeContent.GetXml()));
        }

        public void sendNotifications()
        {
            var aqi_Limit = (double)localSettings.Values["Aqi_Limit"];
            if (oldSiteStrDict["AQI"] != currSiteStrDict["AQI"] && getAqVal(currSiteStrDict["SiteName"], "AQI") > aqi_Limit)
            {
                sendNotification("AQI: " + currSiteStrDict["AQI"], "AQI");
            }

            var pm2_5_Limit = (double)localSettings.Values["Pm2_5_Limit"];
            if (oldSiteStrDict["PM2.5"] != currSiteStrDict["PM2.5"] && getAqVal(currSiteStrDict["SiteName"], "PM2.5") > pm2_5_Limit)
            {
                sendNotification("PM 2.5濃度: " + currSiteStrDict["PM2.5"], "PM2.5");
            }

#if DEBUG
            sendNotification("AQI: " + currSiteStrDict["AQI"], "AQI");
            sendNotification("PM 2.5濃度: " + currSiteStrDict["PM2.5"], "PM2.5");
#endif
        }

        public void sendNotification(string title, string tag)
        {
            var content = "觀測站: " + currSiteStrDict["SiteName"];
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
                        Source = "Assets/logos/PackageLogo.png",
                        HintCrop = ToastGenericAppLogoCrop.None
                    }
                }
            };
        }

        public double getAqVal(string siteName, string aqName)
        {
            double val = 0;
            if(aqName == "Status")
            {
                aqName = "AQI";
            }
            double.TryParse(sitesStrDict[siteName][aqName], out val);
            return val;
        }

        public int getAqLevel(string siteName, string aqName)
        {
            var val = getAqVal(siteName, aqName);
            var aqLevel = aqLimits[aqName].FindIndex(x => val <= x);
            if (aqLevel == -1)
            {
                aqLevel = aqLimits[aqName].Count;
            }
            return aqLevel;
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
