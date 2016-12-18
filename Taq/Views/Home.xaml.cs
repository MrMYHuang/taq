using System;
using System.Linq;
using System.Threading.Tasks;
using TaqShared.ModelViews;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Data;

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
            // Wait MainPage's initPos for AutoPos being set.
            while (app.vm.m.localSettings.Values["AutoPos"] == null)
            {
                // Force Umi_Loaded to an async function by await this.
                await Task.Delay(100);
            }
            umi.IsEnabled = app.vm.AutoPos;
        }

        private void subscrComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            // Do binding SelectedIndex to SubscrSiteId after ubscrComboBox.Items ready (subscrComboBox_Loaded).
            // Don't directly bind SelectedIndex to SubscrSiteId in XAML!
            var b = new Binding { Source = app.vm, Path = new PropertyPath("SubscrSiteId"), Mode = BindingMode.TwoWay };
            subscrComboBox.SetBinding(ComboBox.SelectedIndexProperty, b);
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
            if(app.vm.m.localSettings.Values["TaqBackTaskUpdated"] == null || (bool)app.vm.m.localSettings.Values["TaqBackTaskUpdated"] == false)
            {
                // SelectionChanged is triggered by changing selected item by, e.g., tapping.
                app.vm.m.localSettings.Values["subscrSite"] = selSite.siteName;
                //app.vm.loadSubscrSiteId();
                await app.vm.m.loadCurrSite(true);
                app.vm.currSite2AqView();
                await app.vm.backTaskUpdateTiles();
            }
            else
            {
                // SelectionChanged is triggered by TaqBackTask updating.
                // Do nothing but set this:
                app.vm.m.localSettings.Values["TaqBackTaskUpdated"] = false;
            }
        }
        private async void umiButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await app.vm.m.findNearestSite();
            app.vm.m.localSettings.Values["subscrSite"] = app.vm.m.nearestSite;
            app.vm.loadSubscrSiteId();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // ComboBox ItemSource is ready after page loading.
            //subscrComboBox.SelectedIndex = app.vm.SubscrSiteId;
        }
    }
}
