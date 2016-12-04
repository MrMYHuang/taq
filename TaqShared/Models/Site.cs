using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace TaqShared.Models
{
    public class AqView : INotifyPropertyChanged
    {
        // Map icon background color.
        public string circleColor;
        public string CircleColor
        {
            get
            {
                return circleColor;
            }
            set
            {
                circleColor = value;
                NotifyPropertyChanged();
            }
        }

        public string circleText;
        public string CircleText
        {
            get
            {
                return circleText;
            }
            set
            {
                circleText = value;
                NotifyPropertyChanged();
            }
        }

        private string listText;
        public string ListText
        {
            get
            {
                return listText;
            }
            set
            {
                listText = value;
                NotifyPropertyChanged();
            }
        }

        private SolidColorBrush textColor;
        public SolidColorBrush TextColor
        {
            get
            {
                return textColor;
            }
            set
            {
                textColor = value;
                NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class Site : AqView, INotifyPropertyChanged
    {
        public string siteName;
        public string county;
        public double twd97Lat;
        public double twd97Lon;
    }
}
