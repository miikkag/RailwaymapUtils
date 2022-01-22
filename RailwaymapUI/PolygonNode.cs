using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class PolygonNode
    {
        public double MercX;
        public double MercY;
        public double Lat;
        public double Lon;

        public PolygonNode(double lat, double lon)
        {
            Lat = lat;
            Lon = lon;

            MercX = Commons.Merc_X(lon);
            MercY = Commons.Merc_Y(lat);
        }

        public PolygonNode(BinaryReader reader)
        {
            try
            {
                Lat = reader.ReadDouble();
                Lon = reader.ReadDouble();
                MercX = reader.ReadDouble();
                MercY = reader.ReadDouble();
            }
            catch (Exception ex)
            {
                throw new Exception("PolygonNode: error reading cache: " + ex.Message);
            }
        }

        public void Write_Cache(BinaryWriter writer)
        {
            writer.Write(Lat);
            writer.Write(Lon);
            writer.Write(MercX);
            writer.Write(MercY);
        }

        public override string ToString()
        {
            return ("Lat=" + Lat.ToString() + " Lon=" + Lon.ToString() + " MercX=" + MercX.ToString() + " MercY=" + MercY.ToString());
        }
    }
}
