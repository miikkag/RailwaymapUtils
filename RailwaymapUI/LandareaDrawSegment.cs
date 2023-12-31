﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public enum CrossDirection { Unknown, ToLand, ToWater };

    public class LandareaDrawSegment
    {
        public readonly PolygonNode Start;
        public readonly PolygonNode End;

        public double LatMax;
        public double LatMin;

        public double LonMax;
        public double LonMin;

        public double CrossMercX;
        public int CrossMapX;

        public double MercXMax;
        public double MercXMin;

        public CrossDirection CrossDir;

        public LandareaDrawSegment(double lat1, double lon1, double lat2, double lon2)
        {
            Start = new PolygonNode(lat1, lon1);
            End = new PolygonNode(lat2, lon2);

            LatMax = Math.Max(lat1, lat2);
            LatMin = Math.Min(lat1, lat2);

            LonMax = Math.Max(lon1, lon2);
            LonMin = Math.Min(lon1, lon2);

            double mercx1 = Commons.Merc_X(lon1);
            double mercx2 = Commons.Merc_X(lon2);

            MercXMin = Math.Min(mercx1, mercx2);
            MercXMax = Math.Max(mercx1, mercx2);

            CrossMercX = 0;
            CrossMapX = -1;

            CrossDir = CrossDirection.Unknown;
        }

        public void CalculateCrossPoint(double merc_y, BoundsXY bounds)
        {
            if (Math.Abs(Start.MercX - End.MercX)>1.0)
            {
                double k = (Start.MercY - End.MercY) / (Start.MercX - End.MercX);

                CrossMercX = Start.MercX + ((merc_y - Start.MercY) / k);
            }
            else
            {
                CrossMercX = Start.MercX;
            }

            CrossMapX = (int)Math.Round(bounds.Scale * (CrossMercX - bounds.X_min));

            if (Start.Lat < End.Lat)
            {
                CrossDir = CrossDirection.ToWater;
            }
            else if (Start.Lat > End.Lat)
            {
                CrossDir = CrossDirection.ToLand;
            }
            else
            {
                CrossDir = CrossDirection.Unknown;
            }
        }

        public override string ToString()
        {
            return string.Format("MapX: " + CrossMapX.ToString() + "  Start: " + Start.ToString() + "  End: " + End.ToString() + " crossdir=" + CrossDir.ToString());
        }
    }


    public class LandareaDrawSegmentComparerLat : IComparer<LandareaDrawSegment>
    {
        public int Compare(LandareaDrawSegment s1, LandareaDrawSegment s2)
        {
            return s2.LatMax.CompareTo(s1.LatMax);
        }
    }

    public class LandareaDrawSegmentComparerCross : IComparer<LandareaDrawSegment>
    {
        public int Compare(LandareaDrawSegment s1, LandareaDrawSegment s2)
        {
            return s1.CrossMercX.CompareTo(s2.CrossMercX);
        }
    }
}
