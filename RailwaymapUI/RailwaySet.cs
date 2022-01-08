using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class RailwaySet
    {
        private List<WayRail> ways_list;
        public WayRail[] ways;

        public RailwaySet()
        {
            ways_list = new List<WayRail>();
            ways = null;
        }

        public void Add_Item(WayRail item)
        {
            ways_list.Add(item);
        }

        public void Ready()
        {
            ways = ways_list.ToArray();
            ways_list.Clear();
        }
    }
}
