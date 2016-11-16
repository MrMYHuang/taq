using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using TaqShared;
using TaqShared.Models;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
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
    public sealed partial class Settings : Page, INotifyPropertyChanged
    {
        public App app;
        public Windows.Storage.ApplicationDataContainer localSettings;
        public Site site;
        public Settings()
        {
            localSettings =
       Windows.Storage.ApplicationData.Current.LocalSettings;
            site = new Site { CircleColor = Shared.pm2_5_colors[(int)localSettings.Values["Pm2_5_ConcensIdx"] - 1] };
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

        public int Pm2_5_ConcensIdx
        {
            get
            {
                return (int)localSettings.Values["Pm2_5_ConcensIdx"];
            }

            set
            {
                localSettings.Values["Pm2_5_ConcensIdx"] = value;
                site.CircleColor = Shared.pm2_5_colors[value - 1];
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

        private void comboBox_SelectionChanged(Object sender, SelectionChangedEventArgs e)
        {
            var selSite = (Site)((ComboBox)sender).SelectedItem;
            // sites reloading can trigger this event handler and results in null.
            if (selSite == null)
            {
                return;
            }
            localSettings.Values["subscrSite"] = selSite.siteName;
            app.shared.oldSite = app.shared.currSite;
            app.shared.currSite = app.shared.sites.Where(s => s.siteName == selSite.siteName).First();
            app.shared.updateLiveTile();
#if DEBUG
            app.shared.sendNotify();
#endif
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
