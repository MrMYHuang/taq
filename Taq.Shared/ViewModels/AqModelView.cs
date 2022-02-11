using Microsoft.Practices.Prism.Mvvm;
using Windows.UI.Xaml.Media;

namespace Taq.Shared.ViewModels
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
                if (value != circleColor)
                {
                    SetProperty(ref circleColor, value);
                }
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
                if (value != circleText)
                {
                    SetProperty(ref circleText, value);
                }
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
                if (value != listText)
                {
                    SetProperty(ref listText, value);
                }
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
                if (value != textColor)
                {
                    SetProperty(ref textColor, value);
                }
            }
        }
    }
}
