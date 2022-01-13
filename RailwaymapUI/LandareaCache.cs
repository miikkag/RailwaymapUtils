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
        private const string TIMESTAMP_KEY = "db_timestamp";

        public static bool Check_Cache_DB(string filename_db, string filename_cache)
        {
            bool result = false;

            if (File.Exists(filename_cache))
            {
                DateTime db_timestamp = File.GetLastWriteTime(filename_db);

                string timestamp_expect = db_timestamp.ToString("yyyy-MM-dd HH':'mm':'ss");

                System.Diagnostics.Debug.WriteLine("Check_Cache_DB: Expect timestamp: " + timestamp_expect);

                try
                {
                    using (SQLiteConnection conn = new SQLiteConnection("Data Source=" + filename_cache + "; Read Only=True"))
                    {
                        conn.Open();

                        using (SQLiteCommand cmd_settings = new SQLiteCommand("SELECT key,val FROM settings;", conn))
                        {
                            SQLiteDataReader rdr_settings = cmd_settings.ExecuteReader();

                            while (rdr_settings.Read())
                            {
                                string key = rdr_settings.GetString(0);

                                if (key == TIMESTAMP_KEY)
                                {
                                    string timestamp_cached = rdr_settings.GetString(1);

                                    System.Diagnostics.Debug.WriteLine("Check_Cache_DB: item found: " + timestamp_cached);

                                    if (timestamp_expect == timestamp_cached)
                                    {
                                        System.Diagnostics.Debug.WriteLine("Check_Cache_DB: timestamp match ok");
                                        result = true;
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine("Check_Cache_DB " + filename_db + " - mismatch: " + timestamp_expect + " -- " + timestamp_cached);
                                    }
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine("Check_Cache_DB: unknown key: " + key);
                                }
                            }
                        }

                        conn.Close();
                        conn.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Check_Cache_DB failed: " + ex.Message);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Check_Cache_DB: File does not exists: " + filename_cache);
            }

            GC.Collect();

            return (result);
        }

        public static void Convert_Coastline(string filename_db, string filename_cache, ProgressInfo progress)
        {
            progress.Set_Info(true, "Converting land area", 0);

            DateTime db_timestamp = File.GetLastWriteTime(filename_db);

            if (!File.Exists(filename_cache))
            {
                SQLiteConnection.CreateFile(filename_cache);
            }

            using (SQLiteConnection conn_src = new SQLiteConnection("Data Source=" + filename_db + "; Read Only=True"))
            using (SQLiteConnection conn_cache = new SQLiteConnection("Data Source=" + filename_cache))
            {
                conn_src.Open();
                conn_cache.Open();

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

                using (SQLiteCommand cmd_cache = new SQLiteCommand("DROP TABLE IF EXISTS segments;", conn_cache))
                {
                    cmd_cache.ExecuteNonQuery();
                }

                using (SQLiteCommand cmd_cache = new SQLiteCommand("DROP TABLE IF EXISTS settings;", conn_cache))
                {
                    cmd_cache.ExecuteNonQuery();
                }

                using (SQLiteCommand cmd_cache = new SQLiteCommand("VACUUM;", conn_cache))
                {
                    cmd_cache.ExecuteNonQuery();
                }

                using (SQLiteCommand cmd_cache = new SQLiteCommand("CREATE TABLE segments ( id INTEGER PRIMARY KEY, lat1 REAL, lon1 REAL, lat2 REAL, lon2 REAL, island BOOLEAN, islet BOOLEAN );", conn_cache))
                {
                    cmd_cache.ExecuteNonQuery();
                }

                using (SQLiteCommand cmd_cache = new SQLiteCommand("CREATE TABLE settings ( key STRING, val STRING );", conn_cache))
                {
                    cmd_cache.ExecuteNonQuery();
                }

                using (SQLiteCommand cmd_cache = new SQLiteCommand("INSERT INTO settings ( key, val ) VALUES ( $key, $val );", conn_cache))
                {
                    cmd_cache.Parameters.AddWithValue("$key", TIMESTAMP_KEY);
                    cmd_cache.Parameters.AddWithValue("$val", db_timestamp.ToString("yyyy-MM-dd HH':'mm':'ss"));

                    cmd_cache.ExecuteNonQuery();
                }

                List<LandAreaSegment> segments = new List<LandAreaSegment>();


                for (int i = 0; i < ways.Count; i++)
                {
                    if ((i % 100) == 0)
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
                                            Console.WriteLine("Landarea: Node " + node_id.ToString() + " not found");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                ways.Clear();

                using (SQLiteTransaction tr = conn_cache.BeginTransaction())
                {
                    using (SQLiteCommand cmd_insert = new SQLiteCommand("INSERT INTO segments (lat1, lon1, lat2, lon2, island, islet) VALUES ( $lat1, $lon1, $lat2, $lon2, $island, $islet );", conn_cache))
                    {
                        progress.Set_Info(true, "Saving segment cache", 0);

                        for (int i = 0; i < segments.Count; i++)
                        {
                            if ((i % 100) == 0)
                            {
                                // Update progress
                                if ((DateTime.Now - last_progress).TotalMilliseconds >= 200)
                                {
                                    progress.Set_Info((i * 100 / segments.Count));

                                    last_progress = DateTime.Now;
                                }
                            }

                            cmd_insert.Parameters.AddWithValue("$lat1", segments[i].Lat1);
                            cmd_insert.Parameters.AddWithValue("$lon1", segments[i].Lon1);
                            cmd_insert.Parameters.AddWithValue("$lat2", segments[i].Lat2);
                            cmd_insert.Parameters.AddWithValue("$lon2", segments[i].Lon2);
                            cmd_insert.Parameters.AddWithValue("$island", segments[i].Island);
                            cmd_insert.Parameters.AddWithValue("$islet", segments[i].Islet);

                            cmd_insert.ExecuteNonQuery();
                        }
                    }

                    tr.Commit();
                }

                segments.Clear();

                conn_cache.Close();
                conn_src.Close();
            }
        }
    }
}
