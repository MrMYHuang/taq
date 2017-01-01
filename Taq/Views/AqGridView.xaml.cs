using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TaqShared.Models;
using TaqShared.ModelViews;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class AqGridView : Page
    {
        public App app;
        public Frame rootFrame;
        public MainPage mainPage;
        public ObservableCollection<AqViewModel> aqvms = new ObservableCollection<AqViewModel>();
        private string siteName;

        public AqGridView(ObservableCollection<AqViewModel> _aqvms, string _siteName)
        {
            app = App.Current as App;
            rootFrame = Window.Current.Content as Frame;
            mainPage = rootFrame.Content as MainPage;
            siteName = _siteName;
            aqvms = _aqvms;
            this.InitializeComponent();
        }

        private void gv_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var aqName = StaticTaqModel.fieldNames.Keys.ToList()[gv.SelectedIndex];
            // Check whether the AQ name support history.
            if (app.vm.m.aqHistNames.FindIndex(v => v == aqName) == -1)
            {
                return;
            }

            var p = new object[]
            {
                siteName,
                aqName
            };
            mainPage.frame.Navigate(typeof(AqHistories), p);
        }
    }
}
