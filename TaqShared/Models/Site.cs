using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TaqShared.Models
{
    public class Site : INotifyPropertyChanged
    {
        public string siteName;
        private string county { get; set; }
        private string pm2_5 { get; set; }

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
    }
}
