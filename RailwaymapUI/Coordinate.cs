﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class Coordinate
    {
        public double Latitude;
        public double Longitude;

        public Coordinate(double lat, double lon)
        {
            Latitude = lat;
            Longitude = lon;
        }
    }
}
