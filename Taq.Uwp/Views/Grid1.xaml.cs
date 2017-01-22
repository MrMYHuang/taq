using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Taq.Shared.ModelViews;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Taq.Uwp.Views
{
    public sealed partial class Grid1 : UserControl
    {
        AqViewModel aqvm;
        string aqName;
        string subscript;
        string unit;
        public Grid1(AqViewModel _aqvm, string _aqName, string _subscript, string _unit = "")
        {
            aqvm = _aqvm;
            aqName = _aqName;
            subscript = _subscript;
            unit = _unit;
            this.InitializeComponent();
        }
    }
}
