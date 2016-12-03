using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Media;

namespace TaqShared
{
    public class AqView : INotifyPropertyChanged
    {        // Map icon background color.
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

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

}
