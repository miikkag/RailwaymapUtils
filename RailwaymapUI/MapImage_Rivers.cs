using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class MapImage_Rivers : MapImage
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
                throw new Exception("River cache file does not exist.");
            }


            gr.Clear(Color.Transparent);

            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

            progress.Set_Info(true, "Reading river cache", 0);
            DateTime last_progress = DateTime.Now;

            List<WaterBody> rivers_inner = new List<WaterBody>();
            List<WaterBody> rivers_outer = new List<WaterBody>();
            List<WaterBody> rivers_single = new List<WaterBody>();
            List<Waterway> waterways = new List<Waterway>();

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

                        UInt64 descriptor = reader.ReadUInt64();

                        if (descriptor == RiverCache.RIVER_OUTER_DESCRIPTOR)
                        {
                            rivers_outer.Add(new WaterBody(reader));
                        }
                        else if (descriptor == RiverCache.RIVER_INNER_DESCRIPTOR)
                        {
                            rivers_inner.Add(new WaterBody(reader));
                        }
                        else if (descriptor == RiverCache.RIVER_SINGLEWAY_DESCRIPTOR)
                        {
                            rivers_single.Add(new WaterBody(reader));
                        }
                        else if (descriptor == RiverCache.RIVER_WATERWAY_DESCRIPTOR)
                        {
                            waterways.Add(new Waterway(reader));
                        }
                        else
                        {
                            throw new Exception("Invalid river descriptor: " + descriptor.ToString("X"));
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

            System.Diagnostics.Debug.WriteLine(string.Format("River cache data: outer:{0} inner:{1} single:{2} waterways:{3}",
                rivers_outer.Count, rivers_inner.Count, rivers_single.Count, waterways.Count));


            int total_items = rivers_outer.Count + rivers_inner.Count + rivers_single.Count + waterways.Count;

            if (total_items == 0)
            {
                total_items = 1;
            }

            int base_items = 0;

            if (set.Draw_Rivers_Waterbodies)
            {
                progress.Set_Info(true, "Drawing rivers (outer)", 0);

                for (int i = 0; i < rivers_outer.Count; i++)
                {
                    if ((i % 50) == 0)
                    {
                        if ((DateTime.Now - last_progress).TotalMilliseconds > 500)
                        {
                            progress.Set_Info((i * 100) / total_items);

                            last_progress = DateTime.Now;
                        }
                    }

                    if (rivers_outer[i].Segments.Count > 0)
                    {
                        rivers_outer[i].Draw_To_Bitmap(bounds, set.Filter_RiverBody_Area, set.Color_Water, bmp);
                    }
                }

                base_items = rivers_outer.Count;

                progress.Set_Info(true, "Drawing rivers (inner)", 0);

                for (int i = 0; i < rivers_inner.Count; i++)
                {
                    if ((i % 50) == 0)
                    {
                        if ((DateTime.Now - last_progress).TotalMilliseconds > 200)
                        {
                            progress.Set_Info(((base_items + i) * 100) / total_items);

                            last_progress = DateTime.Now;
                        }
                    }

                    if (rivers_inner[i].Segments.Count > 0)
                    {
                        rivers_inner[i].Draw_To_Bitmap(bounds, set.Filter_RiverLand_Area, set.Color_Land, bmp);
                    }
                }

                base_items += rivers_inner.Count;

                progress.Set_Info(true, "Drawing rivers (single)", 0);

                for (int i = 0; i < rivers_single.Count; i++)
                {
                    if ((i % 50) == 0)
                    {
                        if ((DateTime.Now - last_progress).TotalMilliseconds > 200)
                        {
                            progress.Set_Info(((base_items + i) * 100) / total_items);

                            last_progress = DateTime.Now;
                        }
                    }

                    if (rivers_single[i].Segments.Count > 0)
                    {
                        rivers_single[i].Draw_To_Bitmap(bounds, set.Filter_RiverBody_Area, set.Color_Water, bmp);
                    }
                }
            }

            if (set.Draw_Rivers_Waterways)
            {
                progress.Set_Info(true, "Drawing rivers (waterways)", 0);

                List<List<Coordinate>> draw_coors = new List<List<Coordinate>>();

                for (int i = 0; i < waterways.Count; i++)
                {
                    draw_coors.Add(waterways[i].Coordinates);
                }

                Draw_Way_Coordinates(draw_coors, progress, set.Filter_Waterway_Line, 2, set.Color_Water, bounds, false);
            }

            rivers_inner.Clear();
            rivers_outer.Clear();
            rivers_single.Clear();
            waterways.Clear();

            GC.Collect();
        }

        public void Save_ImageCache(string imgname_cache, string db_filename)
        {
            bmp.Save(imgname_cache, ImageFormat.Png);

            File.SetLastWriteTime(imgname_cache, File.GetLastWriteTime(db_filename));
        }
    }
}
