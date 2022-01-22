using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class LakeSegment
    {
        public readonly PolygonNode Start;
        public readonly PolygonNode End;

        public readonly double LatMax;
        public readonly double LatMin;
        public readonly double LonMax;
        public readonly double LonMin;

        public double CrossMercX;
        public int CrossMapX;


        public LakeSegment(double lat1, double lon1, double lat2, double lon2)
        {
            Start = new PolygonNode(lat1, lon1);
            End = new PolygonNode(lat2, lon2);

            LatMax = Math.Max(lat1, lat2);
            LatMin = Math.Min(lat1, lat2);

            LonMax = Math.Max(lon1, lon2);
            LonMin = Math.Min(lon1, lon2);

            CrossMercX = 0;
            CrossMapX = -1;
        }

        public LakeSegment(BinaryReader reader)
        {
            try
            {
                Start = new PolygonNode(reader);
                End = new PolygonNode(reader);

                LatMax = reader.ReadDouble();
                LatMin = reader.ReadDouble();
                LonMax = reader.ReadDouble();
                LonMin = reader.ReadDouble();

                CrossMercX = 0;
                CrossMapX = -1;
            }
            catch (Exception ex)
            {
                throw new Exception("LakeSegment: error reading cache: " + ex.Message);
            }
        }

        public bool Compare(LakeSegment seg)
        {
            return (Start.Lon == seg.Start.Lon) &&
                (Start.Lat == seg.Start.Lat) &&
                (End.Lon == seg.End.Lon) &&
                (End.Lat == seg.End.Lat);
        }

        public void WriteCache(BinaryWriter writer)
        {
            Start.Write_Cache(writer);
            End.Write_Cache(writer);

            writer.Write(LatMax);
            writer.Write(LatMin);
            writer.Write(LonMax);
            writer.Write(LonMin);
        }

        public void CalculateCrossPoint(double merc_y, BoundsXY bounds)
        {
            if (Math.Abs(Start.MercX - End.MercX) > 1.0)
            {
                double k = (Start.MercY - End.MercY) / (Start.MercX - End.MercX);

                CrossMercX = Start.MercX + ((merc_y - Start.MercY) / k);
            }
            else
            {
                CrossMercX = Start.MercX;
            }

            CrossMapX = (int)Math.Round(bounds.Scale * (CrossMercX - bounds.X_min));
        }
    }

    public class LakeSegmentComparerLat : IComparer<LakeSegment>
    {
        public int Compare(LakeSegment s1, LakeSegment s2)
        {
            return s2.LatMax.CompareTo(s1.LatMax);
        }
    }

    public class LakeSegmentComparerCross : IComparer<LakeSegment>
    {
        public int Compare(LakeSegment s1, LakeSegment s2)
        {
            return s1.CrossMercX.CompareTo(s2.CrossMercX);
        }
    }

}
