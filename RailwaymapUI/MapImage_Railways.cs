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
    public class MapImage_Railways : MapImage
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


        public void Draw(SQLiteConnection conn, BoundsXY bounds, ProgressInfo progress, DrawSettings set, RailwayLegend legend, bool clear_first)
        {
            progress.Set_Info(true, "Processing railways", 0);

            DateTime last_progress = DateTime.Now;

            if ((gr != null) && (conn != null))
            {
                if (clear_first)
                {
                    gr.Clear(Color.Transparent);
                }

                gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

                RailwaySet rs = new RailwaySet();

                using (SQLiteCommand cmd_ways = new SQLiteCommand("SELECT id FROM ways;", conn))
                using (SQLiteDataReader rdr_ways = cmd_ways.ExecuteReader())
                {
                    while (rdr_ways.Read())
                    {
                        Int64 way_id = rdr_ways.GetInt64(0);

                        WayRail wr = new WayRail(way_id);

                        using (SQLiteCommand cmd_tags = new SQLiteCommand("SELECT k,v FROM way_tags WHERE way_id=$id;", conn))
                        {
                            cmd_tags.Parameters.AddWithValue("$id", way_id);

                            using (SQLiteDataReader rdr_tags = cmd_tags.ExecuteReader())
                            {
                                while (rdr_tags.Read())
                                {
                                    string k = rdr_tags.GetString(0);
                                    string v = rdr_tags.GetString(1);

                                    switch (k)
                                    {
                                        case "electrified":
                                            if (v != "no")
                                            {
                                                wr.electrified = true;
                                            }
                                            break;

                                        case "voltage":
                                            int.TryParse(v, out wr.voltage);

                                            if (wr.voltage > 0)
                                            {
                                                wr.electrified = true;
                                            }
                                            break;

                                        case "gauge":
                                            if (v.Contains(";"))
                                            {
                                                // Dual gauge
                                                string[] parts = v.Split(';');

                                                if (parts.Length > 1)
                                                {
                                                    int.TryParse(parts[0], out wr.gauge1);
                                                    int.TryParse(parts[1], out wr.gauge2);
                                                }
                                            }
                                            else
                                            {
                                                int.TryParse(v, out wr.gauge1);
                                            }
                                            break;

                                        case "railway":
                                            if (v == "narrow_gauge")
                                            {
                                                wr.gauge1 = 1;
                                            }
                                            else if (v == "light_rail")
                                            {
                                                wr.lightrail = true;
                                            }
                                            else if (v == "station")
                                            {
                                                wr.station = true;
                                            }
                                            break;

                                        case "usage":
                                            wr.usage = v;
                                            break;

                                        case "passenger_lines":
                                            int.TryParse(v, out wr.tracks);
                                            break;

                                        case "disused":
                                            if (v == "yes")
                                            {
                                                wr.disused = true;
                                            }
                                            break;

                                        case "construction":
                                            if (v == "rail")
                                            {
                                                wr.construction = true;
                                            }
                                            else if (v == "light_rail")
                                            {
                                                wr.construction = true;
                                                wr.lightrail = true;
                                            }
                                            break;
                                    }
                                }
                            }
                        }

                        rs.Add_Item(wr);
                    }
                }

                progress.Set_Info(true, "Processing railways", 0);

                var items_single = new Dictionary<RailwayType, List<List<List<Coordinate>>>>();
                var items_dual = new Dictionary<RailwayType, List<List<List<Coordinate>>>>();

                foreach (RailwayType rt in Enum.GetValues(typeof(RailwayType)))
                {
                    items_single.Add(rt, new List<List<List<Coordinate>>>());
                    items_dual.Add(rt, new List<List<List<Coordinate>>>());
                }

                int total = rs.ways.Count;

                for (int i = 0; i < rs.ways.Count; i++)
                {
                    WayRail wr = rs.ways[i];

                    bool draw;

                    if (wr.station)
                    {
                        draw = false;
                    }
                    else if (wr.usage == "main" || wr.usage == "branch")
                    {
                        draw = true;
                    }
                    else if (wr.usage == "tourism")
                    {
                        draw = set.Draw_Railway_Tourism || set.Draw_Railway_All;
                    }
                    else if (wr.usage == "")
                    {
                        draw = set.Draw_Railway_Unset || set.Draw_Railway_All;
                    }
                    else
                    {
                        draw = set.Draw_Railway_All;
                    }

                    if (draw)
                    {
                        List<List<Coordinate>> ws = Commons.Generate_Wayset_Single(conn, wr.way_id);

                        Dictionary<RailwayType, List<List<List<Coordinate>>>> use_items;

                        if (wr.tracks > 1)
                        {
                            use_items = items_dual;
                        }
                        else
                        {
                            use_items = items_single;
                        }

                        if (wr.disused)
                        {
                            use_items[RailwayType.Disused].Add(ws); ;
                        }
                        else if (wr.construction)
                        {
                            if ((wr.gauge1 > 0) && (wr.gauge1 < set.Normal_Gauge_Min))
                            {
                                use_items[RailwayType.Narrow_Construction].Add(ws);
                            }
                            else
                            {
                                use_items[RailwayType.Normal_Construction].Add(ws);
                            }
                        }
                        else if (wr.gauge2 > 0)
                        {
                            use_items[RailwayType.Dual_Gauge].Add(ws);
                        }
                        else if ((wr.gauge1 > 0) && (wr.gauge1 < set.Normal_Gauge_Min))
                        {
                            if (wr.electrified)
                            {
                                use_items[RailwayType.Narrow_Electrified].Add(ws);
                            }
                            else
                            {
                                use_items[RailwayType.Narrow_Non_Electrified].Add(ws);
                            }
                        }
                        else if (!wr.electrified)
                        {
                            // Non-electrified
                            use_items[RailwayType.Normal_Non_Electrified].Add(ws);
                        }
                        else
                        {
                            switch (wr.voltage)
                            {
                                case 750:
                                    use_items[RailwayType.Normal_Electrified_750V].Add(ws);
                                    break;

                                case 1500:
                                    use_items[RailwayType.Normal_Electrified_1500V].Add(ws);
                                    break;

                                case 3000:
                                    use_items[RailwayType.Normal_Electrified_3000V].Add(ws);
                                    break;

                                case 15000:
                                    use_items[RailwayType.Normal_Electrified_15kV].Add(ws);
                                    break;

                                case 25000:
                                    use_items[RailwayType.Normal_Electrified_25kV].Add(ws);
                                    break;

                                default:
                                    use_items[RailwayType.Normal_Electrified_Other].Add(ws);
                                    break;
                            }
                        }

                        if ((DateTime.Now - last_progress).TotalMilliseconds >= 500)
                        {
                            progress.Set_Info((i * 100 / total));

                            last_progress = DateTime.Now;
                        }
                    }
                }

                progress.Set_Info(true, "Drawing railways", 0);

                int t = 0;

                foreach (RailwayType rt in Enum.GetValues(typeof(RailwayType)))
                {
                    if (items_dual[rt].Count > 0)
                    {
                        legend.Set_UsedType(rt);

                        Color color = Commons.Get_Draw_Color(rt, set);

                        for (int i = 0; i < items_dual[rt].Count; i++)
                        {
                            Draw_Way_Coordinates(items_dual[rt][i], null, set.Filter_Railways_Line, 3, color, bounds, set.Filter_Railways_DrawLine);

                            t++;

                            if ((DateTime.Now - last_progress).TotalMilliseconds >= 500)
                            {
                                progress.Set_Info((t * 100 / total));

                                last_progress = DateTime.Now;
                            }
                        }
                    }
                }

                foreach (RailwayType rt in Enum.GetValues(typeof(RailwayType)))
                {
                    if (items_single[rt].Count > 0)
                    {
                        legend.Set_UsedType(rt);

                        Color color = Commons.Get_Draw_Color(rt, set);

                        for (int i = 0; i < items_single[rt].Count; i++)
                        {
                            Draw_Way_Coordinates(items_single[rt][i], null, set.Filter_Railways_Line, 1, color, bounds, set.Filter_Railways_DrawLine);

                            t++;

                            if ((DateTime.Now - last_progress).TotalMilliseconds >= 500)
                            {
                                progress.Set_Info((t * 100 / total));

                                last_progress = DateTime.Now;
                            }
                        }
                    }
                }
            }

            progress.Clear();
        }

        public void Save_ImageCache(string imgname_cache, string db_filename)
        {
            bmp.Save(imgname_cache, ImageFormat.Png);

            File.SetLastWriteTime(imgname_cache, File.GetLastWriteTime(db_filename));
        }
    }
}
