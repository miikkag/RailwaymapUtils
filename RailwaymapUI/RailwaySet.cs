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
        public readonly List<WayRail> ways;

        public RailwaySet()
        {
            ways = new List<WayRail>();
        }

        public void Add_Item(WayRail item)
        {
            ways.Add(item);
        }
    }
}
