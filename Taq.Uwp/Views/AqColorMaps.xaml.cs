using Syncfusion.UI.Xaml.Charts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Taq.Shared.Models;
using Taq.Shared.ModelViews;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Taq.Uwp.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AqColorMaps : Page
    {
        public App app;

        public static List<string> apNames = new List<string> { "AQI", "PM2.5", "PM10", "O3", "CO", "SO2", "NO2" };

        public ObservableCollection<AqLimits> aqLimitsColl { get; set; }
        public AqColorMaps()
        {
            app = App.Current as App;
            this.InitializeComponent();
            aqLimitsColl = new ObservableCollection<AqLimits>();
            var aqLevelCnt = StaticTaqModel.aqiLimits.Count;
            foreach (var aqName in apNames)
            {
                var aqLimit = new AqLimits
                {
                    Name = aqName,
                    Limits = new ObservableCollection<double>(),
                    Diffs = new ObservableCollection<double>()
                };

                // Skip aqLimits[aqName][0], because it is unused -1.
                aqLimit.Limits.Add(StaticTaqModel.aqLimits[aqName][1]);
                aqLimit.Diffs.Add(StaticTaqModel.aqLimits[aqName][1]);
                for (var i = 2; i < aqLevelCnt; i++)
                {
                    aqLimit.Limits.Add(StaticTaqModel.aqLimits[aqName][i]);
                    aqLimit.Diffs.Add(StaticTaqModel.aqLimits[aqName][i] - StaticTaqModel.aqLimits[aqName][i - 1]);
                }
                aqLimitsColl.Add(aqLimit);

            }
            for (var i = 1; i < aqLevelCnt; i++)
            {
                var sbs = new StackingColumn100Series();
                sbs.Interior = new SolidColorBrush(StaticTaqModelView.html2RgbColor(StaticTaqModel.aqiBgColors[i]));

                sbs.XBindingPath = "Name";
                sbs.YBindingPath = "Diffs[" + i + "]";

                var b = new Binding();
                b.Source = this;
                b.Path = new PropertyPath("aqLimitsColl");
                b.Mode = BindingMode.OneWay;
                sbs.SetBinding(StackingBarSeries.ItemsSourceProperty, b);

                var cai = new ChartAdornmentInfo();
                cai.SegmentLabelContent = LabelContent.LabelContentPath;
                // DataContext =\"aqLimitsColl[" + i + "].\"
                var textColor = "Black";
                if(i > StaticTaqModel.aqTextColorLimit)
                {
                    textColor = "White";
                }
                var dt = XamlReader.Load($"<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">" +
                    "<TextBlock Foreground=\"" + textColor + "\" Text=\"{Binding Converter={StaticResource con}}\"></TextBlock></DataTemplate>") as DataTemplate;
                cai.LabelTemplate = dt;
                cai.LabelPosition = AdornmentsLabelPosition.Inner;
                cai.ShowLabel = true;

                sbs.AdornmentsInfo = cai;
                sbs.SegmentSpacing = 0;

                sfChart.Series.Add(sbs);
            }

            this.DataContext = this;
        }
    }

    public class AqLimits
    {
        public string Name { get; set; }
        public ObservableCollection<double> Limits { get; set; }
        public ObservableCollection<double> Diffs { get; set; }
    }

    public class convert : IValueConverter
    {
        static int y = 0;
        static int x = 0;
        public object Convert(object value, Type targetType, object parameter, string culture)
        {

            var adornment = value as ChartAdornment;
            var aqLimit = adornment.Item as AqLimits;
            var limit = aqLimit.Limits[y];
            x++;

            if(x == AqColorMaps.apNames.Count)
            {
                y++;
                x = 0;
            }

            return limit;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string culture)
        {
            return value;
        }

    }
}
