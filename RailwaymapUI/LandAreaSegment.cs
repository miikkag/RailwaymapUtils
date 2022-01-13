using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class LandAreaSegment
    {
        public double Lat1;
        public double Lat2;
        public double Lon1;
        public double Lon2;
        public bool Island;
        public bool Islet;

        public LandAreaSegment(double lat1, double lon1, double lat2, double lon2, bool island, bool islet)
        {
            Lat1 = lat1;
            Lat2 = lat2;
            Lon1 = lon1;
            Lon2 = lon2;
            Island = island;
            Islet = islet;
        }
    }
}
