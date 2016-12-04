using Microsoft.Practices.Prism.Mvvm;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Media;

namespace Taq
{
    public class AqViewModel : BindableBase
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
                SetProperty(ref circleColor, value);
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
                SetProperty(ref circleText, value);
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
                SetProperty(ref listText, value);
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
                SetProperty(ref textColor, value);
            }
        }
    }
}
