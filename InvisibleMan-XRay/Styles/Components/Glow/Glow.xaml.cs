using System.Windows;
using System.Windows.Controls;

namespace InvisibleManXRay.Components
{
    public partial class Glow : UserControl
    {
        public static readonly DependencyProperty GlowHeightProperty = DependencyProperty.Register("GlowHeight", typeof(int), typeof(Glow), new PropertyMetadata(50));

        public int GlowHeight
        {
            get => (int)GetValue(GlowHeightProperty);
            set => SetValue(GlowHeightProperty, value);
        }

        public static readonly DependencyProperty GlowWidthProperty = DependencyProperty.Register("GlowWidth", typeof(int), typeof(Glow), new PropertyMetadata(50));

        public int GlowWidth
        {
            get => (int)GetValue(GlowWidthProperty);
            set => SetValue(GlowWidthProperty, value);
        }

        public Glow()
        {
            InitializeComponent();
        }
    }
}
