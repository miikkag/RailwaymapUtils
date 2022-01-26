using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class MapImage_Railways : MapImage
    {
        public void Draw(SQLiteConnection conn, BoundsXY bounds, ProgressInfo progress, DrawSettings set, bool clear_first)
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
                    else
                    {
                        draw = set.Draw_Railway_Spur;
                    }

                    if (draw)
                    {
                        WaySet ws = Commons.Generate_Wayset_Single(conn, wr.way_id);

                        double filter = set.Filter_Railways_Line;

                        bool thick = (wr.tracks > 1);

                        bool filter_drawline = set.Filter_Railways_DrawLine;

                        Color color;

                        if (wr.disused)
                        {
                            color = set.Color_Railways_Disused;
                        }
                        else if (wr.construction)
                        {
                            color = set.Color_Railways_Construction;
                        }
                        else if (wr.gauge2 > 0)
                        {
                            color = set.Color_Railways_Dualgauge;
                        }
                        else if ((wr.gauge1 > 0) && (wr.gauge1 < set.Normal_Gauge_Min))
                        {
                            if (wr.electrified)
                            {
                                color = set.Color_Railways_Narrow_Electric;
                            }
                            else
                            {
                                color = set.Color_Railways_Narrow_Diesel;
                            }
                        }
                        else if (!wr.electrified)
                        {
                            // Non-electrified
                            color = set.Color_Railways_Diesel;
                        }
                        else
                        {
                            switch (wr.voltage)
                            {
                                case 1500:
                                    color = set.Color_Railways_1500V;
                                    break;

                                case 3000:
                                    color = set.Color_Railways_3000V;
                                    break;

                                case 15000:
                                    color = set.Color_Railways_15kV;
                                    break;

                                case 25000:
                                    color = set.Color_Railways_25kV;
                                    break;

                                default:
                                    color = set.Color_Railways_Elecrified_other;
                                    break;
                            }
                        }

                        Draw_Way_Coordinates(ws, null, filter, thick, color, bounds, filter_drawline);
                    }
                }
            }

            progress.Clear();
        }
    }
}
