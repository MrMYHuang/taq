using System;
using System.Threading.Tasks;
using TaqShared;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Taq.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    // For updating UI after TaqBackTask downloads a new XML.

    public sealed partial class Home : Page
    {
        public App app;

        public Home()
        {
            app = App.Current as App;
            this.InitializeComponent();
        }
    }
}
