using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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
    }
}
