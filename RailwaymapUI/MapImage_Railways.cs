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

                rs.Ready();

                progress.Set_Info(true, "Drawing railways", 0);

                for (int i = 0; i < rs.ways.Length; i++)
                {
                    if ((DateTime.Now - last_progress).TotalMilliseconds >= 200)
                    {
                        progress.Set_Info((i * 100 / rs.ways.Length));

                        last_progress = DateTime.Now;
                    }

                    WayRail wr = rs.ways[i];

                    bool draw;

                    if (wr.usage == "main" || wr.usage == "branch")
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

                        double filter = set.Filter_Railways_Line;

                        int thickness = 1;

                        if (wr.tracks > 1)
                        {
                            thickness = 3;
                        }

                        bool filter_drawline = set.Filter_Railways_DrawLine;

                        Color color;
                        RailwayType draw_type;

                        if (wr.disused)
                        {
                            draw_type = RailwayType.Disused;
                        }
                        else if (wr.construction)
                        {
                            if ((wr.gauge1 > 0) && (wr.gauge1 < set.Normal_Gauge_Min))
                            {
                                draw_type = RailwayType.Narrow_Construction;
                            }
                            else
                            {
                                draw_type = RailwayType.Normal_Construction;
                            }
                        }
                        else if (wr.gauge2 > 0)
                        {
                            draw_type = RailwayType.Dual_Gauge;
                        }
                        else if ((wr.gauge1 > 0) && (wr.gauge1 < set.Normal_Gauge_Min))
                        {
                            if (wr.electrified)
                            {
                                draw_type = RailwayType.Narrow_Electrified;
                            }
                            else
                            {
                                draw_type = RailwayType.Narrow_Non_Electrified;
                            }
                        }
                        else if (!wr.electrified)
                        {
                            // Non-electrified
                            draw_type = RailwayType.Normal_Non_Electrified;
                        }
                        else
                        {
                            switch (wr.voltage)
                            {
                                case 750:
                                    draw_type = RailwayType.Normal_Electrified_750V;
                                    break;

                                case 1500:
                                    draw_type = RailwayType.Normal_Electrified_1500V;
                                    break;

                                case 3000:
                                    draw_type = RailwayType.Normal_Electrified_3000V;
                                    break;

                                case 15000:
                                    draw_type = RailwayType.Normal_Electrified_15kV;
                                    break;

                                case 25000:
                                    draw_type = RailwayType.Normal_Electrified_25kV;
                                    break;

                                default:
                                    draw_type = RailwayType.Normal_Electrified_Other;
                                    break;
                            }
                        }

                        legend.Set_UsedType(draw_type);

                        color = Commons.Get_Draw_Color(draw_type, set);

                        Draw_Way_Coordinates(ws, null, filter, thickness, color, bounds, filter_drawline);
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
