using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class Way
    {
        public List<Coordinate> Coordinates;
        public bool error_unconnected;

        public Way()
        {
            Coordinates = new List<Coordinate>();
            error_unconnected = false;
        }

        public void Add_Node(double lat, double lon)
        {
            Coordinates.Add(new Coordinate(lat, lon));
        }
    }
}
