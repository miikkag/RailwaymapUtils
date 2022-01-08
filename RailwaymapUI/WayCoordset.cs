using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class WayCoordset
    {
        public Coordinate[] Coords;

        private List<Coordinate> coordlist;

        public WayCoordset()
        {
            coordlist = new List<Coordinate>();
        }

        public void AddItem(Coordinate c)
        {
            coordlist.Add(c);
        }

        public void Ready()
        {
            Coords = coordlist.ToArray();
            coordlist.Clear();
        }
    }
}
