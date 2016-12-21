using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
        ObservableCollection<AqViewModel> aqvms = new ObservableCollection<AqViewModel>();

        public AqGridView(ObservableCollection<AqViewModel> _aqvms)
        {
            app = App.Current as App;
            aqvms = _aqvms;
            this.InitializeComponent();
        }
    }
}
