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
        public static double[] pm2_5_concens = new double[] { 11, 23, 35, 41, 47, 53, 58, 64, 70 };
        public static string[] pm2_5_colors = new string[] { "#9cff9c", "#31ff00", "#31cf00", "#ffff00", "#ffcf00", "#ff9a00", "#ff6464", "#ff0000", "#990000", "#ce30ff" };
        private Windows.Storage.ApplicationDataContainer localSettings;
        public Uri source = new Uri("http://YourTaqServerIp/taq/taq.xml");
        public string dataXmlFile = "taq.xml";
        public string currDataXmlFile = "currData.xml";
        public XDocument xd = new XDocument();
        public XDocument siteGeoXd = new XDocument();
        public ObservableCollection<Site> sites = new ObservableCollection<Site>();
        public Site currSite = new Site { siteName = "N/A", Pm2_5 = "0" };
        public ObservableCollection<string> currSiteStr = new ObservableCollection<string>();
        public IEnumerable<XElement> currSiteX;

        public Site oldSite = new Site { siteName = "N/A", Pm2_5 = "0" };


        public Dictionary<string, string> fieldNames = new Dictionary<string, string>
        {
            {"SiteName", "觀測站" },
            { "County", "縣市"},
            { "PSI", "PSI"},
            { "MajorPollutant", "主要汙染"},
            { "Status", "狀態"},
            { "SO2", "SO2"},
            { "CO", "CO"},
            { "O3", "O3"},
            { "PM10", "PM 10"},
            { "PM2.5", "PM 2.5"},
            { "NO2", "NO2"},
            { "WindSpeed", "風速"},
            { "WindDirec", "風向"},
            { "FPMI", "FPMI"},
            { "NOx", "NOx"},
            { "NO", "NO"},
            { "PublishTime", "發佈時間"}
        };

        public Shared()
        {
            localSettings =
       Windows.Storage.ApplicationData.Current.LocalSettings;
        }

        public async Task<int> downloadDataXml()
        {
            // Download may fail, so we create a temp StorageFile.
            var dlFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("Temp"+ dataXmlFile, CreationCollisionOption.ReplaceExisting);

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
                    xd = XDocument.Load("Assets/taq.xml");
                    throw new OldXmlException();
                }
            }
            else
            {
                xd = XDocument.Load("Assets/taq.xml");
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

            if (sites.Count != 0)
            {
                removeAllSites();
            }

            foreach (var d in dataX.OrderBy(x => x.Element("County").Value))
            {
                var siteName = d.Descendants("SiteName").First().Value;
                var geoD = from gd in geoDataX
                           where gd.Descendants("SiteName").First().Value == siteName
                           select gd;
                sites.Add(new Site
                {
                    siteName = siteName,
                    County = d.Descendants("County").First().Value,
                    Pm2_5 = d.Descendants("PM2.5").First().Value,
                    twd97Lat = double.Parse(geoD.Descendants("TWD97Lat").First().Value),
                    twd97Lon = double.Parse(geoD.Descendants("TWD97Lon").First().Value),
                });
            }

            reloadSubscrSiteId();
            return 0;
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
                oldSite = new Site { siteName = oldSiteX.Descendants("SiteName").First().Value, Pm2_5 = oldSiteX.Descendants("PM2.5").First().Value };
            }
            catch (Exception ex)
            {
                // Ignore.
            }

            try
            {
                // Get new site from the setting.
                var newSiteName = (string)localSettings.Values["subscrSite"];
                currSiteX = from d in xd.Descendants("Data")
                              where d.Descendants("SiteName").First().Value == newSiteName
                              select d;
                currSite = new Site { siteName = currSiteX.Descendants("SiteName").First().Value, Pm2_5 = currSiteX.Descendants("PM2.5").First().Value };

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
            if (currSiteStr.Count() != 0)
            {
                // Don't remove all elements by new.
                // Otherwise, data bindings would be problematic.
                for (var i = currSiteStr.Count() - 1; i >= 0; i--)
                {
                    currSiteStr.RemoveAt(i);
                }
            }
            /*
            var currSiteName = currSite.siteName;
            var currEmuX = from x in xd.Descendants("Data")
                           where x.Descendants("SiteName").First().Value == currSiteName
                           select x;
            */
            var currEle = currSiteX.First().Elements();

            // Reorder important items to the top.
            currEle = currEle.OrderByDescending(x => x.Name == "PSI"); // Less important
            currEle = currEle.OrderByDescending(x => x.Name == "PM10");
            currEle = currEle.OrderByDescending(x => x.Name == "PM2.5");
            currEle = currEle.OrderByDescending(x => x.Name == "SiteName");
            currEle = currEle.OrderByDescending(x => x.Name == "PublishTime"); // More important

            foreach (var item in currEle)
            {
                currSiteStr.Add(fieldNames[item.Name.ToString()] + "\n" + item.Value);
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

            // get the XML content of one of the predefined tile templates, so that, you can customize it
            // create the wide template
            var timeStr = currSiteX.Descendants("PublishTime").First().Value.Substring(11, 5);
            var wideContent = TileContentFactory.CreateTileWide310x150BlockAndText01();
            wideContent.TextBlock.Text = currSite.Pm2_5;
            wideContent.TextSubBlock.Text = "PM 2.5";
            wideContent.TextBody1.Text = "觀測站：" + currSite.siteName;
            wideContent.TextBody2.Text = "發佈時間：" + timeStr;
            //wideContent.Image.Src = "ms-appx:///Assets/Wide310x150Logo.scale-200.png";

            // create the square template and attach it to the wide template 
            ITileSquare150x150Block squareContent = TileContentFactory.CreateTileSquare150x150Block();
            squareContent.TextBlock.Text = currSite.Pm2_5.ToString();
            squareContent.TextSubBlock.Text = "PM 2.5\n" + currSite.siteName + "/" + timeStr;
            wideContent.Square150x150Content = squareContent;

            // Create a new tile notification.
            updater.Update(new TileNotification(wideContent.GetXml()));
        }

        public void sendNotify()
        {
            int pm2_5_WarnIdx = (int)localSettings.Values["Pm2_5_ConcensIdx"];
            //var currMin = DateTime.Now.Minute;
            if (!(oldSite.Pm2_5 != currSite.Pm2_5 && pm2_5ConcensToIdx(currSite.pm2_5_int) > pm2_5_WarnIdx))
            {
                return;
            }
            var title = "PM 2.5濃度: " + currSite.Pm2_5;
            var content = "觀測站: " + currSite.siteName;
            // Now we can construct the final toast content
            ToastContent toastContent = new ToastContent()
            {
                Visual = getNotifyVisual(title, content)
            };

            // And create the toast notification
            var toast = new ToastNotification(toastContent.GetXml());
            toast.ExpirationTime = DateTime.Now.AddHours(1);
            toast.Tag = "1";
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

        public int pm2_5ConcensToIdx(int concens)
        {
            var i = 0;
            for (; i < pm2_5_concens.Length; i++)
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
