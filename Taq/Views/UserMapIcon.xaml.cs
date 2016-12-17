using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Taq.Views
{
    public sealed partial class UserMapIcon : UserControl
    {
        public UserMapIcon()
        {
            this.InitializeComponent();
            accentAltBrush = new SolidColorBrush(Color.FromArgb(0xff, (byte)~accentBrush.Color.R, (byte)~accentBrush.Color.G, (byte)~accentBrush.Color.B));
        }

        SolidColorBrush accentBrush = Application.Current.Resources["SystemControlHighlightAccentBrush"] as SolidColorBrush;
        SolidColorBrush accentAltBrush;
        SolidColorBrush accentDisBrush = new SolidColorBrush(Colors.Gray);
        public void UserControl_IsEnabledChanged(object sender, Windows.UI.Xaml.DependencyPropertyChangedEventArgs e)
        {
            if (this.IsEnabled)
            {
                centerCircle.Fill = accentBrush;
            }
            else
            {
                centerCircle.Fill = accentDisBrush;
            }
        }

        private void centerCircle_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
                centerCircle.Fill = accentAltBrush;
        }

        private void centerCircle_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            centerCircle.Fill = accentBrush;
        }
    }
}
