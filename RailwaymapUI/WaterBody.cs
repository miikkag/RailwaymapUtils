using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public enum WaterBodyWayRole { Inner, Outer };

    public class WaterBody
    {
        public Int64 Id;

        public List<Way> Ways;

        public List<LakeSegment> Segments;

        public CoordinateMinMax MinMax;

        public WaterBody(Int64 id)
        {
            Id = id;

            Ways = new List<Way>();
            Segments = new List<LakeSegment>();
            MinMax = new CoordinateMinMax();
        }

        public WaterBody(BinaryReader reader)
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
                throw new Exception("WaterBody: error reading cache: " + ex.Message);
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


        public void Read_Nodes(List<Int64> way_ids, SQLiteConnection conn)
        {
            for (int w = 0; w < way_ids.Count; w++)
            {
                Way way = new Way();

                using (SQLiteCommand cmd_nodes = new SQLiteCommand("SELECT node_id FROM way_nodes WHERE way_id=$id;", conn))
                {
                    cmd_nodes.Parameters.AddWithValue("$id", way_ids[w]);

                    using (SQLiteDataReader rdr_nodes = cmd_nodes.ExecuteReader())
                    {
                        while (rdr_nodes.Read())
                        {
                            Int64 node_id = rdr_nodes.GetInt64(0);

                            using (SQLiteCommand cmd_coords = new SQLiteCommand("SELECT lat,lon FROM nodes WHERE id=$id;", conn))
                            {
                                cmd_coords.Parameters.AddWithValue("$id", node_id);

                                using (SQLiteDataReader rdr_coords = cmd_coords.ExecuteReader())
                                {
                                    if (rdr_coords.Read())
                                    {
                                        double lat = rdr_coords.GetDouble(0);
                                        double lon = rdr_coords.GetDouble(1);

                                        way.Add_Node(node_id, lat, lon);
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine("Coordinates for node " + node_id.ToString() + " not found");
                                    }
                                }
                            }
                        }
                    }
                }

                Add_Way(way);
            }
        }


        public void Draw_To_Bitmap(BoundsXY bounds, int min_areasize, Color draw_color, Bitmap bmp)
        {
            // Determine lake size
            int xmin = Commons.Merc2MapX(Commons.Merc_X(MinMax.LonMin), bounds);
            int xmax = Commons.Merc2MapX(Commons.Merc_X(MinMax.LonMax), bounds);
            int y1 = Commons.Merc2MapY(Commons.Merc_Y(MinMax.LatMin), bounds);
            int y2 = Commons.Merc2MapY(Commons.Merc_Y(MinMax.LatMax), bounds);

            int ymax = Math.Max(y1, y2);
            int ymin = Math.Min(y1, y2);

            int areasize = Math.Max(xmax - xmin, ymax - ymin);

            //System.Diagnostics.Debug.WriteLine(string.Format("xmin={0} xmax={1} ymin={2} ymax={3}  LatMin={4} LatMax={5} LonMin={6} LonMax={7}",
            //    xmin, xmax, ymin, ymax,
            //    lake.MinMax.LatMin, lake.MinMax.LatMax, lake.MinMax.LonMin, lake.MinMax.LonMax));

            if (ymax >= bmp.Height)
            {
                ymax = bmp.Height - 1;
            }

            if (ymin < 0)
            {
                ymin = 0;
            }

            if (areasize > min_areasize)
            {
                //System.Diagnostics.Debug.WriteLine("Drawing lake: size=" + areasize.ToString() + " segments=" + lake.Segments.Count.ToString() +
                //    String.Format("  y={0}-{1} x={2}-{3}", ymin, ymax, xmin, xmax));

                for (int y = ymin; y <= ymax; y++)
                {
                    double y_unscaled = y / bounds.Scale;
                    double y_merc = bounds.Y_max - y_unscaled;

                    double lat = Commons.Merc_Y2Lat(y_merc);

                    // Find segments that cross this latitude
                    List<LakeSegment> crosslist = new List<LakeSegment>();

                    bool run = true;
                    int s = 0;

                    while (run)
                    {
                        if (Segments[s] != null)
                        {
                            if ((Segments[s].LatMax >= lat) && (Segments[s].LatMin <= lat))
                            {
                                crosslist.Add(Segments[s]);
                            }
                            else if (Segments[s].LatMin > lat)
                            {
                                // Passed this segment already -- can be removed
                                Segments[s] = null;
                            }
                            else if (Segments[s].LatMax < lat)
                            {
                                // Segment out of reach, list is sorted based on LatMax so no more crossing segments will be found
                                run = false;
                            }
                            else
                            {
                                // Do Nothing
                            }
                        }

                        s++;

                        if (s >= Segments.Count)
                        {
                            run = false;
                        }
                    }

                    if ((crosslist.Count > 0) && (crosslist.Count % 2) == 0)
                    {
                        double step_merc_y = Commons.Merc_Y(lat);

                        for (int c = 0; c < crosslist.Count; c++)
                        {
                            crosslist[c].CalculateCrossPoint(step_merc_y, bounds);
                        }

                        // Sort based on Lon crossing point (x coordinate)
                        crosslist.Sort(new LakeSegmentComparerCross());

                        bool draw = true;
                        int prevx = crosslist[0].CrossMapX;

                        for (int c = 1; c < crosslist.Count; c++)
                        {
                            int currentx = crosslist[c].CrossMapX;

                            if (currentx > prevx)
                            {
                                if (draw)
                                {
                                    for (int x = prevx; x <= currentx; x++)
                                    {
                                        if ((x >= 0) && (x < bmp.Width))
                                        {
                                            bmp.SetPixel(x, y, draw_color);
                                        }
                                    }
                                }
                            }
                            else if (prevx > currentx)
                            {
                                System.Diagnostics.Debug.WriteLine("Invalid cross list waterbody=" + Id.ToString() + " lat=" + lat.ToString());
                            }

                            prevx = currentx;

                            draw = !draw;
                        }
                    }
                    else
                    {
                        if (crosslist.Count > 0)
                        {
                            System.Diagnostics.Debug.WriteLine("Waterbody " + Id.ToString() + " lat=" + lat.ToString() + " cross count " + crosslist.Count.ToString());
                        }
                    }
                }
            }
        }
    }
}
