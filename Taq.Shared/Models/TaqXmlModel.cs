﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Taq;
using Windows.Storage;

namespace Taq.Shared.Models
{
    public class TaqXmlModel : TaqModel
    {
        // Reload air quality XML files.
        override public async Task<int> loadAq2Dict()
        {
            XDocument xd = new XDocument();
            try
            {
                var dataXml = await ApplicationData.Current.LocalFolder.GetFileAsync(Params.aqDbFile);
                using (var s = await dataXml.OpenStreamForReadAsync())
                {
                    // Reload to xd.
                    xd = XDocument.Load(s);
                }
            }
            catch
            {
                xd = XDocument.Load("Assets/" + Params.aqDbFile);
            }

            var dataX = from data in xd.Descendants("Data")
                        select data;
            var geoDataX = from data in siteGeoXd.Descendants("Data")
                           select data;

            sitesStrDict.Clear();
            sitesGeoDict.Clear();
            foreach (var d in dataX.OrderBy(x => x.Element("county").Value))
            {
                var siteName = d.Descendants("sitename").First().Value;
                var geoD = from gd in geoDataX
                           where gd.Descendants("sitename").First().Value == siteName
                           select gd;

                var siteDict = d.Elements().ToDictionary(x => x.Name.LocalName, x => x.Value);
                var geoDict = geoD.Elements().ToDictionary(x => x.Name.LocalName, x => x.Value);
                siteDict.Add("TWD97Lat", geoDict["TWD97Lat"]);
                siteDict.Add("TWD97Lon", geoDict["TWD97Lon"]);
                sitesGeoDict.Add(siteName, new GpsPoint
                {
                    twd97Lat = double.Parse(siteDict["TWD97Lat"]),
                    twd97Lon = double.Parse(siteDict["TWD97Lon"]),
                });
                // Shorten long status strings for map icons.
                siteDict.Add("ShortStatus", StaticTaqModel.getShortStatus(siteDict["status"]));
                sitesStrDict.Add(siteName, siteDict);
            }
            return 0;
        }

        override public async Task<int> loadMainSite(string newMainSite)
        {
            // Load the old sites.
            XDocument loadOldXd = new XDocument();
            try
            {
                var loadOldXml = await ApplicationData.Current.LocalFolder.GetFileAsync("Old" + Params.aqDbFile);

                using (var s = await loadOldXml.OpenStreamForReadAsync())
                {
                    loadOldXd = XDocument.Load(s);
                }
            }
            catch
            {
                loadOldXd = XDocument.Load("Assets/Old" + Params.aqDbFile);
            }

            var oldDataX = from data in loadOldXd.Descendants("Data")
                           select data;
            oldSitesStrDict.Clear();
            foreach (var d in oldDataX.OrderBy(x => x.Element("county").Value))
            {
                var siteName = d.Descendants("sitename").First().Value;
                oldSitesStrDict.Add(siteName, d.Elements().ToDictionary(x => x.Name.LocalName, x => x.Value));
            }

            // Save new site to the setting.
            localSettings.Values["MainSite"] = newMainSite;
            updateSubscrSiteListByMainSite(newMainSite);

            return 0;
        }
    }
}
