using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Taq;
using Windows.Storage;
using System.Xml.Linq;

namespace Taq.Shared.Models
{
    public class TaqJsonModel : TaqModel
    {
        public async Task<JObject> readJObject(StorageFile file)
        {
            using (var s = await file.OpenStreamForReadAsync())
            {
                using (var reader = new StreamReader(s))
                {
                    var jo = JObject.Parse(reader.ReadToEnd());
                    return jo;
                }
            }
        }

        // Reload air quality JSON files.
        override public async Task<int> loadAq2Dict()
        {
            JObject jTaqDb;
            try
            {
                var dataJson = await ApplicationData.Current.LocalFolder.GetFileAsync(Params.aqDbFile);
                jTaqDb = await readJObject(dataJson);
            }
            catch
            {
                var dataJson = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/" + Params.aqDbFile));
                jTaqDb = await readJObject(dataJson);
            }

            var data = jTaqDb["records"];

            sitesStrDict.Clear();
            sitesGeoDict.Clear();
            foreach (var d in data)
            {
                var siteDict = d.ToObject<Dictionary<string, string>>();
                var siteName = siteDict["sitename"];
                // Skip duplicate keys as possible.
                if (sitesStrDict.ContainsKey(siteName))
                    continue;

                sitesGeoDict.Add(siteName, new GpsPoint
                {
                    twd97Lat = double.Parse(siteDict["latitude"]),
                    twd97Lon = double.Parse(siteDict["longitude"]),
                });
                // Shorten long status strings for map icons.
                siteDict.Add("ShortStatus", StaticTaqModel.getShortStatus(siteDict["status"]));
                if (siteDict["ShortStatus"] == "維護")
                {
                    foreach (var f in aqHistNames)
                    {
                        siteDict[f] = "N/A";
                    }
                }
                sitesStrDict.Add(siteName, siteDict);
            }
            return 0;
        }

        override public async Task<int> loadMainSite(string newMainSite)
        {
            // Load the old sites.
            JObject jTaqDb;
            try
            {
                var dataJson = await ApplicationData.Current.LocalFolder.GetFileAsync("Old" + Params.aqDbFile);
                jTaqDb = await readJObject(dataJson);
            }
            catch
            {
                var dataJson = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Old" + Params.aqDbFile));
                jTaqDb = await readJObject(dataJson);
            }

            var oldData = jTaqDb["result"]["records"];
            oldSitesStrDict.Clear();
            foreach (var d in oldData)
            {
                var siteDict = d.ToObject<Dictionary<string, string>>();
                var siteName = siteDict["sitename"];
                // Skip duplicate keys as possible.
                if (oldSitesStrDict.ContainsKey(siteName))
                    continue;

                oldSitesStrDict.Add(siteName, siteDict);
            }

            // Save new site to the setting.
            localSettings.Values["MainSite"] = newMainSite;
            updateSubscrSiteListByMainSite(newMainSite);

            return 0;
        }
    }
}
