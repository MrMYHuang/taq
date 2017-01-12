using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TaqBackTask;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Taq.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Settings : Page, INotifyPropertyChanged
    {
        public App app;
        public Frame rootFrame;
        public MainPage mainPage;
        public ApplicationDataContainer localSettings;
        public Settings()
        {
            app = App.Current as App;
            rootFrame = Window.Current.Content as Frame;
            mainPage = rootFrame.Content as MainPage;
            localSettings = ApplicationData.Current.LocalSettings;
            this.InitializeComponent();
        }

        public bool AppTheme
        {
            get
            {
                return (bool)localSettings.Values["AppTheme"];
            }

            set
            {
                if (value != (bool)localSettings.Values["AppTheme"])
                {
                    localSettings.Values["AppTheme"] = value;
                }
            }
        }

        public double Aqi_Limit
        {
            get
            {
                return (double)localSettings.Values["AQI_Limit"];
            }

            set
            {
                if (value != (double)localSettings.Values["AQI_Limit"])
                {
                    localSettings.Values["AQI_Limit"] = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public double Pm2_5_Limit
        {
            get
            {
                return (double)localSettings.Values["PM2.5_Limit"];
            }

            set
            {
                if (value != (double)localSettings.Values["PM2.5_Limit"])
                {
                    localSettings.Values["PM2.5_Limit"] = value;
                    NotifyPropertyChanged();
                }
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

        private async void bgUpdateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selId = ((ComboBox)sender).SelectedIndex;
            app.vm.BgUpdatePeriodId = selId;
            await mainPage.UserPresentTaskReg(Convert.ToUInt32(localSettings.Values["BgUpdatePeriod"]));
        }
    }
}
