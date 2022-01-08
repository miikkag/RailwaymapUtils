using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class BoundsPoint
    {
        public int X_max;
        public int X_min;
        public int Y_max;
        public int Y_min;

        public BoundsPoint()
        {
            X_max = int.MinValue;
            Y_max = int.MinValue;
            X_min = int.MaxValue;
            Y_min = int.MaxValue;
        }

        public void TryXY(System.Drawing.Point pt)
        {
            if (pt.X < X_min)
            {
                X_min = pt.X;
            }

            if (pt.X > X_max)
            {
                X_max = pt.X;
            }

            if (pt.Y < Y_min)
            {
                Y_min = pt.Y;
            }

            if (pt.Y > Y_max)
            {
                Y_max = pt.Y;
            }
        }
    }
}
