using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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
        public string pm2_5 { get; set; }
        public int pm2_5_int { get; set; }
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
                    int val = 0;
                    if (!int.TryParse(value, out val))
                    {
                        this.pm2_5 = "N/A";
                        this.pm2_5_int = 0;
                    }
                    else
                    {
                        this.pm2_5 = value;
                        this.pm2_5_int = val;
                    }

                    var i = 0;
                    for (; i < Shared.pm2_5_concens.Count; i++)
                    {
                        if (pm2_5_int <= Shared.pm2_5_concens[i])
                        {
                            break;
                        }
                    }
                    CircleColor = Shared.pm2_5_colors[i];
                    NotifyPropertyChanged();
                }
            }
        }

        public int aqi_int;
        private string aqi;
        public string Aqi
        {
            get
            {
                return aqi;
            }

            set
            {
                int val = 0;
                if (!int.TryParse(value, out val))
                {
                    aqi = "N/A";
                    aqi_int = 0;
                }
                else
                {
                    aqi = value;
                    aqi_int = val;
                }
                NotifyPropertyChanged();
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

        public string listText;
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

        public SolidColorBrush TextColor
        {
            get
            {
                if (pm2_5_int > 47)
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
