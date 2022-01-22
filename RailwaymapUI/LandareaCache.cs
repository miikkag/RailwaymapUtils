using System;
using System.IO;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace RailwaymapUI
{
    public class LandareaCache
    {
        public static void Convert_Coastline(string filename_db, string filename_cache, ProgressInfo progress)
        {
            progress.Set_Info(true, "Converting land area", 0);

            string db_timestamp_str = File.GetLastWriteTime(filename_db).ToString("yyyy-MM-dd HH':'mm':'ss");

            using (FileStream fs = File.OpenWrite(filename_cache))
            using (BinaryWriter writer = new BinaryWriter(fs, Encoding.UTF8, false))
            {
                writer.Write(@db_timestamp_str);

                using (SQLiteConnection conn_src = new SQLiteConnection("Data Source=" + filename_db + "; Read Only=True"))
                {
                    conn_src.Open();

                    DateTime last_progress = DateTime.Now;

                    List<Tuple<Int64, bool, bool>> ways = new List<Tuple<Int64, bool, bool>>();

                    using (SQLiteCommand cmd_ways = new SQLiteCommand("SELECT id FROM ways;", conn_src))
                    using (SQLiteDataReader rdr_ways = cmd_ways.ExecuteReader())
                    {
                        while (rdr_ways.Read())
                        {
                            Int64 way_id = rdr_ways.GetInt64(0);

                            bool island = false;
                            bool islet = false;

                            using (SQLiteCommand cmd_tags = new SQLiteCommand("SELECT k,v FROM way_tags WHERE way_id=$id;", conn_src))
                            {
                                cmd_tags.Parameters.AddWithValue("$id", way_id);

                                using (SQLiteDataReader rdr_tags = cmd_tags.ExecuteReader())
                                {
                                    while (rdr_tags.Read())
                                    {
                                        string k = rdr_tags.GetString(0);
                                        string v = rdr_tags.GetString(1);

                                        if (k == "place")
                                        {
                                            if (v == "island")
                                            {
                                                island = true;
                                            }
                                            else if (v == "islet")
                                            {
                                                islet = true;
                                            }
                                            else
                                            {
                                                // Ignore
                                            }
                                        }
                                    }
                                }
                            }

                            ways.Add(Tuple.Create(way_id, island, islet));
                        }
                    }

                    if (ways.Count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("Convert landarea: No ways found");

                        progress.Clear();

                        return;
                    }

                    List<LandAreaSegment> segments = new List<LandAreaSegment>();

                    for (int i = 0; i < ways.Count; i++)
                    {
                        if ((i % 200) == 0)
                        {
                            // Update progress
                            if ((DateTime.Now - last_progress).TotalMilliseconds >= 200)
                            {
                                progress.Set_Info((i * 100 / ways.Count));

                                last_progress = DateTime.Now;
                            }
                        }

                        using (SQLiteCommand cmd_waynodes = new SQLiteCommand("SELECT node_id FROM way_nodes WHERE way_id=$id;", conn_src))
                        {
                            cmd_waynodes.Parameters.AddWithValue("$id", ways[i].Item1);

                            double prev_lat = double.NaN;
                            double prev_lon = double.NaN;
                            bool prev_set = false;

                            using (SQLiteDataReader rdr_waynodes = cmd_waynodes.ExecuteReader())
                            {
                                while (rdr_waynodes.Read())
                                {
                                    Int64 node_id = rdr_waynodes.GetInt64(0);

                                    using (SQLiteCommand cmd_nodes = new SQLiteCommand("SELECT lat,lon FROM nodes WHERE id=$id;", conn_src))
                                    {
                                        cmd_nodes.Parameters.AddWithValue("$id", node_id);

                                        using (SQLiteDataReader rdr_nodes = cmd_nodes.ExecuteReader())
                                        {
                                            if (rdr_nodes.Read())
                                            {
                                                double lat = rdr_nodes.GetDouble(0);
                                                double lon = rdr_nodes.GetDouble(1);

                                                if (prev_set)
                                                {
                                                    segments.Add(new LandAreaSegment(prev_lat, prev_lon, lat, lon, ways[i].Item2, ways[i].Item3));
                                                }
                                                else
                                                {
                                                    prev_set = true;
                                                }

                                                prev_lat = lat;
                                                prev_lon = lon;
                                            }
                                            else
                                            {
                                                System.Diagnostics.Debug.WriteLine("Landarea: Node " + node_id.ToString() + " not found");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    ways.Clear();

                    progress.Set_Info(true, "Saving segment cache", 0);

                    for (int i = 0; i < segments.Count; i++)
                    {
                        if ((i % 200) == 0)
                        {
                            // Update progress
                            if ((DateTime.Now - last_progress).TotalMilliseconds >= 200)
                            {
                                progress.Set_Info((i * 100 / segments.Count));

                                last_progress = DateTime.Now;
                            }
                        }

                        writer.Write(segments[i].Lat1);
                        writer.Write(segments[i].Lon1);
                        writer.Write(segments[i].Lat2);
                        writer.Write(segments[i].Lon2);
                        writer.Write(segments[i].Island);
                        writer.Write(segments[i].Islet);
                    }

                    segments.Clear();
                    conn_src.Close();
                }
            }
        }
    }
}
