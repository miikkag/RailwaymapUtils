using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class LandareaNode
    {
        public double MercX;
        public double MercY;
        public double Lat;
        public double Lon;

        public LandareaNode(double lat, double lon, BoundsXY bounds)
        {
            Lat = lat;
            Lon = lon;

            MercX = Commons.Merc_X(lon);
            MercY = Commons.Merc_Y(lat);
        }

        public override string ToString()
        {
            return ("Lat=" + Lat.ToString() + " Lon=" + Lon.ToString() + " MercX=" + MercX.ToString() + " MercY=" + MercY.ToString());
        }
    }
}
