using System;
using TaqShared.Models;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Taq.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AqSiteMap : Page
    {
        public ApplicationDataContainer localSettings;
        public App app;

        public AqSiteMap()
        {
            localSettings =
       ApplicationData.Current.LocalSettings;
            app = App.Current as App;
            this.InitializeComponent();
            initPos();
            //map.Loaded += initPos;
        }

        // center on Taiwan     
        Geopoint twCenter =
            new Geopoint(new BasicGeoposition()
            {
                Latitude = 23.973875,
                Longitude = 120.982024

            });

        GeolocationAccessStatus locAccStat;
        Geolocator geoLoc;
        private async void initPos()
        {
            addMapIcons();

            locAccStat = await Geolocator.RequestAccessAsync();

            switch (locAccStat)
            {
                case GeolocationAccessStatus.Allowed:
                    // Center map on user location.
                    geoLoc = new Geolocator { ReportInterval = 2000 };
                    // Subscribe to the PositionChanged event to get location updates.
                    //geoLoc.PositionChanged += OnPositionChanged;
                    try
                    {
                        var pos = await geoLoc.GetGeopositionAsync();
                        var p = pos.Coordinate.Point;

                        var userMapIcon = new UserMapIcon();
                        map.Children.Add(userMapIcon);
                        MapControl.SetLocation(userMapIcon, p);
                        MapControl.SetNormalizedAnchorPoint(userMapIcon, new Point(0.5, 0.5));
                        await map.TrySetSceneAsync(MapScene.CreateFromLocationAndRadius(p, 1000));
                    }
                    catch (Exception ex)
                    {
                    }
                    break;
                default:
                    // Center map on Taiwan center.
                    await map.TrySetSceneAsync(MapScene.CreateFromLocationAndRadius(twCenter, 110e3));
                    break;
            }
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
            foreach (var s in app.shared.sites)
            {
                addMapIcon(s, i);
                i++;
            }
        }

        private void addMapIcon(Site site, int i)
        {
            var gp = new Geopoint(new BasicGeoposition { Latitude = site.twd97Lat, Longitude = site.twd97Lon });

            var ellipse1 = new CircleText();

            Binding colorBind = new Binding();
            colorBind.Source = app.shared.sites;
            colorBind.Path = new PropertyPath("[" + i + "].CircleColor");
            colorBind.Mode = BindingMode.OneWay;
            ellipse1.circle.SetBinding(Ellipse.FillProperty, colorBind);

            Binding textBind = new Binding();
            textBind.Source = app.shared.sites;
            textBind.Path = new PropertyPath("[" + i + "].CircleText");
            textBind.Mode = BindingMode.OneWay;
            ellipse1.txtBlk.SetBinding(TextBlock.TextProperty, textBind);

            Binding textColorBind = new Binding();
            textColorBind.Source = app.shared.sites;
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
