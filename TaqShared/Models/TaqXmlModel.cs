using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Taq;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;

namespace TaqShared.Models
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
            catch (Exception ex)
            {
                xd = XDocument.Load("Assets/" + Params.aqDbFile);
            }

            var dataX = from data in xd.Descendants("Data")
                        select data;
            var geoDataX = from data in siteGeoXd.Descendants("Data")
                           select data;

            sitesStrDict.Clear();
            sitesGeoDict.Clear();
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
    }
}
