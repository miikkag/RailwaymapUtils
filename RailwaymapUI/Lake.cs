using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public enum LakeWayRole { Inner, Outer };

    public class Lake
    {
        public Int64 Id;

        public List<Way> Ways;

        public List<LakeSegment> Segments;

        public CoordinateMinMax MinMax;

        public Lake(Int64 id)
        {
            Id = id;

            Ways = new List<Way>();
            Segments = new List<LakeSegment>();
            MinMax = new CoordinateMinMax();
        }

        public Lake(BinaryReader reader)
        {
            try
            {
                Id = reader.ReadInt64();

                Ways = new List<Way>();
                Segments = new List<LakeSegment>();
                MinMax = new CoordinateMinMax();

                MinMax.LatMin = reader.ReadDouble();
                MinMax.LatMax = reader.ReadDouble();
                MinMax.LonMin = reader.ReadDouble();
                MinMax.LonMax = reader.ReadDouble();

                int segment_count = reader.ReadInt32();

                for (int s = 0; s < segment_count; s++)
                {
                    Segments.Add(new LakeSegment(reader));
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Lake: error reading cache: " + ex.Message);
            }
        }

        public void Add_Way(Way w)
        {
            if (w.Coordinates.Count > 0)
            {
                Ways.Add(w);
            }
        }

        public void Write_Cache(BinaryWriter writer)
        {
            writer.Write(Id);

            writer.Write(MinMax.LatMin);
            writer.Write(MinMax.LatMax);
            writer.Write(MinMax.LonMin);
            writer.Write(MinMax.LonMax);

            writer.Write(Segments.Count);

            for (int s = 0; s < Segments.Count; s++)
            {
                Segments[s].WriteCache(writer);
            }
        }

        public void Make_Segments()
        {
            for (int w = 0; w < Ways.Count; w++)
            {
                for (int i = 1; i < Ways[w].Coordinates.Count; i++)
                {
                    LakeSegment seg = new LakeSegment(
                        Ways[w].Coordinates[i - 1].Latitude,
                        Ways[w].Coordinates[i - 1].Longitude,
                        Ways[w].Coordinates[i].Latitude,
                        Ways[w].Coordinates[i].Longitude);

                    Segments.Add(seg);
                }
            }


            Segments.Sort(new LakeSegmentComparerLat());

            for (int i = 0; i < Segments.Count; i++)
            {
                MinMax.LatMin = Math.Min(MinMax.LatMin, Segments[i].LatMin);
                MinMax.LatMax = Math.Max(MinMax.LatMax, Segments[i].LatMax);
                MinMax.LonMin = Math.Min(MinMax.LonMin, Segments[i].LonMin);
                MinMax.LonMax = Math.Max(MinMax.LonMax, Segments[i].LonMax);
            }

            Ways.Clear();
        }
    }
}
