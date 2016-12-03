using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TaqShared;
using TaqShared.Models;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Taq.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Settings : Page, INotifyPropertyChanged
    {
        public App app;
        public ApplicationDataContainer localSettings;
        public Site site;
        public Settings()
        {
            localSettings =
       ApplicationData.Current.LocalSettings;
            site = new Site { CircleColor = Shared.aqiBgColors[(int)localSettings.Values["Pm2_5_LimitId"] - 1] };
            this.InitializeComponent();
            app = App.Current as App;
        }

        public bool AppTheme
        {
            get
            {
                return (bool)localSettings.Values["AppTheme"];
            }

            set
            {
                localSettings.Values["AppTheme"] = value;
            }
        }

        public int Aqi_LimitId
        {
            get
            {
                return (int)localSettings.Values["Aqi_LimitId"];
            }

            set
            {
                localSettings.Values["Aqi_LimitId"] = value;
                NotifyPropertyChanged();
            }
        }

        public int Pm2_5_LimitId
        {
            get
            {
                return (int)localSettings.Values["Pm2_5_LimitId"];
            }

            set
            {
                localSettings.Values["Pm2_5_LimitId"] = value;
                site.CircleColor = Shared.aqiBgColors[value - 1];
                NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void bgUpdateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selId = ((ComboBox)sender).SelectedIndex;
            app.BgUpdatePeriodId = selId;
        }
    }

    public class Double2Int : IValueConverter
    {
        public Object Convert(object value, Type targetType, object parameter, string lang)
        {
            return System.Convert.ToDouble(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string lang)
        {
            return System.Convert.ToInt32(value);
        }
    }

}
