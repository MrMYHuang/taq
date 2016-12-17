using System;
using System.Linq;
using System.Threading.Tasks;
using TaqShared.ModelViews;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Taq.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    // For updating UI after TaqBackTask downloads a new XML.

    public sealed partial class Home : Page
    {
        public App app;
        public Frame rootFrame;
        public MainPage mainPage;

        public Home()
        {
            app = App.Current as App;
            rootFrame = Window.Current.Content as Frame;
            mainPage = rootFrame.Content as MainPage;
            this.InitializeComponent();
            umi.Loaded += Umi_Loaded;
        }

        private async void Umi_Loaded(object sender, RoutedEventArgs e)
        {
            // Wait MainPage's initPos for MapAutoPos being set.
            while (app.vm.m.localSettings.Values["MapAutoPos"] == null)
            {
                // Force Umi_Loaded to an async function by await this.
                await Task.Delay(100);
            }
            umi.IsEnabled = app.vm.MapAutoPos;
        }

        private async void subscrComboBox_SelectionChanged(Object sender, SelectionChangedEventArgs e)
        {
            var selSite = (SiteViewModel)((ComboBox)sender).SelectedItem;
            // sites reloading can trigger this event handler and results in null.
            // Unused???
            if (selSite == null)
            {
                return;
            }
            app.vm.m.localSettings.Values["subscrSite"] = selSite.siteName;
            app.vm.loadSubscrSiteId();
            await app.vm.m.loadCurrSite(true);
            await app.vm.backTaskUpdateTiles();
            app.vm.currSite2AqView();
        }
        private async void umiButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await app.vm.findNearestSite();
            var nearestSite = app.vm.sites.Where(s => s.siteName == app.vm.nearestSite.siteName).First();
            app.vm.m.localSettings.Values["subscrSite"] = nearestSite.siteName;
            app.vm.loadSubscrSiteId();
            subscrComboBox.SelectedIndex = app.vm.SubscrSiteId;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // ComboBox ItemSource is ready after page loading.
            subscrComboBox.SelectedIndex = app.vm.SubscrSiteId;
        }
    }
}
