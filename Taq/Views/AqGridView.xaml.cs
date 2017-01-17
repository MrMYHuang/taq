using System.Collections.ObjectModel;
using System.Linq;
using Taq.Shared.Models;
using Taq.Shared.ModelViews;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Taq.Uwp.Views
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
        public int id;

        public AqGridView(ObservableCollection<AqViewModel> _aqvms, int _id)
        {
            app = App.Current as App;
            rootFrame = Window.Current.Content as Frame;
            mainPage = rootFrame.Content as MainPage;
            id = _id;
            aqvms = _aqvms;
            this.InitializeComponent();
        }

        private void gv_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!(e.OriginalSource is TextBlock || e.OriginalSource is Border))
            {
                return;
            }

            var aqName = StaticTaqModel.fieldNames.Keys.ToList()[gv.SelectedIndex];
            // Check whether the AQ name support history.
            if (app.vm.m.aqHistNames.FindIndex(v => v == aqName) == -1)
            {
                return;
            }

            var p = new object[]
            {
                app.vm.m.subscrSiteList[id],
                aqName
            };
            mainPage.frame.Navigate(typeof(AqHistories), p);
        }
    }
}
