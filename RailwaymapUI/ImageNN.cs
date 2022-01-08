using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace RailwaymapUI
{
    public class ImageNN : System.Windows.Controls.Image
    {
        protected override void OnRender(DrawingContext dc)
        {
            this.VisualBitmapScalingMode = System.Windows.Media.BitmapScalingMode.NearestNeighbor;
            base.OnRender(dc);
        }
    }
}
