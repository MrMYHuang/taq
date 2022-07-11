using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.Devices.Geolocation;

namespace Taq.Shared.Models
{
    public static class StaticTaqModel
    {
        // XML AQ names to ordinary AQ names.
        // The order of keys is meaningful!
        // The display order of AQ items in Home.xaml follows this order of keys.
        // Please check function loadSite2Aqvm for the usage of this variable!
        public static Dictionary<string, string> fieldNames = new Dictionary<string, string>
        {
            { "sitename", "觀測站" },
            { "county", "縣市"},

            { "publishtime", "發佈時間"},
            { "pollutant", "污染指標物"},

            { "status", "狀態"},
            { "aqi", "空氣品質指標"},

            { "pm2.5", "即時"},
            { "pm2.5_avg", "平均"},

            { "pm10", "即時"},
            { "pm10_avg", "平均"},

            { "o3", ""},
            { "o3_8hr", "8hr"},

            { "co", ""},
            { "co_8hr", "平均"},

            { "so2", ""},
            { "no2", ""},

            { "nox", ""},
            { "no", ""},

            { "wind_speed", "風速"},
            { "wind_direc", "風向"},
        };

        public static Dictionary<string, string> shortStatusDict = new Dictionary<string, string>
        {
            { "對敏感族群不健康", "敏感"},
            { "對所有族群不健康", "所有"},
            { "非常不健康", "非常"},
            { "設備維護", "維護"},
        };

        public static string getShortStatus(string statusStr)
        {
            var key = StaticTaqModel.shortStatusDict.Keys.Where(k => k == statusStr);
            if (key.Count() != 0)
            {
                return StaticTaqModel.shortStatusDict[key.First()];
            }
            return statusStr;
        }

        // Limit of color of texts on the corresponding AQ background color.
        // <= aqTextColorLimit: black
        // > aqTextColorLimit: white
        public static int aqTextColorLimit = 3;

        // AQ level limits and corresponding colors lists.
        // Notice: a color list has one more element than a limit list!
        public static List<double> defaultLimits = new List<double> { 0 };
        public static List<string> defaultColors = new List<string> { "#a6ce39", "#a6ce39" };

        public static List<string> dirtyColors = new List<string> { "#C0C0C0", "#C0C0C0" };

        public static List<double> aqiLimits = new List<double> { -1, 50, 100, 150, 200, 300, 400, 500 };
        // From https://www3.epa.gov/airnow/aqi_brochure_02_14.pdf
        public static List<string> aqiBgColors = new List<string> { "#C0C0C0", "#a6ce39", "#fff200", "#f7901e", "#ed1d24", "#a2064a", "#891a1c", "#891a1c", "#891a1c" };

        public static List<double> pm2_5Limits = new List<double> {-1, 15.4, 35.4, 54.4, 150.4, 250.4, 350.4, 500.4 };
        public static List<double> pm10Limits = new List<double> {-1, 54, 125, 254, 354, 424, 504, 604 };
        public static List<double> o3Limits = new List<double> {-1, 60, 125, 164, 204, 404, 504, 604 };
        // 201, 202, ...
        public static List<double> o3_8hrLimits = new List<double> {-1, 54, 70, 85, 105, 200, 201, 202 };
        public static List<double> coLimits = new List<double> {-1, 4.4, 9.4, 12.4, 15.4, 30.4, 40.4, 50.4 };
        public static List<double> so2Limits = new List<double> {-1, 35, 75, 185, 304, 604, 804, 1004 };
        public static List<double> no2Limits = new List<double> {-1, 53, 100, 360, 649, 1249, 1649, 2049 };

        public static int getAqLevel(string aqName, double aqVal)
        {
            var aqLevel = aqLimits[aqName].FindIndex(x => aqVal <= x);
            if (aqLevel == -1)
            {
                aqLevel = aqLimits[aqName].Count;
            }
            return aqLevel;
        }

        // Combine color lists into Dictinoary.
        public static Dictionary<string, List<string>> aqColors = new Dictionary<string, List<string>>
        {
            { "publishtime", defaultColors},
            { "sitename", defaultColors },
            { "county", defaultColors},
            { "pollutant", dirtyColors},
            { "aqi", aqiBgColors},
            { "status", aqiBgColors},
            { "ShortStatus", aqiBgColors},
            { "pm2.5", aqiBgColors},
            { "pm2.5_avg", aqiBgColors},
            { "pm10", aqiBgColors},
            { "pm10_avg", aqiBgColors},
            { "o3", aqiBgColors},
            { "o3_8hr", aqiBgColors},
            { "co", aqiBgColors},
            { "co_8hr", aqiBgColors},
            { "so2", aqiBgColors},
            { "no2", aqiBgColors},
            { "nox", dirtyColors},
            { "no", dirtyColors},
            { "wind_speed", defaultColors},
            { "wind_direc", defaultColors},
        };

        // Combine limit lists into Dictinoary.
        public static Dictionary<string, List<double>> aqLimits = new Dictionary<string, List<double>>
        {
            { "publishtime", defaultLimits},
            { "sitename", defaultLimits },
            { "county", defaultLimits},
            { "pollutant", defaultLimits},
            { "aqi", aqiLimits},
            { "status", aqiLimits},
            { "ShortStatus", aqiLimits},
            { "pm2.5", pm2_5Limits},
            { "pm2.5_avg", pm2_5Limits},
            { "pm10", pm10Limits},
            { "pm10_avg", pm10Limits},
            { "o3", o3Limits},
            { "o3_8hr", o3_8hrLimits},
            { "co", coLimits},
            { "co_8hr", coLimits},
            { "so2", so2Limits},
            { "no2", no2Limits},
            { "nox", defaultLimits},
            { "no", defaultLimits},
            { "wind_speed", defaultLimits},
            { "wind_direc", defaultLimits},
        };

        // (near) center on Taiwan     
        public static Geopoint twCenterLoc = new Geopoint(new BasicGeoposition()
        {
            Latitude = 23.6,
            Longitude = 120.982024
        });

        // Squared Euclidean distance.
        public static double posDist(GpsPoint p1, GpsPoint p2)
        {
            return Math.Pow(p1.twd97Lat - p2.twd97Lat, 2) + Math.Pow(p1.twd97Lon - p2.twd97Lon, 2);
        }

        // Return true if the file modified date is older for time renewTime.
        public static async Task<bool> checkFileOutOfDate(string file, TimeSpan renewTime)
        {
            try
            {
                var fsf = await ApplicationData.Current.LocalFolder.GetFileAsync(file);
                var fbp = await fsf.GetBasicPropertiesAsync();
                var fdm = fbp.DateModified;
                var now = DateTimeOffset.Now;
                return ((now - fdm) > renewTime);
            }
            // If file not exist.
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return true;
            }
        }
    }
}
