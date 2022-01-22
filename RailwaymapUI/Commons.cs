using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RailwaymapUI
{
    public enum MapItems { Landarea, Water, Borders, Railways, Cities, Sites, Scale, Margins };

    public static class Commons
    {
        private const double EARTHSIZE_MAJOR = 6378137.0;
        private const double EARTHSIZE_MINOR = 6356752.3142;

        private static readonly double RATIO = EARTHSIZE_MINOR / EARTHSIZE_MAJOR;
        private static readonly double ECCENT = Math.Sqrt(1.0 - (RATIO * RATIO));
        private static readonly double COM = ECCENT / 2.0;

        private static readonly double DEG2RAD = Math.PI / 180.0;
        private static readonly double RAD2Deg = 180.0 / Math.PI;

        static public double PROGRESS_INTERVAL = 200;

        static public double Merc_X(double lon)
        {
            return (EARTHSIZE_MAJOR * DEG2RAD * lon);
        }

        static public double Merc_Y(double lat)
        {
            if (lat > 89.5)
            {
                lat = 89.5;
            }

            if (lat < -89.5)
            {
                lat = -89.5;
            }

            double phi = DEG2RAD * lat;
            double sinphi = Math.Sin(phi);

            double con = ECCENT * sinphi;

            con = Math.Pow(((1.0 - con) / (1.0 + con)), COM);

            double ts = Math.Tan(((Math.PI / 2) - phi) / 2) / con;
            double y = 0.0 - (EARTHSIZE_MAJOR * Math.Log(ts));

            return y;
        }

        public static double Merc_X2Lon(double x)
        {
            return (x * RAD2Deg / EARTHSIZE_MAJOR);
        }

        public static double Merc_Y2Lat(double y)
        {
            double ts = Math.Exp(-y / EARTHSIZE_MAJOR);
            double phi = (Math.PI / 2.0) - 2 * Math.Atan(ts);
            double dphi = 1.0;
            int i = 0;

            while ((Math.Abs(dphi) > 0.000000001) && (i < 15))
            {
                double con = ECCENT * Math.Sin(phi);
                dphi = (Math.PI / 2.0) - 2 * Math.Atan(ts * Math.Pow((1.0 - con) / (1.0 + con), COM)) - phi;
                phi += dphi;
                i++;
            }

            return (phi * RAD2Deg);
        }

        public static int Merc2MapX(double mercx, BoundsXY bounds)
        {
            return (int)Math.Round(bounds.Scale * (mercx - bounds.X_min));
        }

        public static int Merc2MapY(double mercy, BoundsXY bounds)
        {
            //return (int)Math.Round(bounds.Scale * (mercy - bounds.Y_min));
            double maph = bounds.Scale * Math.Abs(bounds.Y_max - bounds.Y_min);
            double offset = bounds.Scale * (mercy - bounds.Y_min);

            return (int)Math.Round(maph - offset);
        }


        static public double Haversine_km(Coordinate pos1, Coordinate pos2)
        {
            const double R = 6371;

            double dLat = ToRadian(pos2.Latitude - pos1.Latitude);
            double dLon = ToRadian(pos2.Longitude - pos1.Longitude);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadian(pos1.Latitude)) * Math.Cos(ToRadian(pos2.Latitude)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
            double d = R * c;

            return d;
        }

        static private double ToRadian(double val)
        {
            return (DEG2RAD * val);
        }


        static public WaySet Generate_Wayset_Relations(SQLiteConnection conn)   // TODO: inner/outer
        {
            WaySet ws = new WaySet();


            return ws;
        }

        static public WaySet Generate_Wayset_Single(SQLiteConnection conn, Int64 way_id)
        {
            WaySet ws = new WaySet();

            WayCoordset tmpset = new WayCoordset();

            using (SQLiteCommand cmd_nodes = new SQLiteCommand("SELECT node_id FROM way_nodes WHERE way_id=$id;", conn))
            {
                cmd_nodes.Parameters.AddWithValue("$id", way_id);

                using (SQLiteDataReader rdr_nodes = cmd_nodes.ExecuteReader())
                {
                    while (rdr_nodes.Read())
                    {
                        long id_node = rdr_nodes.GetInt64(0);

                        using (SQLiteCommand cmd_coords = new SQLiteCommand("SELECT lat, lon FROM nodes WHERE id=$id;", conn))
                        {
                            cmd_coords.Parameters.AddWithValue("$id", id_node);

                            using (SQLiteDataReader rdr_coords = cmd_coords.ExecuteReader())
                            {
                                if (rdr_coords.Read())
                                {
                                    double lat = rdr_coords.GetDouble(0);
                                    double lon = rdr_coords.GetDouble(1);

                                    tmpset.AddItem(new Coordinate(lat, lon));
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine("Node " + id_node.ToString() + " not found in way id " + way_id.ToString());
                                }
                            }
                        }
                    }
                }
            }

            tmpset.Ready();

            if (tmpset.Coords.Length > 0)
            {
                ws.AddItem(tmpset);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Empty tmpset for way id " + way_id.ToString());
            }

            ws.Ready();

            return ws;
        }

        static public WaySet Generate_Wayset(SQLiteConnection conn)
        {
            WaySet ws = new WaySet();

            using (SQLiteCommand cmd_wayid = new SQLiteCommand("SELECT id FROM ways;", conn))
            using (SQLiteDataReader rdr_wayid = cmd_wayid.ExecuteReader())
            {
                SQLiteCommand cmd_nodes = new SQLiteCommand("SELECT node_id FROM way_nodes WHERE way_id=$id;", conn);

                while (rdr_wayid.Read())
                {
                    WayCoordset tmpset = new WayCoordset();

                    long id_way = rdr_wayid.GetInt64(0);

                    cmd_nodes.Parameters.AddWithValue("$id", id_way);

                    using (SQLiteDataReader rdr_nodes = cmd_nodes.ExecuteReader())
                    {
                        while (rdr_nodes.Read())
                        {
                            long id_node = rdr_nodes.GetInt64(0);

                            using (SQLiteCommand cmd_coords = new SQLiteCommand("SELECT lat, lon FROM nodes WHERE id=$id;", conn))
                            {
                                cmd_coords.Parameters.AddWithValue("$id", id_node);

                                using (SQLiteDataReader rdr_coords = cmd_coords.ExecuteReader())
                                {
                                    if (rdr_coords.Read())
                                    {
                                        double lat = rdr_coords.GetDouble(0);
                                        double lon = rdr_coords.GetDouble(1);

                                        tmpset.AddItem(new Coordinate(lat, lon));
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine("Node " + id_node.ToString() + " not found in way id " + id_way.ToString());
                                    }
                                }
                            }
                        }
                    }

                    tmpset.Ready();

                    if (tmpset.Coords.Length > 0)
                    {
                        ws.AddItem(tmpset);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Empty tmpset for way id " + id_way.ToString());
                    }
                }
            }

            ws.Ready();

            return ws;
        }


        static public WaySet Make_Wayset(SQLiteConnection conn, string tablename)
        {
            WaySet ws = new WaySet();

            int prev_setnum = -1;
            WayCoordset tmpset = new WayCoordset();

            SQLiteCommand cmd = new SQLiteCommand("SELECT setnum, latitude, longitude FROM " + tablename + ";", conn);

            SQLiteDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                int setnum = rdr.GetInt32(0);
                double lat = rdr.GetDouble(1);
                double lon = rdr.GetDouble(2);

                if (setnum != prev_setnum)
                {
                    tmpset.Ready();
                    ws.AddItem(tmpset);

                    tmpset = new WayCoordset();

                    prev_setnum = setnum;
                }

                tmpset.AddItem(new Coordinate(lat, lon));
            }

            tmpset.Ready();
            ws.AddItem(tmpset);

            ws.Ready();

            return ws;
        }

        static public bool Check_Cache_DB(string filename_db, string filename_cache)
        {
            bool result = false;

            if (File.Exists(filename_cache))
            {
                DateTime db_timestamp = File.GetLastWriteTime(filename_db);

                string timestamp_expect = db_timestamp.ToString("yyyy-MM-dd HH':'mm':'ss");

                System.Diagnostics.Debug.WriteLine("Check_Cache_DB: " + filename_db + " Expect timestamp: " + timestamp_expect);

                using (FileStream fs = File.OpenRead(filename_cache))
                using (BinaryReader reader = new BinaryReader(fs, Encoding.UTF8, false))
                {
                    string timestamp_cache = reader.ReadString();

                    if (timestamp_expect == timestamp_cache)
                    {
                        System.Diagnostics.Debug.WriteLine("Check_Cache_DB: " + filename_db + " Timestamp match ok");

                        result = true;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Check_Cache_DB: " + filename_db + " Timestamp mismatch: " + timestamp_expect + " -- " + timestamp_cache);
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Check_Cache_DB: " + filename_db + " File does not exists: " + filename_cache);
            }

            GC.Collect();

            return (result);
        }

        public static BitmapSource Bitmap2BitmapSource(System.Drawing.Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height,
                bitmap.HorizontalResolution, bitmap.VerticalResolution,
                PixelFormats.Pbgra32, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);

            return bitmapSource;
        }
    }
}