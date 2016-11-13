using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace TaqShared.Models
{
    /*public class Pm2_5_Info : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }*/

    public class Site : INotifyPropertyChanged
    {

        public string siteName;
        private string county { get; set; }
        private string pm2_5 { get; set; }
        public double twd97Lat;
        public double twd97Lon;

        public string Pm2_5
        {
            get
            {
                return this.pm2_5;
            }

            set
            {
                if (value != this.pm2_5)
                {
                    if (value == "")
                    {
                        this.pm2_5 = "0";
                    }
                    else
                    {
                        this.pm2_5 = value;

                    }

                    var i = 0;
                    for (; i < Shared.pm2_5_concens.Length; i++)
                    {
                        if (int.Parse(pm2_5) <= Shared.pm2_5_concens[i])
                        {
                            break;
                        }
                    }
                    CircleColor = Shared.pm2_5_colors[i];
                    NotifyPropertyChanged();
                }
            }
        }

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

        public SolidColorBrush TextColor
        {
            get
            {
                var i = 0;
                if (int.Parse(Pm2_5) > 47)
                {
                    return new SolidColorBrush(Colors.White);
                }
                return new SolidColorBrush(Colors.Black);
            }
        }

        public string County
        {
            get
            {
                return county;
            }
            set
            {
                county = value;
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
