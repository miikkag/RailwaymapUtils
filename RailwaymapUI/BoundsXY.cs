using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class BoundsXY
    {
        public double X_max;
        public double X_min;
        public double Y_max;
        public double Y_min;
        public double Scale;

        public BoundsXY(double xmax, double xmin, double ymax, double ymin, double scale)
        {
            X_max = xmax;
            X_min = xmin;
            Y_max = ymax;
            Y_min = ymin;

            Scale = scale;
        }

        public BoundsXY()
        {
            X_max = double.MinValue;
            Y_max = double.MinValue;
            X_min = double.MaxValue;
            Y_min = double.MaxValue;
        }

        public void TryXY(double x_try, double y_try)
        {
            if (x_try < X_min)
            {
                X_min = x_try;
            }

            if (x_try > X_max)
            {
                X_max = x_try;
            }

            if (y_try < Y_min)
            {
                Y_min = y_try;
            }

            if (y_try > Y_max)
            {
                Y_max = y_try;
            }
        }

        override public string ToString()
        {
            string result = string.Format("Xmin={0}  Xmax={1}  Ymin={2}  Ymax={3}", X_min, X_max, Y_min, Y_max);

            return result;
        }
    }
}
