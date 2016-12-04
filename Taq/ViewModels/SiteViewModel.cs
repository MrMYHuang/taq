using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace Taq
{
    public class SiteViewModel : AqViewModel, INotifyPropertyChanged
    {
        public string siteName;
        public string county;
        public double twd97Lat;
        public double twd97Lon;
    }
}
