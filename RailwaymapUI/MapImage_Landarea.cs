using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class MapImage_Landarea : MapImage
    {
        private enum LandOrWater { Unknown = 0,Land=1, Water=2 };

        public void Draw(string filename_cache, BoundsXY bxy, ProgressInfo progress, DrawSettings set)
        {
            if (gr == null)
            {
                return;
            }

            if (!File.Exists(filename_cache))
            {
                throw new Exception("Coastline cache file does not exist.");
            }

            gr.Clear(Color.Transparent);

            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

            System.Diagnostics.Debug.WriteLine(bxy.ToString());

            progress.Set_Info(true, "Reading coastline cache", 0);

            DateTime last_progress = DateTime.Now;

            List<LandareaDrawSegment> segments = new List<LandareaDrawSegment>();

            FileInfo info = new FileInfo(filename_cache);

            using (FileStream fs = File.OpenRead(filename_cache))
            using (BinaryReader reader = new BinaryReader(fs, Encoding.UTF8, false))
            {
                _ = reader.ReadString();    // DB Timestamp

                int itemsize = 4 * 8 + 2;   // 4x double, 2x bool
                int itemcount = (int) (info.Length / itemsize);

                for (int i = 0; i < itemcount; i++)
                {
                    if ((i % 1000) == 0)
                    {
                        if ((DateTime.Now - last_progress).TotalMilliseconds > 200)
                        {
                            progress.Set_Info((i * 100) / itemcount);

                            last_progress = DateTime.Now;
                        }
                    }

                    try
                    {
                        double lat1 = reader.ReadDouble();
                        double lon1 = reader.ReadDouble();
                        double lat2 = reader.ReadDouble();
                        double lon2 = reader.ReadDouble();
                        bool island = reader.ReadBoolean();
                        bool islet = reader.ReadBoolean();

                        bool draw_this = true;

                        if (!set.Draw_Landarea_Islands && island)
                        {
                            draw_this = false;
                        }

                        if (!set.Draw_Landarea_Islets && islet)
                        {
                            draw_this = false;
                        }

                        if (draw_this)
                        {
                            segments.Add(new LandareaDrawSegment(lat1, lon1, lat2, lon2));
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Coastline cache unexpected EOF, item " + i.ToString() + " of " + itemcount.ToString() + ": " + ex.Message );
                    }
                }
            }

            progress.Set_Info(true, "Sorting coastline segments", 0);

            //System.Diagnostics.Debug.WriteLine("segments count: " + segments.Count.ToString());

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

                    crosslist.Clear();
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

            segments.Clear();
            progress.Clear();

            GC.Collect();
        }
    }
}
