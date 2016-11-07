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
    public class Site : INotifyPropertyChanged
    {
        static double[] pm2_5_concens = new double[] { 11, 23, 35, 41, 47, 53, 58, 64, 70 };
        static string[] colors = new string[] { "#9cff9c", "#31ff00", "#31cf00", "#ffff00", "#ffcf00", "#ff9a00", "#ff6464", "#ff0000", "#990000", "#ce30ff" };

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
                    this.pm2_5 = value;
                    NotifyPropertyChanged();
                }
            }
        }

        // Map icon background color.
        public string CircleColor
        {
            get
            {
                var i = 0;
                if (Pm2_5 != "")
                {
                    for (; i < pm2_5_concens.Length; i++)
                    {
                        if (double.Parse(Pm2_5) <= pm2_5_concens[i])
                        {
                            return colors[i];
                        }
                    }
                }
                return colors[i];
            }
        }

        public SolidColorBrush TextColor
        {
            get
            {
                var i = 0;
                if (Pm2_5 != "")
                {
                    if (double.Parse(Pm2_5) > 47)
                    {
                        return new SolidColorBrush(Colors.White);
                    }
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
