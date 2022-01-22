using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class CoordinateMinMax
    {
        public double LatMin;
        public double LatMax;
        public double LonMin;
        public double LonMax;

        public CoordinateMinMax()
        {
            LatMin = double.MaxValue;
            LatMax = double.MinValue;
            LonMin = double.MaxValue;
            LonMax = double.MinValue;
        }

        public CoordinateMinMax(double lat_min, double lat_max, double lon_min, double lon_max)
        {
            LatMin = lat_min;
            LatMax = lat_max;
            LonMin = lon_min;
            LonMax = lon_max;
        }
    }
}
