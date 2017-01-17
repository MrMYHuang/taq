using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Taq.Shared.Views
{
    public sealed partial class MedTile : UserControl
    {
        Brush textColor;
        public MedTile(Brush _textColor)
        {
            this.InitializeComponent();
            textColor = _textColor;
        }

        public Brush TextColor
        {
            get
            {
                return textColor;
            }
        }
    }
}
