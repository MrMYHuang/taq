using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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
        public Settings()
        {
            localSettings =
       ApplicationData.Current.LocalSettings;
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

        public double Aqi_Limit
        {
            get
            {
                return (double)localSettings.Values["Aqi_Limit"];
            }

            set
            {
                localSettings.Values["Aqi_Limit"] = value;
                NotifyPropertyChanged();
            }
        }

        public double Pm2_5_Limit
        {
            get
            {
                return (double)localSettings.Values["Pm2_5_Limit"];
            }

            set
            {
                localSettings.Values["Pm2_5_Limit"] = value;
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
            app.vm.BgUpdatePeriodId = selId;
        }
    }
}
