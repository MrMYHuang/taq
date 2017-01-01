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
using TaqShared.ModelViews;
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
            sa.Header = aqName;
        }

        public async Task<int> reqAqHistories()
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(Params.uriHost + Params.aqHistTabName + $"?siteName={siteName}");
            var res = await req.GetResponseAsync();

            using (var s = res.GetResponseStream())
            {
                using (var reader = new StreamReader(s))
                {
                    var jTaqs = JObject.Parse(reader.ReadToEnd());
                    var aqVals = ((JArray)jTaqs[aqName.Replace(".", "_")]).Select(v => (double)v).ToList();
                    var updateHour = jTaqs["updateHour"].ToObject<int>();
                    var updateDate = jTaqs["updateDate"].ToObject<string>();
                    await aqVals2Coll(aqVals, updateHour, updateDate);
                }
            }
            return 0;
        }

        public ObservableCollection<Aq24HrVal> aq24HrValColl { get; set; }
        public List<Brush> aqColors { get; set; }
        public async Task<int> aqVals2Coll(List<double> aqVals, int updateHour, string updateDate)
        {
            for (var h = 0; h < 24; h++)
            {
                var rotHour = (24 + updateHour - h) % 24;
                var aqVal = aqVals[rotHour];
                // This ugly coding style comes from a problem that the chart doesn't update its Hour axis anymore after the first assignment to Hour.
                if(rotHour == 0)
                {
                    aq24HrValColl.Add(new Aq24HrVal
                    {
                        // Replace Hour 0 with date.
                        Hour = updateDate.Replace("-", "/"),
                        Val = aqVal
                    });
                }
                else
                {
                    aq24HrValColl.Add(new Aq24HrVal
                    {
                        Hour = rotHour + "",
                        Val = aqVal
                    });
                }
                var aqLevel = app.vm.m.getAqLevel(aqName, aqVal);
                //aq24HrValColl.Where(hv => hv.Hour == "0").First().Hour = updateDate.Replace("-", "/");
                aqColors.Add(new SolidColorBrush(StaticTaqModelView.html2RgbColor(StaticTaqModel.aqColors[aqName][aqLevel])));
            }
            ccm.CustomBrushes = aqColors;

            return 0;
        }
    }

    public class Aq24HrVal
    {
        public string Hour { get; set; }
        public double Val { get; set; }
    }
}
