using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class LakeCache
    {
        public static readonly UInt64 LAKE_OUTER_DESCRIPTOR = 0xF0DEBC9A78563412;
        public static readonly UInt64 LAKE_INNER_DESCRIPTOR = 0xF1DEBC9A78563412;

        public static void Convert_Lakes(string filename_db, string filename_cache, ProgressInfo progress)
        {
            progress.Set_Info(true, "Reading lakes", 0);

            string db_timestamp_str = File.GetLastWriteTime(filename_db).ToString("yyyy-MM-dd HH':'mm':'ss");

            using (FileStream fs = File.OpenWrite(filename_cache))
            using (BinaryWriter writer = new BinaryWriter(fs, Encoding.UTF8, false))
            {
                writer.Write(@db_timestamp_str);

                DateTime last_progress = DateTime.Now;

                List<Int64> relations = new List<Int64>();

                List<Lake> lakes_inner = new List<Lake>();
                List<Lake> lakes_outer = new List<Lake>();

                int total_items;
                int base_items;

                using (SQLiteConnection sqlite_lakes = new SQLiteConnection("Data Source=" + filename_db))
                {
                    sqlite_lakes.Open();

                    using (SQLiteCommand cmd_rels = new SQLiteCommand("SELECT id FROM relations;", sqlite_lakes))
                    using (SQLiteDataReader rdr_rels = cmd_rels.ExecuteReader())
                    {
                        while (rdr_rels.Read())
                        {
                            Int64 id = rdr_rels.GetInt64(0);

                            relations.Add(id);
                        }

                        rdr_rels.Close();
                        cmd_rels.Dispose();
                    }

                    for (int i = 0; i < relations.Count; i++)
                    {
                        if ((i % 50) == 0)
                        {
                            if ((DateTime.Now - last_progress).TotalMilliseconds > 200)
                            {
                                progress.Set_Info((i * 100) / relations.Count);
                                last_progress = DateTime.Now;
                            }
                        }

                        List<Int64> members_outer = new List<Int64>();
                        List<Int64> members_inner = new List<Int64>();

                        using (SQLiteCommand cmd_members = new SQLiteCommand("SELECT way_id,role FROM relation_members WHERE relation_id=$id;", sqlite_lakes))
                        {
                            cmd_members.Parameters.AddWithValue("$id", relations[i]);

                            using (SQLiteDataReader rdr_members = cmd_members.ExecuteReader())
                            {
                                while (rdr_members.Read())
                                {
                                    Int64 way_id = rdr_members.GetInt64(0);
                                    string role = rdr_members.GetString(1);

                                    if (role == "inner")
                                    {
                                        members_inner.Add(way_id);
                                    }
                                    else if (role == "outer")
                                    {
                                        members_outer.Add(way_id);
                                    }
                                    else
                                    {
                                        // Ignore
                                    }
                                }

                                rdr_members.Close();
                            }
                        }

                        Lake tmp_lake_inner = new Lake(relations[i]);
                        Lake tmp_lake_outer = new Lake(relations[i]);

                        Read_Nodes(members_outer, tmp_lake_outer, sqlite_lakes);
                        Read_Nodes(members_inner, tmp_lake_inner, sqlite_lakes);

                        lakes_inner.Add(tmp_lake_inner);
                        lakes_outer.Add(tmp_lake_outer);
                    }

                    relations.Clear();


                    progress.Set_Info(true, "Processing lakes (outer)", 0);
                    last_progress = DateTime.Now;

                    total_items = lakes_outer.Count + lakes_inner.Count;
                    base_items = 0;

                    for (int i = 0; i < lakes_outer.Count; i++)
                    {
                        if ((i % 50) == 0)
                        {
                            if ((DateTime.Now - last_progress).TotalMilliseconds > 200)
                            {
                                progress.Set_Info(((base_items + i) * 100) / total_items);

                                last_progress = DateTime.Now;
                            }
                        }

                        lakes_outer[i].Ways = Commons.Combine_Ways(lakes_outer[i].Ways);
                        lakes_outer[i].Make_Segments();
                    }

                    base_items = lakes_outer.Count;
                    progress.Set_Info(true, "Processing lakes (inner)", (base_items * 100) / total_items);

                    for (int i = 0; i < lakes_inner.Count; i++)
                    {
                        if ((i % 50) == 0)
                        {
                            if ((DateTime.Now - last_progress).TotalMilliseconds > 200)
                            {
                                progress.Set_Info(((base_items + i) * 100) / total_items);
                                last_progress = DateTime.Now;
                            }
                        }

                        lakes_inner[i].Ways = Commons.Combine_Ways(lakes_inner[i].Ways);
                        lakes_inner[i].Make_Segments();
                    }

                    sqlite_lakes.Close();
                    sqlite_lakes.Dispose();
                }

                GC.Collect();

                progress.Set_Info(true, "Saving lake cache", 0);

                System.Diagnostics.Debug.WriteLine("Lakes: " + lakes_outer.Count().ToString() + " " + lakes_inner.Count().ToString());

                total_items = lakes_outer.Count + lakes_inner.Count;
                base_items = 0;

                for (int i = 0; i < lakes_outer.Count; i++)
                {
                    if ((i % 50) == 0)
                    {
                        if ((DateTime.Now - last_progress).TotalMilliseconds > 200)
                        {
                            progress.Set_Info(((base_items + i) * 100) / total_items);
                            last_progress = DateTime.Now;
                        }
                    }

                    writer.Write(LAKE_OUTER_DESCRIPTOR);
                    lakes_outer[i].Write_Cache(writer);
                }

                base_items = lakes_outer.Count;

                for (int i = 0; i < lakes_inner.Count; i++)
                {
                    if ((i % 50) == 0)
                    {
                        if ((DateTime.Now - last_progress).TotalMilliseconds > 200)
                        {
                            progress.Set_Info(((base_items + i) * 100) / total_items);
                            last_progress = DateTime.Now;
                        }
                    }

                    writer.Write(LAKE_INNER_DESCRIPTOR);
                    lakes_inner[i].Write_Cache(writer);
                }
            }
        }


        private static void Read_Nodes(List<Int64> way_ids, Lake lake, SQLiteConnection conn)
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

                lake.Add_Way(way);
            }
        }
    }
}
