using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class MapImage_Lakes : MapImage
    {
        public bool DrawFromCache(string filename_cache_img)
        {
            bool result = true;

            if (gr == null)
            {
                return false;
            }

            if (!File.Exists(filename_cache_img))
            {
                return false;
            }

            gr.Clear(Color.Transparent);
            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

            using (Image img = Image.FromFile(filename_cache_img))
            {
                if ((img.Width == bmp.Width) && (img.Height == bmp.Height))
                {
                    gr.DrawImage(Image.FromFile(filename_cache_img), 0, 0);
                }
                else
                {
                    result = false;
                }
            }

            return result;
        }

        public void Draw(string filename_cache, BoundsXY bounds, ProgressInfo progress, DrawSettings set)
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

            progress.Set_Info(true, "Reading lake cache", 0);
            DateTime last_progress = DateTime.Now;

            List<Lake> lakes_outer = new List<Lake>();
            List<Lake> lakes_inner = new List<Lake>();

            using (FileStream fs = File.OpenRead(filename_cache))
            using (BinaryReader reader = new BinaryReader(fs, Encoding.UTF8, false))
            {
                _ = reader.ReadString();    // DB Timestamp

                try
                {
                    long cache_size = fs.Length;

                    while (true)
                    {
                        if ((DateTime.Now - last_progress).TotalMilliseconds > 200)
                        {
                            progress.Set_Info((int)((100 * fs.Position) / cache_size));
                            last_progress = DateTime.Now;
                        }

                        UInt64 lake_descriptor = reader.ReadUInt64();

                        Lake tmp_lake = new Lake(reader);

                        if (lake_descriptor == LakeCache.LAKE_OUTER_DESCRIPTOR)
                        {
                            lakes_outer.Add(tmp_lake);
                        }
                        else if (lake_descriptor == LakeCache.LAKE_INNER_DESCRIPTOR)
                        {
                            lakes_inner.Add(tmp_lake);
                        }
                        else
                        {
                            throw new Exception("Invalid lake descriptor: " + lake_descriptor.ToString("X"));
                        }
                    }
                }
                catch (EndOfStreamException)
                {
                    // End of stream -- Do Nothing
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            System.Diagnostics.Debug.WriteLine("Lake count: outer=" + lakes_outer.Count.ToString() + "  inner=" + lakes_inner.Count.ToString());


            int total_items = lakes_outer.Count + lakes_inner.Count;

            progress.Set_Info(true, "Drawing lakes (outer)", 0);

            for (int i = 0; i < lakes_outer.Count; i++)
            {
                if ((i % 50) == 0)
                {
                    if ((DateTime.Now - last_progress).TotalMilliseconds > 200)
                    {
                        progress.Set_Info((i * 100) / total_items);
                    }
                }

                if (lakes_outer[i].Segments.Count > 0)
                {
                    Draw_Lake(lakes_outer[i], bounds, set.Filter_Water_Area, set.Color_Water);
                }
            }

            int base_items = lakes_outer.Count;

            if (set.Draw_WaterLand)
            {
                progress.Set_Info(true, "Drawing lakes (inner)", 0);

                for (int i = 0; i < lakes_inner.Count; i++)
                {
                    if ((i % 50) == 0)
                    {
                        if ((DateTime.Now - last_progress).TotalMilliseconds > 200)
                        {
                            progress.Set_Info(((base_items + i) * 100) / total_items);
                        }
                    }

                    if (lakes_inner[i].Segments.Count > 0)
                    {
                        Draw_Lake(lakes_inner[i], bounds, set.Filter_WaterLand_Area, set.Color_Land);
                    }
                }
            }

            lakes_inner.Clear();
            lakes_outer.Clear();

            GC.Collect();
        }

        public void Save_ImageCache(string imgname_cache, string db_filename)
        {
            bmp.Save(imgname_cache, ImageFormat.Png);

            File.SetLastWriteTime(imgname_cache, File.GetLastWriteTime(db_filename));
        }


        private void Draw_Lake(Lake lake, BoundsXY bounds, int min_areasize, Color draw_color)
        {
            // Determine lake size
            int xmin = Commons.Merc2MapX(Commons.Merc_X(lake.MinMax.LonMin), bounds);
            int xmax = Commons.Merc2MapX(Commons.Merc_X(lake.MinMax.LonMax), bounds);
            int y1 = Commons.Merc2MapY(Commons.Merc_Y(lake.MinMax.LatMin), bounds);
            int y2 = Commons.Merc2MapY(Commons.Merc_Y(lake.MinMax.LatMax), bounds);

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
                        if (lake.Segments[s] != null)
                        {
                            if ((lake.Segments[s].LatMax >= lat) && (lake.Segments[s].LatMin <= lat))
                            {
                                //AddIfNotDuplicate(lake.Segments[s], crosslist);
                                crosslist.Add(lake.Segments[s]);
                            }
                            else if (lake.Segments[s].LatMin > lat)
                            {
                                // Passed this segment already -- can be removed
                                lake.Segments[s] = null;
                            }
                            else if (lake.Segments[s].LatMax < lat)
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

                        if (s >= lake.Segments.Count)
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
                                System.Diagnostics.Debug.WriteLine("Invalid cross list lake=" + lake.Id.ToString() + " lat=" + lat.ToString());
                            }

                            prevx = currentx;

                            draw = !draw;
                        }
                    }
                    else
                    {
                        if (crosslist.Count > 0)
                        {
                            System.Diagnostics.Debug.WriteLine("Lake " + lake.Id.ToString() + " lat=" + lat.ToString() + " cross count " + crosslist.Count.ToString());
                        }
                    }
                }
            }
        }

        private static void AddIfNotDuplicate(LakeSegment seg, List<LakeSegment> segments)
        {
            bool found = false;

            for (int i = 0; i < segments.Count; i++)
            {
                if (seg.Compare(segments[i]))
                {
                    found = true;
                    System.Diagnostics.Debug.WriteLine("Duplicate segment");
                    break;
                }
            }

            if (!found)
            {
                segments.Add(seg);
            }
        }
    }
}
