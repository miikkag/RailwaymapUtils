using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class MapImage_Landarea : MapImage
    {
        private enum LandOrWater { Unknown = 0,Land=1, Water=2 };

        public void Draw(SQLiteConnection conn, BoundsXY bxy, ProgressInfo progress, DrawSettings set)
        {
            if ((gr == null) || (conn == null))
            {
                return;
            }

            gr.Clear(Color.Transparent);

            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

            System.Diagnostics.Debug.WriteLine(bxy.ToString());

            progress.Set_Info(true, "Processing land area cache", 0);

            DateTime last_progress = DateTime.Now;

            List<LandareaDrawSegment> segments = new List<LandareaDrawSegment>();

            int rowcount = 0;

            using (SQLiteCommand cmd_count = new SQLiteCommand("SELECT COUNT() FROM segments;", conn))
            using (SQLiteDataReader rdr_count = cmd_count.ExecuteReader())
            {
                if (rdr_count.Read())
                {
                    rowcount = rdr_count.GetInt32(0);
                }

                rdr_count.Close();
                cmd_count.Dispose();
            }

            using (SQLiteCommand cmd_segments = new SQLiteCommand("SELECT lat1,lon1,lat2,lon2,island,islet FROM segments;", conn))
            using (SQLiteDataReader rdr_segments = cmd_segments.ExecuteReader())
            {
                int i = 0;

                while (rdr_segments.Read())
                {
                    if ((i % 1000) == 0)
                    {
                        if ((DateTime.Now - last_progress).TotalMilliseconds > 200)
                        {
                            progress.Set_Info((i * 100) / rowcount);

                            last_progress = DateTime.Now;
                        }
                    }

                    bool island = rdr_segments.GetBoolean(4);
                    bool islet = rdr_segments.GetBoolean(5);

                    bool draw_this = true;

                    if (island && !set.Draw_Landarea_Islands)
                    {
                        draw_this = false;
                    }

                    if (islet && !set.Draw_Landarea_Islets)
                    {
                        draw_this = false;
                    }

                    if (draw_this)
                    {
                        double lat1 = rdr_segments.GetDouble(0);
                        double lon1 = rdr_segments.GetDouble(1);
                        double lat2 = rdr_segments.GetDouble(2);
                        double lon2 = rdr_segments.GetDouble(3);

                        segments.Add(new LandareaDrawSegment(lat1, lon1, lat2, lon2, bxy));
                    }

                    i++;
                }

                rdr_segments.Close();
                cmd_segments.Dispose();
            }

            System.Diagnostics.Debug.WriteLine("segments count: " + segments.Count.ToString());

            // Sort segments based on latitude
            segments.Sort(new LandareaDrawSegmentComparerLat());

            progress.Set_Info(true, "Drawing land area", 0);

            Color nocross_color = Color.Transparent;
            LandOrWater landwater = LandOrWater.Unknown;
            int[] landwatercount = new int[3];

            int backfill_until = -1;

            for (int y = 0; y < bmp.Height; y++)
            {
                if ((DateTime.Now - last_progress).TotalMilliseconds >= 200)
                {
                    progress.Set_Info((y * 100) / bmp.Height);

                    last_progress = DateTime.Now;
                }

                double y_unscaled = y / bxy.Scale;
                double y_merc = bxy.Y_max - y_unscaled;

                double lat = Commons.Merc_Y2Lat(y_merc);

                // Find segments that cross this latitude
                List<LandareaDrawSegment> crosslist = new List<LandareaDrawSegment>();

                bool run = true;
                int s = 0;

                while (run)
                {
                    if (segments[s] != null)
                    {
                        if ((segments[s].LatMax >= lat) && (segments[s].LatMin <= lat))
                        {
                            crosslist.Add(segments[s]);
                        }
                        else if (segments[s].LatMin > lat)
                        {
                            // Passed this segment already -- can be removed
                            segments[s] = null;
                        }
                        else if (segments[s].LatMax < lat)
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

                    if (s >= segments.Count)
                    {
                        run = false;
                    }
                }

                if (crosslist.Count > 0)
                {
                    landwatercount[(int)LandOrWater.Unknown] = 0;
                    landwatercount[(int)LandOrWater.Land] = 0;
                    landwatercount[(int)LandOrWater.Water] = 0;

                    double step_merc_y = Commons.Merc_Y(lat);

                    for (int c = 0; c < crosslist.Count; c++)
                    {
                        crosslist[c].CalculateCrossPoint(step_merc_y, bxy);
                    }

                    // Sort based on Lon crossing point (x coordinate)
                    crosslist.Sort(new LandareaDrawSegmentComparerCross());

                    // Draw from left edge to first segment
                    Color drawcolor;

                    if (crosslist[0].CrossDir == CrossDirection.ToWater)
                    {
                        // Draw land
                        drawcolor = set.Color_Land;    // TODO
                        landwater = LandOrWater.Land;
                    }
                    else if (crosslist[0].CrossDir == CrossDirection.ToLand)
                    {
                        // Draw Water
                        drawcolor = set.Color_Water;
                        landwater = LandOrWater.Water;
                    }
                    else
                    {
                        drawcolor = set.Color_DebugPink;
                        landwater = LandOrWater.Unknown;
                    }

                    //System.Diagnostics.Debug.WriteLine("y=" + draw_y.ToString() + " lat=" + lat.ToString() + "  " + crosslist[0].Cross.ToString());

                    int prevx = 0;

                    for (int c = 0; c < crosslist.Count; c++)
                    {
                        int currentx = crosslist[c].CrossMapX;

                        for (int x = prevx; x < currentx; x++)
                        {
                            if ((x >= 0) && (x < bmp.Width))
                            {
                                bmp.SetPixel(x, y, drawcolor);
                                landwatercount[(int)landwater]++;
                            }
                            else
                            {
                                //System.Diagnostics.Debug.WriteLine("y=" + draw_y.ToString() + " lat=" + lat.ToString() + " cross " + c.ToString() + " draw invalid x=" + x.ToString());
                            }
                        }

                        if (crosslist[c].CrossDir == CrossDirection.ToWater)
                        {
                            drawcolor = set.Color_Water;
                        }
                        else if (crosslist[c].CrossDir == CrossDirection.ToLand)
                        {
                            drawcolor = set.Color_Land;
                        }
                        else
                        {
                            drawcolor = set.Color_DebugPink;
                        }

                        prevx = currentx;
                    }

                    for (int x = prevx; x < bmp.Width; x++)
                    {
                        if (x >= 0)
                        {
                            bmp.SetPixel(x, y, drawcolor);
                            landwatercount[(int)landwater]++;
                        }
                        else
                        {
                            //System.Diagnostics.Debug.WriteLine("y=" + draw_y.ToString() + " lat=" + lat.ToString() + " final draw invalid x=" + x.ToString());
                        }
                    }

                    if (landwatercount[(int)LandOrWater.Land] > landwatercount[(int)LandOrWater.Water])
                    {
                        nocross_color = set.Color_Land;
                    }
                    else
                    {
                        nocross_color = set.Color_Water;
                    }

                    if (backfill_until >= 0)
                    {
                        // Do backfill
                        for (int bfy = 0; bfy <= backfill_until; bfy++)
                        {
                            for (int x = 0; x < bmp.Width; x++)
                            {
                                bmp.SetPixel(x, bfy, nocross_color);
                            }
                        }

                        backfill_until = -1;
                    }
                }
                else
                {
                    // No crossing segments
                    if (nocross_color != Color.Transparent)
                    {
                        for (int x = 0; x < bmp.Width - 0; x++)
                        {
                            bmp.SetPixel(x, y, nocross_color);
                        }
                    }
                    else
                    {
                        backfill_until = y;
                    }
                }
            }

            progress.Clear();
        }
    }
}
