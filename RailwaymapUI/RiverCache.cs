using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class RiverCache
    {
        public static readonly UInt64 RIVER_OUTER_DESCRIPTOR = 0xF2DEBC9A78563412;
        public static readonly UInt64 RIVER_INNER_DESCRIPTOR = 0xF3DEBC9A78563412;
        public static readonly UInt64 RIVER_SINGLEWAY_DESCRIPTOR = 0xF4DEBC9A78563412;
        public static readonly UInt64 RIVER_WATERWAY_DESCRIPTOR = 0xF5DEBC9A78563412;

        public static void Convert_Rivers(string filename_db, string filename_cache, ProgressInfo progress)
        {
            progress.Set_Info(true, "Reading rivers", 0);

            string db_timestamp_str = File.GetLastWriteTime(filename_db).ToString("yyyy-MM-dd HH':'mm':'ss");

            using (FileStream fs = File.OpenWrite(filename_cache))
            using (BinaryWriter writer = new BinaryWriter(fs, Encoding.UTF8, false))
            {
                writer.Write(@db_timestamp_str);

                DateTime last_progress = DateTime.Now;

                List<Int64> relations = new List<Int64>();

                List<WaterBody> rivers_inner = new List<WaterBody>();
                List<WaterBody> rivers_outer = new List<WaterBody>();
                List<WaterBody> rivers_single = new List<WaterBody>();
                List<Waterway> waterways = new List<Waterway>();

                int total_items;
                int base_items;

                using (SQLiteConnection sqlite_rivers = new SQLiteConnection("Data Source=" + filename_db))
                {
                    sqlite_rivers.Open();

                    using (SQLiteCommand cmd_rels = new SQLiteCommand("SELECT id FROM relations;", sqlite_rivers))
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

                        using (SQLiteCommand cmd_members = new SQLiteCommand("SELECT way_id,role FROM relation_members WHERE relation_id=$id;", sqlite_rivers))
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

                        WaterBody tmp_lake_inner = new WaterBody(relations[i]);
                        WaterBody tmp_lake_outer = new WaterBody(relations[i]);

                        tmp_lake_inner.Read_Nodes(members_inner, sqlite_rivers);
                        tmp_lake_outer.Read_Nodes(members_outer, sqlite_rivers);

                        rivers_inner.Add(tmp_lake_inner);
                        rivers_outer.Add(tmp_lake_outer);
                    }

                    relations.Clear();


                    progress.Set_Info(true, "Processing rivers (outer)", 0);
                    last_progress = DateTime.Now;

                    total_items = rivers_outer.Count + rivers_inner.Count;
                    base_items = 0;

                    if (total_items == 0)
                    {
                        total_items = 1;
                    }

                    for (int i = 0; i < rivers_outer.Count; i++)
                    {
                        if ((i % 50) == 0)
                        {
                            if ((DateTime.Now - last_progress).TotalMilliseconds > 200)
                            {
                                progress.Set_Info(((base_items + i) * 100) / total_items);

                                last_progress = DateTime.Now;
                            }
                        }

                        rivers_outer[i].Ways = Commons.Combine_Ways(rivers_outer[i].Ways);
                        rivers_outer[i].Make_Segments();
                    }

                    base_items = rivers_outer.Count;
                    progress.Set_Info(true, "Processing rivers (inner)", (base_items * 100) / total_items);

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

                        rivers_inner[i].Ways = Commons.Combine_Ways(rivers_inner[i].Ways);
                        rivers_inner[i].Make_Segments();
                    }

                    // ====== Single way rives bodies ======
                    progress.Set_Info(true, "Processing river bodies", 0);

                    List<Int64> riverdoby_ids = new List<Int64>();

                    using (SQLiteCommand cmd_members = new SQLiteCommand("SELECT way_id FROM way_tags WHERE k=$key AND v=$val", sqlite_rivers))
                    {
                        cmd_members.Parameters.AddWithValue("$key", "water");
                        cmd_members.Parameters.AddWithValue("$val", "river");

                        using (SQLiteDataReader rdr_members = cmd_members.ExecuteReader())
                        {
                            while (rdr_members.Read())
                            {
                                Int64 way_id = rdr_members.GetInt64(0);
                                riverdoby_ids.Add(way_id);
                            }

                            rdr_members.Close();
                        }
                    }

                    total_items = riverdoby_ids.Count;

                    if (total_items == 0)
                    {
                        total_items = 1;
                    }

                    for (int i = 0; i < riverdoby_ids.Count; i++)
                    {
                        if ((i % 50) == 0)
                        {
                            if ((DateTime.Now - last_progress).TotalMilliseconds > 200)
                            {
                                progress.Set_Info((i * 100) / total_items);
                                last_progress = DateTime.Now;
                            }
                        }

                        WaterBody tmp_wb = new WaterBody(riverdoby_ids[i]);
                        List<Int64> tmp_list = new List<Int64> { riverdoby_ids[i] };
                        tmp_wb.Read_Nodes(tmp_list, sqlite_rivers);
                        tmp_wb.Make_Segments();

                        rivers_single.Add(tmp_wb);
                    }

                    riverdoby_ids.Clear();

                    // ====== Waterways ======
                    progress.Set_Info(true, "Processing river waterways", 0);

                    List<Int64> waterway_ids = new List<Int64>();

                    using (SQLiteCommand cmd_members = new SQLiteCommand("SELECT way_id FROM way_tags WHERE k=$key AND v=$val", sqlite_rivers))
                    {
                        cmd_members.Parameters.AddWithValue("$key", "waterway");
                        cmd_members.Parameters.AddWithValue("$val", "river");

                        using (SQLiteDataReader rdr_members = cmd_members.ExecuteReader())
                        {
                            while (rdr_members.Read())
                            {
                                Int64 way_id = rdr_members.GetInt64(0);
                                waterway_ids.Add(way_id);
                            }

                            rdr_members.Close();
                        }
                    }

                    total_items = waterway_ids.Count;

                    if (total_items == 0)
                    {
                        total_items = 1;
                    }

                    for (int i = 0; i < waterway_ids.Count; i++)
                    {
                        if ((i % 50) == 0)
                        {
                            if ((DateTime.Now - last_progress).TotalMilliseconds > 200)
                            {
                                progress.Set_Info((i * 100) / total_items);
                                last_progress = DateTime.Now;
                            }
                        }

                        Way tmp_way = new Way();

                        tmp_way.Read_Nodes(waterway_ids[i], sqlite_rivers);

                        Waterway tmp_ww = new Waterway(waterway_ids[i], tmp_way);

                        waterways.Add(tmp_ww);
                    }


                    sqlite_rivers.Close();
                    sqlite_rivers.Dispose();
                }

                GC.Collect();

                progress.Set_Info(true, "Saving river cache", 0);

                System.Diagnostics.Debug.WriteLine(string.Format("Saving river cache: outer:{0} inner:{1} single:{2} waterways:{3}",
                    rivers_outer.Count, rivers_inner.Count, rivers_single.Count, waterways.Count));

                total_items = rivers_outer.Count + rivers_inner.Count + rivers_single.Count + waterways.Count;

                if (total_items == 0)
                {
                    total_items = 1;
                }
                
                base_items = 0;

                for (int i = 0; i < rivers_outer.Count; i++)
                {
                    if ((i % 50) == 0)
                    {
                        if ((DateTime.Now - last_progress).TotalMilliseconds > 200)
                        {
                            progress.Set_Info(((base_items + i) * 100) / total_items);
                            last_progress = DateTime.Now;
                        }
                    }

                    writer.Write(RIVER_OUTER_DESCRIPTOR);
                    rivers_outer[i].Write_Cache(writer);
                }

                base_items = rivers_outer.Count;

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

                    writer.Write(RIVER_INNER_DESCRIPTOR);
                    rivers_inner[i].Write_Cache(writer);
                }

                base_items += rivers_inner.Count;

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

                    writer.Write(RIVER_SINGLEWAY_DESCRIPTOR);
                    rivers_single[i].Write_Cache(writer);
                }

                base_items += rivers_single.Count;

                for (int i = 0; i < waterways.Count; i++)
                {
                    if ((i % 50) == 0)
                    {
                        if ((DateTime.Now - last_progress).TotalMilliseconds > 200)
                        {
                            progress.Set_Info(((base_items + i) * 100) / total_items);
                            last_progress = DateTime.Now;
                        }
                    }

                    writer.Write(RIVER_WATERWAY_DESCRIPTOR);
                    waterways[i].Write_Cache(writer);
                }
            }
        }
    }
}
