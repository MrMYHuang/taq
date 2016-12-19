﻿using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace TaqShared
{
    public sealed partial class SmallTile : UserControl
    {
        Brush textColor;
        public SmallTile(Brush _textColor)
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
