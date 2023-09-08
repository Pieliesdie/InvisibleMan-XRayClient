using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

using MaterialDesignExtensions.Controls;

namespace InvisibleManXRay.Windows;

public partial class BaseWindow : MaterialWindow
{
    public BaseWindow() : base()
    {
        Background = this.FindResource("MaterialDesignPaper") as Brush;
        TextElement.SetForeground(this, this.FindResource("MaterialDesignBody") as Brush);
        FontFamily = FindResource("SegoeUI") as FontFamily;
        TextElement.SetFontWeight(this, FontWeights.Medium);
    }
}
