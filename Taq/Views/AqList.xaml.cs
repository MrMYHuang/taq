using TaqShared.ModelViews;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Linq;
using System;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238


namespace Taq.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AqList : Page
    {
        public App app;
        public Frame rootFrame;
        public MainPage mainPage;

        public AqList()
        {
            app = App.Current as App;
            rootFrame = Window.Current.Content as Frame;
            mainPage = rootFrame.Content as MainPage;
            this.InitializeComponent();
            this.DataContext = this;
        }

        private void aqComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            mainPage.aqComboBox_SelectionChanged(sender, e);
        }

        // For toggling between descending and ascending orderings.
        private bool desc = true;
        private void aqCol_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            Func<SiteViewModel, double> getSortElem;
            if (aqComboBox.SelectedValue.ToString() == "ShortStatus")
            {
                getSortElem = (svm => ((SiteViewModel)svm).aqi);
            }
            else
            {
                getSortElem = (svm => app.vm.m.getValidAqVal(((SiteViewModel)svm).ListText));
            }

            // Don't know why set null first, but it just works!
            listView.ItemsSource = null;
            if(desc)
            {
                listView.ItemsSource = app.vm.sites.OrderByDescending(getSortElem);
            }
            else
            {
                listView.ItemsSource = app.vm.sites.OrderBy(getSortElem);
            }
            desc = !desc;
        }
    }
}
