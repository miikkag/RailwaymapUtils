using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class WayRail
    {
        public Int64 way_id;

        public bool electrified;
        public bool construction;
        public bool disused;
        public int voltage;
        public int gauge1;
        public int gauge2;
        public int tracks;
        public bool lightrail;
        public string usage;

        public WayRail(Int64 id)
        {
            way_id = id;
            electrified = false;
            construction = false;
            disused = false;
            voltage = 0;
            gauge1 = 0;
            gauge2 = 0;
            tracks = 1;
            lightrail = false;
            usage = "";
        }
    }
}
