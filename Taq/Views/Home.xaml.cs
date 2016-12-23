﻿using System;
using System.Linq;
using System.Threading.Tasks;
using TaqShared.ModelViews;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Navigation;

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

        private async void mainSiteComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            // Wait MainPage's initPos for AutoPos being set.
            while (mainSiteComboBox.Items.Count == 0 || mainSiteComboBox.Items.Count <= app.vm.MainSiteId)
            {
                await Task.Delay(100);
            }
            // Do binding SelectedIndex to MainSiteId after ubscrComboBox.Items ready (mainSiteComboBox_Loaded).
            // Don't directly bind SelectedIndex to SubscrSiteId in XAML!
            var b = new Binding { Source = app.vm, Path = new PropertyPath("MainSiteId"), Mode = BindingMode.TwoWay };
            mainSiteComboBox.SetBinding(ComboBox.SelectedIndexProperty, b);
        }

        private async void mainSiteComboBox_SelectionChanged(Object sender, SelectionChangedEventArgs e)
        {
            var selSite = (SiteViewModel)((ComboBox)sender).SelectedItem;
            // SelectionChanged is triggered by changing selected item by, e.g., tapping.
            if (app.vm.m.localSettings.Values["TaqBackTaskUpdated"] == null || (bool)app.vm.m.localSettings.Values["TaqBackTaskUpdated"] == false)
            {
                app.vm.m.localSettings.Values["MainSite"] = selSite.siteName;
                app.vm.aqgvList[0] = new AqGridView(app.vm.loadMainSite2dAqView(selSite.siteName));
                await app.vm.backTaskUpdateTiles();
            }
            // SelectionChanged is triggered by TaqBackTask updating.
            // Do nothing but set this:
            else
            {
                app.vm.m.localSettings.Values["TaqBackTaskUpdated"] = false;
            }
        }
        private async void umiButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await app.vm.m.findNearestSite();
            app.vm.m.localSettings.Values["MainSite"] = app.vm.m.nearestSite;
            app.vm.loadMainSiteId();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void subscrButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var mainPage = rootFrame.Content as MainPage;
            mainPage.frame.Navigate(typeof(Subscr));
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            //var md = new Windows.UI.Popups.MessageDialog(app.tappedId);
            //await md.ShowAsync();
            if (app.tappedId == "App" || app.vm.m.subscrSiteList.Count() == 0)
            {
                return;
            }
            var tappedSsId = app.vm.m.subscrSiteList.IndexOf(app.tappedId);
            while (fv.Items.Count() != (app.vm.m.subscrSiteList.Count() + 1))
            {
                await Task.Delay(100);
            }
            // Pay attention! "0" is the main site.
            fv.SelectedIndex = (tappedSsId + 1);
            return;
        }
    }
}
