using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TaqShared;
using TaqShared.Models;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class AqSiteMap : Page
    {
        public App app;
        public AqSiteMap()
        {
            this.InitializeComponent();
            app = App.Current as App;
            map.Loaded += initPos;
        }

        private async void initPos(object sender, RoutedEventArgs e)
        {
            // center on Taiwan     
            var twCenter =
                new Geopoint(new BasicGeoposition()
                {
                    Latitude = 23.973875,
                    Longitude = 120.982024

                });

            foreach(var s in app.sites)
            {
                addMapIcon(s);
            }

            // retrieve map
            await map.TrySetSceneAsync(MapScene.CreateFromLocationAndRadius(twCenter, 110e3));
        }

        private void addMapIcon(Site site)
        {
            var gp = new Geopoint(new BasicGeoposition { Latitude = site.twd97Lat, Longitude = site.twd97Lon });
            // Create a MapIcon.
            MapIcon mapIcon1 = new MapIcon();
            mapIcon1.Location = gp;
            mapIcon1.NormalizedAnchorPoint = new Point(0.5, 1.0);
            mapIcon1.Title = site.siteName + ":" + site.Pm2_5;
            mapIcon1.ZIndex = 0;

            // Add the MapIcon to the map.
            map.MapElements.Add(mapIcon1);

            // Center the map over the POI.
            //map.Center = gp;
        }
    }
}
