using Microsoft.QueryStringDotNET;
using Microsoft.Toolkit.Uwp.Notifications;
using NotificationsExtensions.TileContent;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using TaqShared;
using TaqShared.Models;
using TaqShared.ModelViews;
using Windows.Devices.Geolocation;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Notifications;
using Windows.UI.StartScreen;
using Windows.UI.Xaml.Media;

namespace Taq
{
    public class OldXmlException : Exception
    {

    }

    abstract public class TaqModel
    {
        public ApplicationDataContainer localSettings;
        public Uri source = new Uri(Params.uriHost + "aqJsonDb");
        public XDocument siteGeoXd = new XDocument();
        public Dictionary<string, GpsPoint> sitesGeoDict = new Dictionary<string, GpsPoint>();
        // Full sites AQ information in Dictionary. Converted from XML.
        public Dictionary<string, Dictionary<string, string>> sitesStrDict = new Dictionary<string, Dictionary<string, string>>();
        // The previous siteStrDict from previous download aqDbFile.
        public Dictionary<string, Dictionary<string, string>> oldSitesStrDict = new Dictionary<string, Dictionary<string, string>>();
        // Subscribed sites list. subscrSiteList[0] stands for MainSite!
        public List<string> subscrSiteList = new List<string>();
        public XDocument subscrXd;
        public DateTime lastUpdateTime;

        // AQ name list for MainPage aqComboBox.
        // Don't replace it by aqLimits.Keys! Not all names are used in aqComboBox.
        public List<string> aqList = new List<string> { "ShortStatus", "AQI", "PM2.5", "PM2.5_AVG", "PM10", "PM10_AVG", "O3", "O3_8hr", "CO", "CO_8hr", "SO2", "NO2", "NOx", "NO", "WindSpeed", "WindDirec" };

        // AQ name list for AqHistories.
        public List<string> aqHistNames = new List<string> { "AQI", "PM2.5", "PM2.5_AVG", "PM10", "PM10_AVG", "O3", "O3_8hr", "CO", "CO_8hr", "SO2", "NO2", "NOx", "NO", "WindSpeed", "WindDirec" };

        public TaqModel()
        {
            localSettings =
       ApplicationData.Current.LocalSettings;
            loadSiteGeoXml();
        }

        public async Task<int> downloadAqData(int timeout = 10000)
        {
            await StaticTaqModel.downloadAndBackup(source, Params.aqDbFile, timeout);
            // Download AQ histories for subscribed sites.
            foreach (var siteName in subscrSiteList)
            {
                await StaticTaqModel.downloadAndBackup(
                    new Uri(Params.uriHost + Params.aqHistTabName + $"?siteName={siteName}"),
                    siteName + Params.aqHistFile,
                    timeout);
            }
            return 0;
        }

        private int loadSiteGeoXml()
        {
            //http://opendata.epa.gov.tw/ws/Data/AQXSite/?format=xml
            siteGeoXd = XDocument.Load("Assets/SiteGeo.xml");
            return 0;
        }

        abstract public Task<int> loadAq2Dict();

        abstract public Task<int> loadMainSite(string newMainSite);

        private static string subscrSiteXml = "SubscrSites.xml";
        // Reload subscribed site XML files.
        public async Task<int> loadSubscrSiteXml()
        {
            try
            {
                var dataXml = await ApplicationData.Current.LocalFolder.GetFileAsync(subscrSiteXml);
                using (var s = await dataXml.OpenStreamForReadAsync())
                {
                    subscrXd = XDocument.Load(s);
                }
            }
            catch (Exception ex)
            {
                subscrXd = XDocument.Load("Assets/" + subscrSiteXml);
            }

            subscrSiteList.Clear();
            foreach (var s in subscrXd.Descendants("SiteName"))
            {
                subscrSiteList.Add(s.Value);
            }

            // Insert MainSite.
            subscrSiteList.Insert(0, (string)localSettings.Values["MainSite"]);

            return 0;
        }

        public async Task<int> updateSubscrSiteListByMainSite(string newMainSite)
        {
            subscrSiteList[0] = newMainSite;
            return 0;
        }

        public async Task<int> saveSubscrXd()
        {
            var subscrSitesXml = await ApplicationData.Current.LocalFolder.CreateFileAsync("SubscrSites.xml", CreationCollisionOption.ReplaceExisting);
            using (var s = await subscrSitesXml.OpenStreamForWriteAsync())
            {
                subscrXd.Save(s);
            }
            return 0;
        }

        delegate Task<ISquare310x310TileNotificationContent> LiveTileSty(string siteName);
        public async Task<int> updateLiveTile()
        {
            // create the instance of Tile Updater, which enables you to change the appearance of the calling app's tile
            var updater = TileUpdateManager.CreateTileUpdaterForApplication();
            // enables the tile to queue up to five notifications 
            updater.EnableNotificationQueue(true);
            updater.Clear();
            ISquare310x310TileNotificationContent largeContent;
            LiveTileSty lts;

            if ((bool)localSettings.Values["TileClearSty"])
            {
                lts = new LiveTileSty(clearLiveTiles);
            }
            else
            {
                lts = new LiveTileSty(detailedLiveTiles);
            }
            largeContent = await lts(subscrSiteList[0]);
            // Create a new tile notification.
            updater.Update(new TileNotification(largeContent.GetXml()));

            foreach (var siteName in subscrSiteList.GetRange(1, subscrSiteList.Count - 1))
            {
                if (!SecondaryTile.Exists(siteName))
                {
                    continue;
                }
                var updater2 = TileUpdateManager.CreateTileUpdaterForSecondaryTile(siteName);
                updater2.EnableNotificationQueue(true);
                updater2.Clear();
                largeContent = await lts(siteName);
                updater2.Update(new TileNotification(largeContent.GetXml()));
            }

            return 0;
        }

        public async Task<ISquare310x310TileNotificationContent> clearLiveTiles(string siteName)
        {
            await genTileImages(siteName);
            // Large tile.
            var largeContent = TileContentFactory.CreateTileSquare310x310Image();
            largeContent.Image.Src = $"ms-appdata:///local/{siteName}LargeTile.png";
            largeContent.Branding = NotificationsExtensions.TileContent.TileBranding.None;

            // Wide tile.
            var wideContent = TileContentFactory.CreateTileWide310x150Image();
            wideContent.Image.Src = $"ms-appdata:///local/{siteName}WideTile.png";
            wideContent.Branding = NotificationsExtensions.TileContent.TileBranding.None;

            // Square tile.
            var squareContent = TileContentFactory.CreateTileSquare150x150Image();
            squareContent.Image.Src = $"ms-appdata:///local/{siteName}MedTile.png";
            squareContent.Branding = NotificationsExtensions.TileContent.TileBranding.None;

            // Smaill tile.
            var smallContent = TileContentFactory.CreateTileSquare71x71Image();
            smallContent.Image.Src = $"ms-appdata:///local/{siteName}SmallTile.png";
            smallContent.Branding = NotificationsExtensions.TileContent.TileBranding.None;

            largeContent.Wide310x150Content = wideContent;
            wideContent.Square150x150Content = squareContent;
            squareContent.Square71x71Content = smallContent;
            return largeContent;
        }

        public async Task<ISquare310x310TileNotificationContent> detailedLiveTiles(string siteName)
        {
            var aqiStr = "AQI：" + sitesStrDict[siteName]["AQI"];
            var pm2_5_Str = "PM 2.5：" + sitesStrDict[siteName]["PM2.5"];
            var siteStr = "觀測站：" + sitesStrDict[siteName]["SiteName"];
            var timeStr = sitesStrDict[siteName]["PublishTime"].Substring(11, 5);
            // get the XML content of one of the predefined tile templates, so that, you can customize it
            // Large template
            var statusStr = StaticTaqModel.fieldNames["Status"] + "：" + sitesStrDict[siteName]["Status"];
            var largeContent = TileContentFactory.CreateTileSquare310x310Text09();
            largeContent.TextHeadingWrap.Text = statusStr;
            largeContent.TextHeading1.Text = siteStr;
            largeContent.TextHeading2.Text = "發佈時間：" + timeStr;
            largeContent.TextBody1.Text = aqiStr;
            largeContent.TextBody2.Text = pm2_5_Str;
            largeContent.Branding = NotificationsExtensions.TileContent.TileBranding.None;

            // create the wide template
            var wideContent = TileContentFactory.CreateTileWide310x150Text01();
            wideContent.TextHeading.Text = statusStr;
            wideContent.TextBody1.Text = siteStr;
            wideContent.TextBody2.Text = "發佈時間：" + timeStr;
            wideContent.TextBody3.Text = aqiStr;
            wideContent.TextBody4.Text = pm2_5_Str;
            wideContent.Branding = NotificationsExtensions.TileContent.TileBranding.None;

            // create the square template and attach it to the wide template
            var squareContent = TileContentFactory.CreateTileSquare150x150Text01();
            squareContent.TextHeading.Text = aqiStr;
            squareContent.TextBody1.Text = pm2_5_Str;
            squareContent.TextBody2.Text = siteStr;
            squareContent.TextBody3.Text = "時間：" + timeStr;
            squareContent.Branding = NotificationsExtensions.TileContent.TileBranding.None;

            largeContent.Wide310x150Content = wideContent;
            wideContent.Square150x150Content = squareContent;

            return largeContent;
        }

        public async Task<int> genTileImages(string siteName)
        {
            // Get colors by AQI.
            var aqName = aqHistNames[(int)localSettings.Values["TileBackColorAqId"]];
            var aqLevel = getAqLevel(siteName, aqName);
            // Remove '#'.
            var rectColorStr = StaticTaqModel.aqColors[aqName][aqLevel].Substring(1);
            var r = (byte)Convert.ToUInt32(rectColorStr.Substring(0, 2), 16);
            var g = (byte)Convert.ToUInt32(rectColorStr.Substring(2, 2), 16);
            var b = (byte)Convert.ToUInt32(rectColorStr.Substring(4, 2), 16);
            var bgColor = new SolidColorBrush(Color.FromArgb(0xFF, r, g, b));
            var textColor = StaticTaqModelView.getTextColor(aqLevel);

            // Extract time.
            var dateStr = sitesStrDict[siteName]["PublishTime"].Substring(5, 5).Replace("-", "/");
            var timeStr = sitesStrDict[siteName]["PublishTime"].Substring(11, 5);
            var aqiStr = sitesStrDict[siteName]["AQI"];
            var pm2_5_Str = sitesStrDict[siteName]["PM2.5"];
            var pm10_Str = sitesStrDict[siteName]["PM10"];

            // Small tile
            var smallTile = new SmallTile(textColor);
            smallTile.topTxt.Text = siteName;
            smallTile.downTxt.Text = timeStr;
            smallTile.border.Background = bgColor;

            // Med tile
            var medTile = new MedTile(textColor);
            medTile.topTxt.Text = siteName;
            medTile.topVal.Text = timeStr;
            medTile.medVal.Text = aqiStr;
            medTile.downVal.Text = pm2_5_Str;
            medTile.border.Background = bgColor;

            // Wide tile
            var wideTile = new WideTile(textColor);
            wideTile.topTxt.Text = siteName + "-" + dateStr + "-" + timeStr;
            wideTile.medVal1.Text = aqiStr;
            wideTile.medVal2.Text = pm2_5_Str;
            wideTile.medVal3.Text = pm10_Str;
            wideTile.border.Background = bgColor;

            // Large tile
            var largeTile = new LargeTile(textColor);
            largeTile.val1.Text = siteName;
            largeTile.val2.Text = sitesStrDict[siteName]["ShortStatus"];
            largeTile.val3_1.Text = dateStr;
            largeTile.val3_2.Text = timeStr;
            largeTile.val4.Text = aqiStr;
            largeTile.val5.Text = pm2_5_Str;
            largeTile.val6.Text = pm10_Str;
            largeTile.val7.Text = sitesStrDict[siteName]["O3"];
            largeTile.val8.Text = sitesStrDict[siteName]["CO"];
            largeTile.val9.Text = sitesStrDict[siteName]["SO2"];
            largeTile.border.Background = bgColor;

            await StaticTaqModelView.saveUi2Png(siteName + "SmallTile.png", smallTile);
            await StaticTaqModelView.saveUi2Png(siteName + "MedTile.png", medTile);
            await StaticTaqModelView.saveUi2Png(siteName + "WideTile.png", wideTile);
            await StaticTaqModelView.saveUi2Png(siteName + "LargeTile.png", largeTile);
            return 0;
        }

        public void sendSubscrSitesNotifications()
        {
            sendNotifications(subscrSiteList[0]);
            if ((bool)localSettings.Values["SecondSitesNotify"])
            {
                foreach (var siteName in subscrSiteList.GetRange(1, subscrSiteList.Count - 1))
                {
                    sendNotifications(siteName);
                }
            }
        }

        public void sendNotifications(string siteName)
        {
            var warnStateChangeMode = (bool)localSettings.Values["WarnStateChangeMode"];
            foreach (var aqName in new List<string> { "AQI", "PM2.5" })
            {
                var aqi_Limit = (double)localSettings.Values[aqName + "_Limit"];
                var currAqi = getValidAqVal(sitesStrDict[siteName][aqName]);
                var oldAqi = getValidAqVal(oldSitesStrDict[siteName][aqName]);

                var isAqiOverWarnLevel = (oldAqi != currAqi && currAqi > aqi_Limit);

                var isWarnStateChanged = (oldAqi <= aqi_Limit && aqi_Limit < currAqi) ||
                    (oldAqi > aqi_Limit && aqi_Limit >= currAqi);

                if ((!warnStateChangeMode && isAqiOverWarnLevel) || (warnStateChangeMode && isWarnStateChanged))
                {
                    sendNotification(siteName, aqName);
                }
            }
        }

        public void sendNotification(string siteName, string aqName)
        {
            var title = aqName + ": " + sitesStrDict[siteName][aqName];
            var content = "觀測站: " + siteName;
            var launch = new QueryString() {
                { "siteName", siteName },
            }.ToString();
            // Now we can construct the final toast content
            ToastContent toastContent = new ToastContent()
            {
                Visual = getNotifyVisual(title, content),
                Launch = launch
            };

            // And create the toast notification
            var toast = new ToastNotification(toastContent.GetXml());
            toast.ExpirationTime = DateTime.Now.AddHours(1);
            toast.Tag = siteName + aqName;
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

        public double getValidAqVal(string aqValStr)
        {
            double val = 0;
            double.TryParse(aqValStr, out val);
            return val;
        }

        public double getAqVal(string siteName, string aqName)
        {
            if (aqName == "Status" || aqName == "ShortStatus")
            {
                aqName = "AQI";
            }
            return getValidAqVal(sitesStrDict[siteName][aqName]);
        }

        public int getAqLevel(string siteName, string aqName)
        {
            var val = getAqVal(siteName, aqName);
            return StaticTaqModel.getAqLevel(aqName, val);
        }

        public GeolocationAccessStatus locAccStat;
        public Geolocator geoLoc;
        public string nearestSite;
        public async Task<int> findNearestSite()
        {
            try
            {

                geoLoc = new Geolocator { ReportInterval = 2000 };
                var pos = await geoLoc.GetGeopositionAsync();
                var p = pos.Coordinate.Point;
                var gpsPos = new GpsPoint { twd97Lat = p.Position.Latitude, twd97Lon = p.Position.Longitude };

                var dists = new List<double>();
                foreach (var s in sitesGeoDict)
                {
                    dists.Add(StaticTaqModel.posDist(gpsPos, s.Value));
                }
                var minId = dists.FindIndex(v => v == dists.Min());
                nearestSite = sitesGeoDict.Keys.ToList()[minId];
            }
            catch (Exception ex)
            {
                throw new Exception("定位最近觀測站失敗！");
            }
            return 0;
        }

        public string getLastUpdateTime()
        {
            return lastUpdateTime.ToString("HH:mm:ss tt");
        }
    }
}
