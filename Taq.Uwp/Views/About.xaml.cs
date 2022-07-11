using Syncfusion.UI.Xaml.Charts;
using System.Reflection;
using Taq.Shared.Models;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Taq.Uwp.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class About : Page
    {

        public App app;
        public About()
        {
            app = App.Current as App;
            this.InitializeComponent();
            this.DataContext = this;
        }

        public string mailUri
        {
            get
            {
                return "mailto:myh@live.com?subject=問題回報&body=TAQ版本：" + app.vm.Version;
            }
        }

        public string syncfusionVer
        {
            get
            {
                return typeof(SfChart).GetTypeInfo().Assembly.GetName().Version.ToString();
            }
        }

        public string MapPrivacyUri
        {
            get
            {
                return Params.uriHost + "MapPrivacy.html";
            }
        }
    }
}
