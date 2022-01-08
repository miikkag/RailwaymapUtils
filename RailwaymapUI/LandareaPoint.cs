using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class LandareaPoint
    {
        public int x;
        public int y;
        public double lat;
        public double lon;

        public LandareaPoint(double set_lat, double set_lon, BoundsXY bounds)
        {
            lat = set_lat;
            lon = set_lon;

            x = (int)Math.Round(bounds.Y_Pad + (bounds.Scale * (bounds.Y_max - Commons.Merc_Y(lat))));
            y = (int)Math.Round(bounds.X_Pad + (bounds.Scale * (Commons.Merc_X(lon) - bounds.X_min)));
        }
    }
}
