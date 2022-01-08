using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class WaySet
    {
        public WayCoordset[] WayCoordSets;

        private List<WayCoordset> waycoordsetlist;

        public WaySet()
        {
            waycoordsetlist = new List<WayCoordset>();
        }

        public void AddItem(WayCoordset c)
        {
            waycoordsetlist.Add(c);
        }

        public void Ready()
        {
            WayCoordSets = waycoordsetlist.ToArray();
            waycoordsetlist.Clear();
        }
    }
}
