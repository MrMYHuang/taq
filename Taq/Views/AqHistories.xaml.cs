using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TaqShared.Models;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Taq.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AqHistories : Page
    {
        public App app;
        public AqHistories()
        {
            app = App.Current as App;
            aq24HrValColl = new ObservableCollection<Aq24HrVal>();
            aqColors = new List<Brush>();
            this.InitializeComponent();
        }

        string siteName;
        string aqName;
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            var p = e.Parameter as object[];
            siteName = p[0].ToString();
            aqName = p[1].ToString();
            await reqAqHistories();
        }

        public async Task<int> reqAqHistories()
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(Params.uriHost + $"epatw?siteName={siteName}");
            var res = await req.GetResponseAsync();

            using (var s = res.GetResponseStream())
            {
                using (var reader = new StreamReader(s))
                {
                    var jTaqs = JObject.Parse(reader.ReadToEnd());
                    var aqVals = ((JArray)jTaqs[aqName.Replace(".", "_")]).Select(v => (double)v).ToArray();
                    var updateHour = jTaqs["updateHour"].ToObject<int>();
                    await aqVals2Coll(aqVals, updateHour);
                }
            }
            return 0;
        }

        public ObservableCollection<Aq24HrVal> aq24HrValColl { get; set; }
        public List<Brush> aqColors { get; set; }
        public async Task<int> aqVals2Coll(double[] aqVals, int updateHour)
        {
            for (var i = 0; i < 24; i++)
            {
                var rotHour = (24 + updateHour - i) % 24;
                var aqVal = aqVals[rotHour];
                aq24HrValColl.Add(new Aq24HrVal
                {
                    Hour = rotHour + "",
                    Val = aqVal
                });
                var aqLevel = app.vm.m.getAqLevel(aqName, aqVal);
                aqColors.Add(new SolidColorBrush(html2RgbColor(StaticTaqModel.aqColors[aqName][aqLevel])));
            }
            ccm.CustomBrushes = aqColors;

            return 0;
        }

        public Color html2RgbColor(string colorStr)
        {
            var colorStr2 = colorStr.Substring(1);
            var r = (byte)Convert.ToUInt32(colorStr2.Substring(0, 2), 16);
            var g = (byte)Convert.ToUInt32(colorStr2.Substring(2, 2), 16);
            var b = (byte)Convert.ToUInt32(colorStr2.Substring(4, 2), 16);
            return Color.FromArgb(0xff, r, g, b);
        }
    }

    public class Aq24HrVal
    {
        public string Hour { get; set; }
        public double Val { get; set; }
    }
}
