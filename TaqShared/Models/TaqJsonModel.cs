using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Taq;
using Windows.Storage;
using System.Xml.Linq;
using Windows.ApplicationModel;

namespace TaqShared.Models
{
    public class TaqJsonModel : TaqModel
    {
        public async Task<JObject> readJObject(StorageFile file)
        {
            using (var s = await file.OpenStreamForReadAsync())
            {
                using (var reader = new StreamReader(s))
                {
                    var jo =  JObject.Parse(reader.ReadToEnd());
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
            catch (Exception ex)
            {
                var dataJson = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/" + Params.aqDbFile));
                jTaqDb = await readJObject(dataJson);
            }

            var data = jTaqDb["result"]["records"];
            var geoDataX = from gdata in siteGeoXd.Descendants("Data")
                           select gdata;

            sitesStrDict.Clear();
            sitesGeoDict.Clear();
            foreach (var d in data)
            {
                var siteDict = d.ToObject<Dictionary<string, string>>();
                var siteName = siteDict["SiteName"];
                var geoD = from gd in geoDataX
                           where gd.Descendants("SiteName").First().Value == siteName
                           select gd;
                
                var geoDict = geoD.Elements().ToDictionary(x => x.Name.LocalName, x => x.Value);
                siteDict.Add("TWD97Lat", geoDict["TWD97Lat"]);
                siteDict.Add("TWD97Lon", geoDict["TWD97Lon"]);
                sitesGeoDict.Add(siteName, new GpsPoint
                {
                    twd97Lat = double.Parse(siteDict["TWD97Lat"]),
                    twd97Lon = double.Parse(siteDict["TWD97Lon"]),
                });
                // Shorten long status strings for map icons.
                siteDict.Add("ShortStatus", StaticTaqModel.getShortStatus(siteDict["Status"]));
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
            catch (Exception ex)
            {
                var dataJson = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Old" + Params.aqDbFile));
                jTaqDb = await readJObject(dataJson);
            }

            var oldData = jTaqDb["result"]["records"];
            oldSitesStrDict.Clear();
            foreach (var d in oldData)
            {
                var siteDict = d.ToObject<Dictionary<string, string>>();
                var siteName = siteDict["SiteName"];
                oldSitesStrDict.Add(siteName, siteDict);
            }

            // Save new site to the setting.
            localSettings.Values["MainSite"] = newMainSite;
            mainSiteStrDict = sitesStrDict[newMainSite];

            return 0;
        }
    }
}
