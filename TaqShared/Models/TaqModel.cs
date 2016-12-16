﻿using Microsoft.Toolkit.Uwp.Notifications;
using NotificationsExtensions.TileContent;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using TaqShared;
using TaqShared.ModelViews;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Notifications;
using Windows.UI.StartScreen;
using Windows.UI.Xaml.Controls;
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
        public Uri source = new Uri("https://YourTaqServerDomainName/" + dataXmlFile);
        public XDocument xd = new XDocument();
        public XDocument siteGeoXd = new XDocument();
        // Full sites AQ information in Dictionary. Converted from XML.
        public Dictionary<string, Dictionary<string, string>> sitesStrDict = new Dictionary<string, Dictionary<string, string>>();
        // Current (subscribed) site information in Dictionary.
        public Dictionary<string, string> currSiteStrDict;
        // The previous currSiteStrDict from previous download dataXmlFile.
        public Dictionary<string, Dictionary<string, string>> oldSitesStrDict = new Dictionary<string, Dictionary<string, string>>();

        public List<string> subscrSiteList = new List<string>();
        public XDocument subscrXd;

        // XML AQ names to ordinary AQ names.
        // The order of keys is meaningful!
        // The display order of AQ items in Home.xaml follows this order of keys.
        public Dictionary<string, string> fieldNames = new Dictionary<string, string>
        {
            { "PublishTime", "發佈時間"},
            { "SiteName", "觀測站" },
            { "County", "縣市"},
            { "Status", "狀態"},
            { "AQI", "空氣品質指標"},
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

        public Dictionary<string, string> shortStatusDict = new Dictionary<string, string>
        {
            { "對敏感族群不良", "敏感"},
            { "對所有族群不良", "所有"},
            { "非常不良", "非常"},
        };

        // AQ level limits and corresponding colors lists.
        // Notice: a color list has one more element than a limit list!
        public static List<double> defaultLimits = new List<double> { 0 };
        public static List<string> defaultColors = new List<string> { "#31cf00", "#31cf00" };

        public static List<string> dirtyColors = new List<string> { "#C0C0C0", "#C0C0C0" };

        public static List<double> aqiLimits = new List<double> { 50, 100, 150, 200, 300, 400, 500 };
        public static List<string> aqiBgColors = new List<string> { "#00ff00", "#ffff00", "#ff7e00", "#ff0000", "#800080", "#633300", "#633300", "#633300" };

        public static List<double> pm2_5Limits = new List<double> { 15.4, 35.4, 54.4, 150.4, 250.4, 350.4, 500.4 };
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

        public async Task<int> downloadDataXml(bool confAwait = true, int timeout = 10000)
        {
            // Download may fail, so we create a temp StorageFile.
            var dlFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("Temp" + dataXmlFile, CreationCollisionOption.ReplaceExisting).AsTask().ConfigureAwait(confAwait);

            BackgroundDownloader downloader = new BackgroundDownloader();
            DownloadOperation download = downloader.CreateDownload(source, dlFile);

            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

#if DEBUG
            timeout = 2000;
#endif

            cts.CancelAfter(timeout);
            try
            {
                // Pass the token to the task that listens for cancellation.
                await download.StartAsync().AsTask(token).ConfigureAwait(confAwait);
                // file is downloaded in time
                // Copy download file to dataXmlFile.
                var dataXml = await ApplicationData.Current.LocalFolder.GetFileAsync(dataXmlFile);
                var oldDataXml = await ApplicationData.Current.LocalFolder.CreateFileAsync("Old" + dataXmlFile, CreationCollisionOption.ReplaceExisting);
                await dataXml.CopyAndReplaceAsync(oldDataXml);
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
                var statusStr = siteDict["Status"];
                // Shorten long status strings for map icons.
                if (statusStr.Length > 2)
                {
                    siteDict["Status"] = shortStatusDict[statusStr];
                }
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
                var loadOldXml = await ApplicationData.Current.LocalFolder.GetFileAsync("Old" + dataXmlFile).AsTask().ConfigureAwait(confAwait);

                using (var s = await loadOldXml.OpenStreamForReadAsync().ConfigureAwait(confAwait))
                {
                    loadOldXd = XDocument.Load(s);
                }
            }
            catch (Exception ex)
            {
                loadOldXd = XDocument.Load("Assets/Old" + dataXmlFile);
            }

            var oldDataX = from data in loadOldXd.Descendants("Data")
                           select data;
            oldSitesStrDict.Clear();
            foreach (var d in oldDataX.OrderBy(x => x.Element("County").Value))
            {
                var siteName = d.Descendants("SiteName").First().Value;
                oldSitesStrDict.Add(siteName, d.Elements().ToDictionary(x => x.Name.LocalName, x => x.Value));
            }

            // Get new site from the setting.
            var newSiteName = (string)localSettings.Values["subscrSite"];
            var currSiteX = from d in xd.Descendants("Data")
                            where d.Descendants("SiteName").First().Value == newSiteName
                            select d;

            currSiteStrDict = currSiteX.Elements().ToDictionary(x => x.Name.LocalName, x => x.Value);

            return 0;
        }

        private static string subscrSiteXml = "SubscrSites.xml";
        // Reload subscribed site XML files.
        public async Task<int> loadSubscrSiteXml(bool confAwait = true)
        {
            try
            {
                var dataXml = await ApplicationData.Current.LocalFolder.GetFileAsync(subscrSiteXml).AsTask().ConfigureAwait(confAwait);
                using (var s = await dataXml.OpenStreamForReadAsync().ConfigureAwait(confAwait))
                {
                    // Reload to xd.
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
            largeContent = await lts(currSiteStrDict["SiteName"]);
            // Create a new tile notification.
            updater.Update(new TileNotification(largeContent.GetXml()));

            foreach (var siteName in subscrSiteList)
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
            // not implemented.
            var largeContent = TileContentFactory.CreateTileSquare310x310Image();
            largeContent.Branding = NotificationsExtensions.TileContent.TileBranding.None;

            // not implemented.
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
            squareContent.TextHeading.Text = "AQI：" + currSiteStrDict["AQI"];
            squareContent.TextBody1.Text = pm2_5_Str;
            squareContent.TextBody2.Text = siteStr;
            squareContent.TextBody3.Text = "時間：" + timeStr;
            squareContent.Branding = NotificationsExtensions.TileContent.TileBranding.None;

            // Small tile is not implemented.

            largeContent.Wide310x150Content = wideContent;
            wideContent.Square150x150Content = squareContent;

            return largeContent;
        }

        public async Task<int> genTileImages(string siteName)
        {
            var aqName = "AQI";
            var aqLevel = getAqLevel(siteName, aqName);
            // Remove '#'.
            var rectColorStr = aqColors[aqName][aqLevel].Substring(1);
            var r = (byte)Convert.ToUInt32(rectColorStr.Substring(0, 2), 16);
            var g = (byte)Convert.ToUInt32(rectColorStr.Substring(2, 2), 16);
            var b = (byte)Convert.ToUInt32(rectColorStr.Substring(4, 2), 16);
            var bgColor = new SolidColorBrush(Color.FromArgb(0xFF, r, g, b));

            var timeStr = sitesStrDict[siteName]["PublishTime"].Substring(11, 5);

            var wideTile = new WideTile();
            // Wide tile
            wideTile.topTxt.Text = siteName + " " + timeStr;
            wideTile.medVal1.Text = sitesStrDict[siteName]["AQI"];
            wideTile.medVal2.Text = sitesStrDict[siteName]["PM2.5"];
            wideTile.medVal3.Text = sitesStrDict[siteName]["PM10"];
            wideTile.border.Background = bgColor;

            var medTile = new MedTile();
            // Med tile
            medTile.topTxt.Text = sitesStrDict[siteName]["SiteName"] + aqName;
            medTile.medTxt.Text = sitesStrDict[siteName][aqName];
            medTile.downTxt.Text = timeStr;
            medTile.border.Background = bgColor;

            // Small tile
            var smallTile = new SmallTile();
            smallTile.topTxt.Text = sitesStrDict[siteName]["SiteName"];
            smallTile.downTxt.Text = timeStr;
            smallTile.border.Background = bgColor;

            // Set text color.
            var textColor = aqLevel > 3 ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);
            foreach (var t in new List<TextBlock> { smallTile.topTxt, smallTile.downTxt, medTile.topTxt, medTile.medTxt, medTile.downTxt, wideTile.topTxt, wideTile.medTxt1, wideTile.medVal1, wideTile.medTxt2, wideTile.medVal2, wideTile.medTxt3, wideTile.medVal3 })
            {
                t.Foreground = textColor;
            }
            await StaticTaqModelView.saveUi2Png(siteName + "SmallTile.png", smallTile);
            await StaticTaqModelView.saveUi2Png(siteName + "MedTile.png", medTile);
            await StaticTaqModelView.saveUi2Png(siteName + "WideTile.png", wideTile);
            return 0;
        }

        public void sendSubscrSitesNotifications()
        {
            sendNotifications(currSiteStrDict["SiteName"]);
            foreach (var siteName in subscrSiteList)
            {
                sendNotifications(siteName);
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
            // Now we can construct the final toast content
            ToastContent toastContent = new ToastContent()
            {
                Visual = getNotifyVisual(title, content)
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
            if (aqName == "Status")
            {
                aqName = "AQI";
            }
            return getValidAqVal(sitesStrDict[siteName][aqName]);
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
    }
}
