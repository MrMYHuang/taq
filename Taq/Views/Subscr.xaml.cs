using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.StartScreen;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
    public sealed partial class Subscr : Page
    {
        public App app;

        public Subscr()
        {
            app = App.Current as App;
            this.InitializeComponent();
            initAux();
        }

        public async Task<int> initAux()
        {
            await app.vm.loadSubscrSiteViewModel();
            return 0;
        }

        private async void addButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if(subscrComboBox.SelectedIndex == -1)
            {
                return;
            }
            var siteName = ((SiteViewModel)subscrComboBox.SelectedValue).siteName;
            await app.vm.addSubscrSite(siteName);
        }

        private async void delButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (subscrGridView.SelectedItems.Count == 0)
            {
                return;
            }

            var itemsSelected = subscrGridView.SelectedItems.ToArray();
            foreach(SiteViewModel item in itemsSelected)
            {
                if(SecondaryTile.Exists(item.siteName))
                {
                    var st = new SecondaryTile(item.siteName);
                    await st.RequestDeleteForSelectionAsync(getElementRect((FrameworkElement)sender));
                }
            }
            await app.vm.delSubscrSite(itemsSelected);
        }

        public async void genSecondLiveTiles(object sender, TappedRoutedEventArgs e)
        {
            foreach (var siteName in app.vm.m.subscrSiteList)
            {
                var st = new SecondaryTile(siteName, siteName, siteName, new Uri("ms-appx:///Assets/logos/MediumTile.png"), TileSize.Default);
                st.VisualElements.Wide310x150Logo = new Uri("ms-appx:///Assets/logos/WideTile.png");
                st.VisualElements.Square310x310Logo = new Uri("ms-appx:///Assets/logos/LargeTile.png");
                await st.RequestCreateForSelectionAsync(getElementRect((FrameworkElement)sender));
            }
            await app.vm.backTaskUpdateTiles();
        }

        public static Windows.Foundation.Rect getElementRect(FrameworkElement element)
        {
            GeneralTransform buttonTransform = element.TransformToVisual(null);
            Point point = buttonTransform.TransformPoint(new Point());
            var r = new Rect(point, new Size(element.ActualWidth, element.ActualHeight));
            return r;
        }
    }
}
