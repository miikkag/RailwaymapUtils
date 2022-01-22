using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class Coordinate
    {
        public double Latitude;
        public double Longitude;

        public Coordinate(double lat, double lon)
        {
            Latitude = lat;
            Longitude = lon;
        }

        public bool Compare(Coordinate c)
        {
            const double DIFF_THRESHOLD = 0.00001;

            double diff_lat = Math.Abs(c.Latitude - Latitude);
            double diff_lon = Math.Abs(c.Longitude - Longitude);

            return ((diff_lat < DIFF_THRESHOLD) && (diff_lon < DIFF_THRESHOLD));
        }
    }
}
