using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class Coordinate
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public long NodeID { get; set; }

        public Coordinate(double lat, double lon)
        {
            Latitude = lat;
            Longitude = lon;
            NodeID = 0;
        }

        public Coordinate(long id, double lat, double lon)
        {
            NodeID = id;
            Latitude = lat;
            Longitude = lon;
        }

        public bool Compare(Coordinate c)
        {
            const double DIFF_THRESHOLD = 0.00001;

            if ((NodeID > 0) && (c.NodeID > 0))
            {
                if (NodeID == c.NodeID)
                {
                    return true;
                }
            }

            double diff_lat = Math.Abs(c.Latitude - Latitude);
            double diff_lon = Math.Abs(c.Longitude - Longitude);

            return ((diff_lat < DIFF_THRESHOLD) && (diff_lon < DIFF_THRESHOLD));
        }

        public bool CompareID(Coordinate c)
        {
            if ((NodeID > 0) && (c.NodeID > 0))
            {
                if (NodeID == c.NodeID)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
