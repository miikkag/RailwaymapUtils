using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class Bounds
    {
        public double Lat_min;
        public double Lat_max;
        public double Lon_min;
        public double Lon_max;

        public Bounds()
        {
            Lat_min = double.MaxValue;
            Lat_max = double.MinValue;
            Lon_min = double.MaxValue;
            Lon_max = double.MinValue;
        }

        public Bounds(double latitude_min, double latitude_max, double longitude_min, double longitude_max)
        {
            Lat_min = latitude_min;
            Lat_max = latitude_max;
            Lon_min = longitude_min;
            Lon_max = longitude_max;
        }

        public void Try_Lat(double lat_try)
        {
            if (lat_try < Lat_min)
            {
                Lat_min = lat_try;
            }

            if (lat_try > Lat_max)
            {
                Lat_max = lat_try;
            }
        }

        public void Try_Lon(double lon_try)
        {
            if (lon_try < Lon_min)
            {
                Lon_min = lon_try;
            }

            if (lon_try > Lon_max)
            {
                Lon_max = lon_try;
            }
        }

        public override string ToString()
        {
            string result = string.Format("LAT_min={0}  LAT_max={1}  LON_min={2}  LON_max={3}", Lat_min, Lat_max, Lon_min, Lon_max);

            return (result);
        }
    }
}
