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
        }

        private void UserControl_IsEnabledChanged(object sender, Windows.UI.Xaml.DependencyPropertyChangedEventArgs e)
        {
            if (this.IsEnabled)
            {
                centerCircle.Fill = Application.Current.Resources["SystemControlHighlightAccentBrush"] as SolidColorBrush;
            }
            else
            {
                centerCircle.Fill = new SolidColorBrush(Colors.Gray);
            }
        }
    }
}
