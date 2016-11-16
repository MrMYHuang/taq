using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using System.Xml.Linq;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

using Taq.Views;
using TaqShared;
using Windows.Storage;
using Windows.Networking.BackgroundTransfer;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.ApplicationModel.Core;
using Windows.Globalization;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Taq
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 

    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            frame.Navigate(typeof(Home));
        }

        private void HamburgerButton_Click(Object sender, RoutedEventArgs e)
        {
            MySplitView.IsPaneOpen = !MySplitView.IsPaneOpen;
        }

        private void aboutButton_Click(Object sender, RoutedEventArgs e)
        {
            frame.Navigate(typeof(About));
        }

        private void homeButton_Click(Object sender, RoutedEventArgs e)
        {
            frame.Navigate(typeof(Home));
        }

        private void verButton_Click(Object sender, TappedRoutedEventArgs e)
        {
            frame.Navigate(typeof(Ver));
        }

        private void mapButton_Click(Object sender, TappedRoutedEventArgs e)
        {
            frame.Navigate(typeof(AqSiteMap));
        }

        private void shareBtn_Click(Object sender, TappedRoutedEventArgs e)
        {
            Windows.ApplicationModel.DataTransfer.DataTransferManager.ShowShareUI();
        }

        private void setButton_Click(Object sender, TappedRoutedEventArgs e)
        {
            frame.Navigate(typeof(Settings));
        }
    }
}
