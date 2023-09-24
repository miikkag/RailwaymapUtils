using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class Way
    {
        public List<Coordinate> Coordinates;
        public bool error_unconnected;

        public enum WayMatch { None,
            StartStart, // First nodes of both items match
            EndEnd,     // Last nodes of both item match
            StartEnd,   // First node of this item matches the last node of other item
            EndStart    // Last node of this item matches the first node of other item
        };

        public Way()
        {
            Coordinates = new List<Coordinate>();
            error_unconnected = false;
        }

        public void Add_Node(double lat, double lon)
        {
            Coordinates.Add(new Coordinate(lat, lon));
        }

        public void Add_Node(long id, double lat, double lon)
        {
            Coordinates.Add(new Coordinate(id, lat, lon));
        }

        public void Read_Nodes(Int64 way_id, SQLiteConnection conn)
        {
            using (SQLiteCommand cmd_nodes = new SQLiteCommand("SELECT node_id FROM way_nodes WHERE way_id=$id;", conn))
            {
                cmd_nodes.Parameters.AddWithValue("$id", way_id);

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

                                    Add_Node(lat, lon);
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

        }

        public WayMatch Compare_EndsID(Way other)
        {
            if ((Coordinates.Count > 0) && (other.Coordinates.Count > 0))
            {
                if (Coordinates[0].CompareID(other.Coordinates.Last()))
                {
                    return WayMatch.StartEnd;
                }
                else if (Coordinates.Last().CompareID(other.Coordinates[0]))
                {
                    return WayMatch.EndStart;
                }
                else if (Coordinates[0].CompareID(other.Coordinates[0]))
                {
                    return WayMatch.StartStart;
                }
                else if (Coordinates.Last().CompareID(other.Coordinates.Last()))
                {
                    return WayMatch.EndEnd;
                }
                else
                {
                    return WayMatch.None;
                }
            }
            else
            {
                return WayMatch.None;
            }
        }

        public void Filter_Nodes(double threshold_lat, double threshold_lon)
        {
            if (Coordinates.Count < 3)
            {
                return;
            }

            List<Coordinate> new_coords = new List<Coordinate>();

            int prev_index = 0;

            new_coords.Add(Coordinates[0]);

            for (int i = 1; i < Coordinates.Count - 1; i++)
            {
                if ((Math.Abs(Coordinates[i].Latitude - Coordinates[prev_index].Latitude) >= threshold_lat) &&
                    (Math.Abs(Coordinates[i].Longitude - Coordinates[prev_index].Longitude) >= threshold_lon))
                {
                    new_coords.Add(Coordinates[i]);

                    prev_index = i;
                }
            }

            new_coords.Add(Coordinates.Last());

            Coordinates = new_coords;
        }
    }
}
