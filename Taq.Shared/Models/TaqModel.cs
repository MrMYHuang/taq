using Microsoft.QueryStringDotNET;
using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NotificationsExtensions.TileContent;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Web.Http;
using Windows.Web.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Taq.Shared;
using Taq.Shared.Models;
using Taq.Shared.ViewModels;
using Taq.Shared.Views;
using Windows.ApplicationModel.Resources;
using Windows.Data.Json;
using Windows.Devices.Geolocation;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Notifications;
using Windows.UI.StartScreen;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Media;
using Windows.Storage.Streams;
using Windows.Services.Maps;

namespace Taq.Shared.Models
{
    public class OldXmlException : Exception
    {

    }

    abstract public class TaqModel
    {
        public ApplicationDataContainer localSettings;
        public HttpClient hc = new HttpClient();
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
        public ResourceLoader resLoader = new ResourceLoader();

        // Supported languages.
        public List<string> langList = new List<string> { "zh-TW", "en-US" };

        // AQ name list for AqList and AqSiteMap aqComboBox.
        // Don't replace it by aqLimits.Keys! Not all names are used in aqComboBox.
        public List<string> aqList = new List<string> { "ShortStatus", "publishtime", "aqi", "PM2.5", "PM2.5_AVG", "PM10", "PM10_AVG", "O3", "O3_8hr", "CO", "CO_8hr", "SO2", "NO2", "NOx", "NO", "WIND_SPEED", "WIND_DIREC" };

        // AQ name list for AqHistories.
        public List<string> aqHistNames = new List<string> { "aqi", "PM2.5", "PM2.5_AVG", "PM10", "PM10_AVG", "O3", "O3_8hr", "CO", "CO_8hr", "SO2", "NO2", "NOx", "NO", "WIND_SPEED", "WIND_DIREC" };

        public TaqModel()
        {
            localSettings = ApplicationData.Current.LocalSettings;

            hc.DefaultRequestHeaders.Accept.Add(new HttpMediaTypeWithQualityHeaderValue("application/json"));
            hc.DefaultRequestHeaders.AcceptEncoding.Add(new HttpContentCodingWithQualityHeaderValue("utf-8"));

            loadSiteGeoXml();
        }

        public async Task<int> downloadAqData(int timeout = 10000)
        {
            await downloadAndBackup(source, Params.aqDbFile, timeout);
            // Download AQ histories for subscribed sites.
            foreach (var siteName in subscrSiteList)
            {
                await downloadAndBackup(
                    new Uri(Params.uriHost + Params.aqHistTabName + $"?siteName={siteName}"),
                    siteName + Params.aqHistFile,
                    timeout);
            }
            return 0;
        }

        public async Task<int> downloadAndBackup(Uri source, string dstFile, int timeout = 10000)
        {
            HttpResponseMessage resMsg;
            string resStr;
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;
            cts.CancelAfter(timeout);
            try
            {
                // Pass the token to the task that listens for cancellation.
                resMsg = await httpClientPost(source, getUidPwdJson(), token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                // timeout is reached, downloadOperation is cancled
                throw new Exception(resLoader.GetString("downloadTimeout"));
            }
            finally
            {
                // Releases all resources of cts
                cts.Dispose();
            }

            using (var r = await resMsg.Content.ReadAsInputStreamAsync())
            {
                var sr = new StreamReader(r.AsStreamForRead());
                resStr = sr.ReadToEnd();

            }
            if (!resMsg.IsSuccessStatusCode)
                throw new Exception(resLoader.GetString("taqServerDown") + resStr);

            // file is downloaded in time, save to temp file.
            StorageFile tempSf;
            var jVal = JsonValue.Parse(resStr);
            var jRes = jVal.GetObject();
            var err = jRes["error"].GetString();
            if (err != "")
            {
                throw new Exception(err);
            }
            else
            {
                tempSf = await ApplicationData.Current.LocalFolder.CreateFileAsync("Temp" + dstFile, CreationCollisionOption.ReplaceExisting);
                // Write download data to dstSf.
                using (var s = await tempSf.OpenStreamForWriteAsync())
                {
                    using (var sw = new StreamWriter(s))
                    {
                        sw.Write(resStr);
                    }
                }
            }

            // Backup old file and copy temp file.
            StorageFile dstSf;
            try
            {
                dstSf = await ApplicationData.Current.LocalFolder.GetFileAsync(dstFile);
                // Backup old file.
                var oldAqDbSf = await ApplicationData.Current.LocalFolder.CreateFileAsync("Old" + dstFile, CreationCollisionOption.ReplaceExisting);
                await dstSf.CopyAndReplaceAsync(oldAqDbSf);
            }
            catch (Exception ex)
            {
                // Original file not exist.
                Debug.WriteLine(ex.Message);
                dstSf = await ApplicationData.Current.LocalFolder.CreateFileAsync(dstFile, CreationCollisionOption.ReplaceExisting);
            }
            // Copy download file to dstFile.
            await tempSf.CopyAndReplaceAsync(dstSf);
            return 0;
        }

        public JObject getUidPwdJson()
        {
            var jo = new JObject();
            jo.Add("uid", (string)localSettings.Values["UserId"]);
            jo.Add("pwd", (string)localSettings.Values["UserPwd"]);
            return jo;
        }

        public async Task<HttpResponseMessage> httpClientPost(Uri uri, JObject jPost, CancellationToken ct)
        {
            var content = new HttpStringContent(JsonConvert.SerializeObject(jPost), UnicodeEncoding.Utf8, "application/json");
            return await hc.PostAsync(uri, content).AsTask(ct);
            /*
            var resContentStr = await resMsg.Content.ReadAsStringAsync();
            return JsonValue.Parse(resContentStr).GetObject();*/
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
            catch
            {
                subscrXd = XDocument.Load("Assets/" + subscrSiteXml);
            }

            subscrSiteList.Clear();
            foreach (var s in subscrXd.Descendants("sitename"))
            {
                subscrSiteList.Add(s.Value);
            }

            // Insert MainSite.
            subscrSiteList.Insert(0, (string)localSettings.Values["MainSite"]);

            return 0;
        }

        public int updateSubscrSiteListByMainSite(string newMainSite)
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
            var aqiStr = "AQI：" + sitesStrDict[siteName]["aqi"];
            var pm2_5_Str = "PM 2.5：" + sitesStrDict[siteName]["PM2.5"];
            var siteStr = "觀測站：" + sitesStrDict[siteName]["sitename"];
            var timeStr = sitesStrDict[siteName]["publishtime"].Substring(11, 5);
            // get the XML content of one of the predefined tile templates, so that, you can customize it
            // Large template
            var statusStr = StaticTaqModel.fieldNames["status"] + "：" + sitesStrDict[siteName]["status"];
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
            var textColor = StaticTaqViewModel.getTextColor(aqLevel);

            // Extract time.
            var dateStr = sitesStrDict[siteName]["publishtime"].Substring(5, 5).Replace("-", "/");
            var timeStr = sitesStrDict[siteName]["publishtime"].Substring(11, 5);
            var aqiStr = sitesStrDict[siteName]["aqi"];
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

            await StaticTaqViewModel.saveUi2Png(siteName + "SmallTile.png", smallTile);
            await StaticTaqViewModel.saveUi2Png(siteName + "MedTile.png", medTile);
            await StaticTaqViewModel.saveUi2Png(siteName + "WideTile.png", wideTile);
            await StaticTaqViewModel.saveUi2Png(siteName + "LargeTile.png", largeTile);
            return 0;
        }

        public int sendSubscrSitesNotifications()
        {
            var quietStartTime = (TimeSpan)localSettings.Values["QuietStartTime"];
            var quietEndTime = (TimeSpan)localSettings.Values["QuietEndTime"];

            // Quiet hours checking.
            var zeroTime = new TimeSpan(0, 0, 0);
            var now = DateTime.Now;
            var nowHm = new TimeSpan(now.Hour, now.Minute, 0);
            bool isQuietTime = false;
            // Quiet hours spand between two days.
            if ((quietEndTime - quietStartTime) < zeroTime)
            {
                isQuietTime = quietStartTime <= nowHm || nowHm < quietEndTime;
            }
            else
            {
                isQuietTime = quietStartTime <= nowHm && nowHm < quietEndTime;
            }

            if (isQuietTime)
            {
                return 1;
            }

            sendNotifications(subscrSiteList[0]);

            if ((bool)localSettings.Values["SecondSitesNotify"])
            {
                foreach (var siteName in subscrSiteList.GetRange(1, subscrSiteList.Count - 1))
                {
                    sendNotifications(siteName);
                }
            }
            return 0;
        }

        // Try to send all AQ notifications.
        public void sendNotifications(string siteName)
        {
            var warnStateChangeMode = (bool)localSettings.Values["WarnStateChangeMode"];
            foreach (var aqName in new List<string> { "aqi", "PM2.5" })
            {
                var aqi_Limit = (double)localSettings.Values[aqName + "_Limit"];
                var currAqi = getValidAqVal(sitesStrDict[siteName][aqName]);
                var oldAqi = getValidAqVal(oldSitesStrDict[siteName][aqName]);

                var isAqiOverWarnLevel = (oldAqi != currAqi && aqi_Limit < currAqi);
                // "oldAqi != 0" and "currAqi != 0" conditions filter the cases that numbers comes from NaN. It could filter the case that numbers are real 0, however, we assume that the probability is low.
                var isWarnTransition = (oldAqi <= aqi_Limit && aqi_Limit < currAqi && oldAqi != 0);
                var isSafeTransition = (oldAqi > aqi_Limit && aqi_Limit >= currAqi && currAqi != 0);
                var isWarnStateChanged = isWarnTransition || isSafeTransition;

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
            if(aqValStr == "N/A")
            {
                return -1;
            }
            double.TryParse(aqValStr, out val);
            return val;
        }

        public double getAqVal(string siteName, string aqName)
        {
            if (aqName == "status" || aqName == "ShortStatus")
            {
                aqName = "aqi";
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
            catch
            {
                throw new Exception(resLoader.GetString("positioningNearestSiteFail"));
            }
            return 0;
        }

        public string getLastUpdateTime()
        {
            return lastUpdateTime.ToString("HH:mm:ss tt");
        }

        public async Task<MapLocationFinderResult> findGeoLoc(string addressToGeocode)
        {
            // Geocode the specified address, using the specified reference point
            // as a query hint. Return no more than 3 results.
            var result = await MapLocationFinder.FindLocationsAsync(
                addressToGeocode,
                // Hint point.
                StaticTaqModel.twCenterLoc,
                3);

            return result;
        }
    }
}
