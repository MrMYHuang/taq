using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TaqShared;
using TaqShared.Models;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
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
            this.InitializeComponent();
            app = App.Current as App;
            map.Loaded += initPos;
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
        private async void initPos(object sender, RoutedEventArgs e)
        {
            if (map.Children.Count != 0)
            {
                for (var i = map.Children.Count - 1; i >= 0; i--)
                {
                    map.Children.RemoveAt(i);
                }
            }
            // Add PM 2.5 map icons.
            foreach (var s in app.sites)
            {
                addMapIcon(s);
            }

            locAccStat = await Geolocator.RequestAccessAsync();
            switch (locAccStat)
            {
                case GeolocationAccessStatus.Allowed:
                    // Center map on user location.
                    geoLoc = new Geolocator { ReportInterval = 2000 };
                    // Subscribe to the PositionChanged event to get location updates.
                    //geoLoc.PositionChanged += OnPositionChanged;
                    var pos = await geoLoc.GetGeopositionAsync();
                    var p = pos.Coordinate.Point;

                    var userMapIcon = new UserMapIcon();
                    map.Children.Add(userMapIcon);
                    MapControl.SetLocation(userMapIcon, p);
                    MapControl.SetNormalizedAnchorPoint(userMapIcon, new Point(0.5, 0.5));
                    map.TrySetSceneAsync(MapScene.CreateFromLocationAndRadius(p, 1000));
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

        private void addMapIcon(Site site)
        {
            var gp = new Geopoint(new BasicGeoposition { Latitude = site.twd97Lat, Longitude = site.twd97Lon });

            var ellipse1 = new CircleText();
            Binding colorBind = new Binding();
            colorBind.Source = site.CircleColor;
            ellipse1.circle.SetBinding(Ellipse.FillProperty, colorBind);
            ellipse1.txtBlk.Text = site.siteName + "\n" + site.Pm2_5;
            ellipse1.txtBlk.Foreground = site.TextColor;
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
            return (MapColorScheme) value == MapColorScheme.Dark ? true : false;
        }
    }
}
