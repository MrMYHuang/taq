﻿using System;
using System.Threading.Tasks;
using Taq.Shared.ViewModels;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Shapes;
using System.Linq;
using Taq.Shared.Models;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Taq.Uwp.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AqSiteMap : Page
    {
        public ApplicationDataContainer localSettings;
        public App app;
        public Frame rootFrame;
        public MainPage mainPage;

        public AqSiteMap()
        {
            localSettings =
       ApplicationData.Current.LocalSettings;
            app = App.Current as App;
            rootFrame = Window.Current.Content as Frame;
            mainPage = rootFrame.Content as MainPage;
            this.InitializeComponent();
            initPos();
            //map.Loaded += initPos;
        }

        private async void initPos()
        {
            addMapIcons();

            if (app.vm.AutoPos && app.vm.MapAutoPos)
            {
                await autoPosUmi();
            }
            else
            {
                mapCenterOnTw();
            }
        }

        private void aqComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            mainPage.aqComboBox_SelectionChanged(sender, e);
        }

        private async void umiButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            await autoPosUmi();
        }

        // Map icon for user location obtained by GPS.
        UserMapIcon userMapIcon = new UserMapIcon();
        private async Task<int> autoPosUmi()
        {
            try
            {
                app.vm.StatusText = app.vm.m.resLoader.GetString("positioning");
                // Subscribe to the PositionChanged event to get location updates.
                //geoLoc.PositionChanged += OnPositionChanged;
                app.vm.m.geoLoc = new Geolocator { ReportInterval = 2000 };
                var pos = await app.vm.m.geoLoc.GetGeopositionAsync();
                var p = pos.Coordinate.Point;
                await posUmi(p);
                app.vm.StatusText = app.vm.m.resLoader.GetString("positioningFinish") + " " + app.vm.m.resLoader.GetString("lastDataUpdateTime") + ":" + app.vm.m.getLastUpdateTime();
            }
            catch
            {
                app.vm.StatusText = app.vm.m.resLoader.GetString("positioningFail") + " " + app.vm.m.resLoader.GetString("lastDataUpdateTime") + ":" + app.vm.m.getLastUpdateTime();
            }
            return 0;
        }

        public async Task<int> posUmi(Geopoint p)
        {
            // userMapIcon has not been added.
            if (map.Children.IndexOf(userMapIcon) == -1)
            {
                map.Children.Add(userMapIcon);
            }
            MapControl.SetLocation(userMapIcon, p);
            MapControl.SetNormalizedAnchorPoint(userMapIcon, new Point(0.5, 0.5));
            await map.TrySetSceneAsync(MapScene.CreateFromLocationAndRadius(p, 1000));
            return 0;
        }

        private void mapCenterOnTw()
        {
            // Center map on Taiwan center.
            map.Scene = MapScene.CreateFromLocation(StaticTaqModel.twCenterLoc);
            map.ZoomLevel = 7.5;
        }

        /*
        private async void OnPositionChanged(Geolocator sender, PositionChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
            });
        }
        */

        private void addMapIcons()
        {
            // Add new PM 2.5 map icons.
            var i = 0;
            foreach (var s in app.vm.sites)
            {
                addMapIcon(s, i);
                i++;
            }
        }

        private void addMapIcon(SiteViewModel site, int i)
        {
            var gp = new Geopoint(new BasicGeoposition { Latitude = site.twd97Lat, Longitude = site.twd97Lon });

            var ellipse1 = new CircleText();

            Binding colorBind = new Binding();
            colorBind.Source = app.vm.sites;
            colorBind.Path = new PropertyPath("[" + i + "].CircleColor");
            colorBind.Mode = BindingMode.OneWay;
            ellipse1.circle.SetBinding(Ellipse.FillProperty, colorBind);

            Binding textBind = new Binding();
            textBind.Source = app.vm.sites;
            textBind.Path = new PropertyPath("[" + i + "].CircleText");
            textBind.Mode = BindingMode.OneWay;
            ellipse1.txtBlk.SetBinding(TextBlock.TextProperty, textBind);

            Binding textColorBind = new Binding();
            textColorBind.Source = app.vm.sites;
            textColorBind.Path = new PropertyPath("[" + i + "].TextColor");
            textColorBind.Mode = BindingMode.OneWay;
            ellipse1.txtBlk.SetBinding(TextBlock.ForegroundProperty, textColorBind);
            //var ellipse1 = new Ellipse();

            // Add the MapIcon to the map.
            //map.MapElements.Add(mapIcon1);
            //map.Center = gp;
            //map.ZoomLevel = 14;
            map.Children.Add(ellipse1);
            MapControl.SetLocation(ellipse1, gp);
            MapControl.SetNormalizedAnchorPoint(ellipse1, new Point(0.5, 0.5));

            // Center the map over the POI.
            //map.Center = gp;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            umi.IsEnabled = app.vm.AutoPos;
        }

        // ASB
        private void asb_Loaded(object sender, RoutedEventArgs e)
        {
            asb.ItemsSource = app.vm.m.sitesStrDict.Keys;
        }

        private async void asb_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var siteNames = app.vm.m.sitesStrDict.Keys.Where(sn => sn.Contains(asb.Text)).ToList();

                if (siteNames.Count() == 0)
                {
                    var findLocs = await app.vm.m.findGeoLoc(asb.Text);
                    foreach(var loc in findLocs.Locations)
                    {
                        siteNames.Add(loc.Description);
                    }
                }

                asb.ItemsSource = siteNames;
            }
        }

        private async void asb_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            await asb_QuerySubmit();
        }

        private async void asb_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            asb.Text = args.SelectedItem.ToString();
            await asb_QuerySubmit();
        }

        private async Task<int> asb_QuerySubmit()
        {
            var siteNames = app.vm.m.sitesStrDict.Keys;
            var findSite = siteNames.Where(sn => sn == asb.Text);
            if (findSite.Count() != 0)
            {
                var findSiteName = findSite.First();
                var p = new Geopoint(new BasicGeoposition
                {
                    Latitude = double.Parse(app.vm.m.sitesStrDict[findSiteName]["TWD97Lat"]),
                    Longitude = double.Parse(app.vm.m.sitesStrDict[findSiteName]["TWD97Lon"])
                });
                await map.TrySetSceneAsync(MapScene.CreateFromLocationAndRadius(p, 1000));
            }
            return 0;
        }

        private void asb_GotFocus(object sender, RoutedEventArgs e)
        {
            asb.IsSuggestionListOpen = true;
        }

        public string MapToken
        {
            get
            {
                return Params.bingMapToken;
            }
        }
    }

    public class BoolToMapColorC : IValueConverter
    {
        public Object Convert(object value, Type targetType, object parameter, string lang)
        {
            return (bool)value ? MapColorScheme.Dark : MapColorScheme.Light;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string lang)
        {
            return (MapColorScheme)value == MapColorScheme.Dark ? true : false;
        }
    }
}
