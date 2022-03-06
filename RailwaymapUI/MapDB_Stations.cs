using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RailwaymapUI
{
    public class MapDB_Stations : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public enum SortOrder { Name, Lat }

        public const string CONFIG_PREFIX = "Station.";


        public List<StationItem> Items { get; private set; }
        public List<StationItem> Items_Highlight { get; private set; }


        private bool _edit_stations;
        public bool EditStations { get { return _edit_stations; } set { _edit_stations = value; } }

        private bool _edit_sites;
        public bool EditSites { get { return _edit_sites; } set { _edit_sites = value; } }

        private bool _edit_yards;
        public bool EditYards { get { return _edit_yards; } set { _edit_yards = value; } }

        private bool _edit_lightrail;
        public bool EditLightrail { get { return _edit_lightrail; } set { _edit_lightrail = value; } }

        private bool _edit_halt;
        public bool EditHalt { get { return _edit_halt; } set { _edit_halt = value; } }

        public bool Separate_NonVisibleStations { get; set; }

        private bool _showallstations;
        public bool ShowAllStations { get { return _showallstations; } set { _showallstations = value; OnPropertyChanged("ShowAllStationsVisibility"); } }

        public Visibility ShowAllStationsVisibility { get { if (ShowAllStations) return Visibility.Visible; else return Visibility.Collapsed; } }

        private string _searchstationtext;
        public string SearchStationText
        {
            get { return _searchstationtext; }
            set
            {
                if (_searchstationtext != value)
                {
                    _searchstationtext = value;

                    UpdateSelection();
                }
            }
        }

        private int selection_x1;
        private int selection_y1;
        private int selection_x2;
        private int selection_y2;
        private int selection_ptX;
        private int selection_ptY;


        public MapDB_Stations()
        {
            Items = new List<StationItem>();
            Items_Highlight = new List<StationItem>();

            Separate_NonVisibleStations = true;
            ShowAllStations = false;

            EditStations = true;
            EditSites = false;
            EditYards = false;
            EditLightrail = false;
            EditHalt = true;

            selection_ptX = -1;
            selection_ptY = -1;
            selection_x1 = 0;
            selection_x2 = 0;
            selection_y1 = 0;
            selection_y2 = 0;
            SearchStationText = "";
        }

        public List<string> GetConfig()
        {
            List<string> result = new List<string>();

            foreach (StationItem st in Items)
            {
                StringBuilder str = new StringBuilder(CONFIG_PREFIX);

                str.Append("id"           + Commons.DELIM_EQ + st.id.ToString()      + Commons.DELIM_CONFITEMS);
                str.Append("display_name" + Commons.DELIM_EQ + st.display_name       + Commons.DELIM_CONFITEMS);
                str.Append("valign"       + Commons.DELIM_EQ + st.valign.ToString()  + Commons.DELIM_CONFITEMS);
                str.Append("halign"       + Commons.DELIM_EQ + st.halign.ToString()  + Commons.DELIM_CONFITEMS);
                str.Append("visible"      + Commons.DELIM_EQ + st.visible.ToString() + Commons.DELIM_CONFITEMS);
                str.Append("bold"         + Commons.DELIM_EQ + st.bold.ToString()    + Commons.DELIM_CONFITEMS);
                str.Append("outline"      + Commons.DELIM_EQ + st.outline.ToString() + Commons.DELIM_CONFITEMS);
                str.Append("offsetx"      + Commons.DELIM_EQ + st.offsetx.ToString() + Commons.DELIM_CONFITEMS);
                str.Append("offsety"      + Commons.DELIM_EQ + st.offsety.ToString() + Commons.DELIM_CONFITEMS);
                str.Append("dotsize"      + Commons.DELIM_EQ + st.dotsize.ToString() + Commons.DELIM_CONFITEMS);
                str.Append("english"      + Commons.DELIM_EQ + st.english.ToString() + Commons.DELIM_CONFITEMS);
                str.Append("rotation"     + Commons.DELIM_EQ + st.rotation.ToString());

                result.Add(str.ToString());
            }

            return result;
        }

        public void SetConfig(List<string> data)
        {
            foreach (string str in data)
            {
                string[] items = str.Substring(CONFIG_PREFIX.Length).Split(Commons.DELIM_CONFITEMS.ToCharArray());

                Int64 id = -1;
                string display_name = "";
                StationItem.Valign valign = StationItem.Valign.Top;
                StationItem.Halign halign = StationItem.Halign.Right;
                bool visible = true;
                bool bold = false;
                bool outline = false;
                bool english = false;
                int offsetx = 0;
                int offsety = 0;
                int dotsize = DrawSettings.Dotsize_Station_Default;
                int rotation = 0;

                foreach (string pair in items)
                {
                    string[] parts = pair.Split(Commons.DELIM_EQ.ToCharArray(), 2);

                    if (parts.Length == 2)
                    {
                        switch (parts[0])
                        {
                            case "id":
                                Int64.TryParse(parts[1], out id);
                                break;

                            case "display_name":
                                display_name = parts[1];
                                break;

                            case "valign":
                                Enum.TryParse(parts[1], out valign);
                                break;

                            case "halign":
                                Enum.TryParse(parts[1], out halign);
                                break;

                            case "visible":
                                bool.TryParse(parts[1], out visible);
                                break;

                            case "bold":
                                bool.TryParse(parts[1], out bold);
                                break;

                            case "outline":
                                bool.TryParse(parts[1], out outline);
                                break;

                            case "offsetx":
                                int.TryParse(parts[1], out offsetx);
                                break;

                            case "offsety":
                                int.TryParse(parts[1], out offsety);
                                break;

                            case "dotsize":
                                int.TryParse(parts[1], out dotsize);
                                break;

                            case "english":
                                bool.TryParse(parts[1], out english);
                                break;

                            case "rotation":
                                int.TryParse(parts[1], out rotation);
                                break;

                            default:
                                break;
                        }
                    }
                }

                if (id >= 0)
                {
                    foreach (StationItem st in Items)
                    {
                        if (st.id == id)
                        {
                            st.valign = valign;
                            st.halign = halign;
                            st.visible = visible;
                            st.bold = bold;
                            st.outline = outline;
                            st.offsetx = offsetx;
                            st.offsety = offsety;
                            st.dotsize = dotsize;
                            st.english = english;
                            st.rotation = rotation;
                            st.display_name = display_name; // setting english flag resets display name

                            st.Force_Refresh();

                            break;
                        }
                    }
                }
            }
        }

        public void Load_Stations(string db_filename_railways, string db_filename_lightrail)
        {
            Items.Clear();
            Items_Highlight.Clear();

            using (SQLiteConnection sqlite_railways = new SQLiteConnection("Data Source=" + db_filename_railways))
            {
                sqlite_railways.Open();

                Load_Items(sqlite_railways, false);

                sqlite_railways.Close();
            }

            Items.Sort((x, y) => x.name.CompareTo(y.name));

            using (SQLiteConnection sqlite_lightrail = new SQLiteConnection("Data Source=" + db_filename_lightrail))
            {
                sqlite_lightrail.Open();

                Load_Items(sqlite_lightrail, true);

                sqlite_lightrail.Close();
            }

            OnPropertyChanged("Items");
            OnPropertyChanged("Items_Highlight");

            GC.Collect();
        }

        private void Load_Items(SQLiteConnection conn, bool lightrail)
        {
            using (SQLiteCommand cmd = new SQLiteCommand("SELECT id, lat, lon, station FROM nodes WHERE station>0;", conn))
            {
                SQLiteDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    Int64 id = rdr.GetInt64(0);
                    double lat = rdr.GetDouble(1);
                    double lon = rdr.GetDouble(2);
                    int st_num = rdr.GetInt32(3);

                    string name = "";
                    string name_en = "";

                    StationItemType st_type = StationItemType.None;

                    if (lightrail)
                    {
                        st_type = StationItemType.Lightrail;
                    }
                    else
                    {
                        if (st_num == 1)
                        {
                            st_type = StationItemType.Station;
                        }
                        else if (st_num == 2)
                        {
                            st_type = StationItemType.Site;
                        }
                        else if (st_num == 3)
                        {
                            st_type = StationItemType.Yard;
                        }
                        else if (st_num == 4)
                        {
                            st_type = StationItemType.Halt;
                        }
                    }

                    StationItem st = new StationItem(st_type);

                    using (SQLiteCommand cmd2 = new SQLiteCommand("SELECT k, v FROM node_tags WHERE node_id=" + id.ToString() + ";", conn))
                    {
                        SQLiteDataReader rdr2 = cmd2.ExecuteReader();

                        while (rdr2.Read())
                        {
                            string k = rdr2.GetString(0);
                            string v = rdr2.GetString(1);

                            if (k == "name")
                            {
                                name = v;
                            }
                            else if (k == "name:en")
                            {
                                name_en = v;
                            }
                            else
                            {
                                // Do Nothing
                            }
                        }

                        rdr2.Close();
                    }

                    if (name != "")
                    {
                        st.name = name;
                        st.name_en = name_en;
                        st.display_name = name;
                        st.id = id;
                        st.Coord = new Coordinate(lat, lon);
                        st.from_building = false;

                        if (name_en != "")
                        {
                            st.has_english = true;
                        }
                        else
                        {
                            st.has_english = false;
                        }

                        st.visible = true;

                        Items.Add(st);
                    }
                }

                rdr.Close();
            }


            // Station building ways
            using (SQLiteCommand cmd = new SQLiteCommand("SELECT way_id FROM way_tags WHERE k='railway' AND v='station';", conn))
            using (SQLiteDataReader rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    Int64 way_id = rdr.GetInt64(0);
                    string name = "";
                    List<Coordinate> st_coords = new List<Coordinate>();

                    using (SQLiteCommand cmd_name = new SQLiteCommand("SELECT v FROM way_tags WHERE way_id=" + way_id.ToString() + " AND k='name';", conn))
                    using (SQLiteDataReader rdr_name = cmd_name.ExecuteReader())
                    {
                        while (rdr_name.Read())
                        {
                            name = rdr_name.GetString(0);
                            break;
                        }

                        rdr_name.Close();
                    }

                    using (SQLiteCommand cmd_nodes = new SQLiteCommand("SELECT node_id FROM way_nodes WHERE way_id=" + way_id.ToString() + ";", conn))
                    using (SQLiteDataReader rdr_nodes = cmd_nodes.ExecuteReader())
                    {
                        while (rdr_nodes.Read())
                        {
                            Int64 node_id = rdr_nodes.GetInt64(0);

                            using (SQLiteCommand cmd_coords = new SQLiteCommand("SELECT lat, lon FROM nodes WHERE id=" + node_id.ToString() + ";", conn))
                            using (SQLiteDataReader rdr_coords = cmd_coords.ExecuteReader())
                            {
                                while (rdr_coords.Read())
                                {
                                    double lat = rdr_coords.GetDouble(0);
                                    double lon = rdr_coords.GetDouble(1);

                                    st_coords.Add(new Coordinate(lat, lon));
                                }
                            }
                        }

                        rdr_nodes.Close();
                    }

                    if ((st_coords.Count > 0) && (name != ""))
                    {
                        double avg_lat = 0;
                        double avg_lon = 0;

                        foreach (Coordinate c in st_coords)
                        {
                            avg_lat += c.Latitude;
                            avg_lon += c.Longitude;
                        }

                        avg_lat /= st_coords.Count;
                        avg_lon /= st_coords.Count;

                        StationItem st = new StationItem(StationItemType.Station);

                        if (lightrail)
                        {
                            st.Type = StationItemType.Lightrail;
                        }

                        st.name = name;
                        st.name_en = name;
                        st.display_name = name;
                        st.id = way_id;
                        st.Coord = new Coordinate(avg_lat, avg_lon);
                        st.has_english = false;
                        st.visible = true;
                        st.from_building = true;

                        Items.Add(st);
                    }
                }

                rdr.Close();
            }
        }


        private void UpdateSelection()
        {
            Items_Highlight = new List<StationItem>();

            if ((selection_ptX >= 0) && (selection_ptY >= 0))
            {
                foreach (StationItem st in Items)
                {
                    bool set = false;

                    if ((st.coordX >= selection_x1) && (st.coordX <= selection_x2))
                    {
                        if ((st.coordY >= selection_y1) && (st.coordY <= selection_y2))
                        {
                            bool set_this = true;

                            if ((st.Type == StationItemType.Station) && !EditStations)
                            {
                                set_this = false;
                            }
                            else if ((st.Type == StationItemType.Site) && !EditSites)
                            {
                                set_this = false;
                            }
                            else if ((st.Type == StationItemType.Yard) && !EditYards)
                            {
                                set_this = false;
                            }
                            else if ((st.Type == StationItemType.Lightrail) && !EditLightrail)
                            {
                                set_this = false;
                            }
                            else if ((st.Type == StationItemType.Halt) && !EditHalt)
                            {
                                set_this = false;
                            }

                            if (set_this)
                            {
                                if (SearchStationText != "")
                                {
                                    if (!st.name.Contains(SearchStationText, StringComparison.OrdinalIgnoreCase) && !st.name_en.Contains(SearchStationText, StringComparison.OrdinalIgnoreCase))
                                    {
                                        set_this = false;
                                    }
                                }
                            }

                            if (set_this)
                            {
                                Items_Highlight.Add(st);

                                set = true;
                            }
                        }
                    }

                    st.Highlighted = set;
                }
            }
            else
            {
                foreach (StationItem st in Items)
                {
                    st.Highlighted = false;
                }
            }

            Items_Highlight.Sort(Compare_Stations_Latitude);

            OnPropertyChanged("Items_Highlight");
        }

        public void Refresh_Selection(ZoomBorder zoomer)
        {
            if (zoomer != null)
            {
                selection_ptX = (int)zoomer.Selection_Point.X;
                selection_ptY = (int)zoomer.Selection_Point.Y;

                selection_x1 = selection_ptX - (zoomer.Selection_Size / 2);
                selection_y1 = selection_ptY - (zoomer.Selection_Size / 2);
                selection_x2 = selection_x1 + zoomer.Selection_Size;
                selection_y2 = selection_y1 + zoomer.Selection_Size;
            }
            else
            {
                selection_ptX = -1;
                selection_ptY = -1;

                selection_x1 = 0;
                selection_y1 = 0;
                selection_x2 = 0;
                selection_y2 = 0;
            }

            UpdateSelection();
        }



        public void Set_Station_Valign(Int64 id, StationItem.Valign valign)
        {
            foreach (StationItem st in Items)
            {
                if (st.id == id)
                {
                    st.Set_Valign(valign);

                    break;
                }
            }
        }

        public void Set_Station_Halign(Int64 id, StationItem.Halign halign)
        {
            foreach (StationItem st in Items)
            {
                if (st.id == id)
                {
                    st.Set_Halign(halign);

                    break;
                }
            }
        }

        public void Adjust_Station_OffsetX(Int64 id, int adjust)
        {
            foreach (StationItem st in Items)
            {
                if (st.id == id)
                {
                    st.offsetx += adjust;
                    st.Force_Refresh();

                    break;
                }
            }
        }

        public void Adjust_Station_OffsetY(Int64 id, int adjust)
        {
            foreach (StationItem st in Items)
            {
                if (st.id == id)
                {
                    st.offsety += adjust;
                    st.Force_Refresh();

                    break;
                }
            }
        }

        public void Flip_Station_Bold(Int64 id)
        {
            foreach (StationItem st in Items)
            {
                if (st.id == id)
                {
                    st.bold = !st.bold;
                    st.Force_Refresh();

                    break;
                }
            }
        }

        public void Flip_Station_Outline(Int64 id)
        {
            foreach (StationItem st in Items)
            {
                if (st.id == id)
                {
                    st.outline = !st.outline;
                    st.Force_Refresh();

                    break;
                }
            }
        }


        public void Flip_Station_EN(Int64 id)
        {
            foreach (StationItem st in Items)
            {
                if (st.id == id)
                {
                    st.english = !st.english;
                    st.Force_Refresh();

                    break;
                }
            }
        }

        public void Flip_Station_Rotation(Int64 id)
        {
            foreach (StationItem st in Items)
            {
                if (st.id == id)
                {
                    st.rotation++;

                    if (st.rotation > 2)
                    {
                        st.rotation = 0;
                    }

                    st.Force_Refresh();

                    break;
                }
            }
        }


        public void Set_Highlighted_Deselect()
        {
            foreach (StationItem st in Items_Highlight)
            {
                st.visible = false;
                st.Force_Refresh();
            }
        }

        public void Set_Highlighted_EN()
        {
            foreach (StationItem st in Items_Highlight)
            {
                if (st.has_english)
                {
                    st.english = true;
                    st.Force_Refresh();
                }
            }
        }

        public void Set_Station_Dot(Int64 id, int dotsize)
        {
            foreach (StationItem st in Items)
            {
                if (st.id == id)
                {
                    st.dotsize = dotsize;
                    st.Force_Refresh();

                    break;
                }
            }
        }

        public void Set_All_Deselected(StationItemType hide_type)
        {
            foreach (StationItem st in Items)
            {
                if (st.Type == hide_type)
                {
                    if (st.visible)
                    {
                        st.visible = false;
                        st.Force_Refresh();
                    }
                }
            }
        }

        public void Set_All_FromBuilding_Deselected()
        {
            foreach (StationItem st in Items)
            {
                if (st.from_building)
                {
                    if (st.visible)
                    {
                        st.visible = false;
                        st.Force_Refresh();
                    }
                }
            }
        }


        public void Sort_Stations(SortOrder order)
        {
            if (order == SortOrder.Name)
            {
                Items.Sort(Compare_Stations_Name);
            }
            else
            {
                Items.Sort(Compare_Stations_Latitude);
            }

            Items = new List<StationItem>(Items);

            OnPropertyChanged("Items");
        }

        private int Compare_Stations_Name(StationItem item1, StationItem item2)
        {
            int result;

            if (Separate_NonVisibleStations)
            {
                if (item1.visible == item2.visible)
                {
                    // Same visibility -- compare normally
                    result = item1.name.CompareTo(item2.name);
                }
                else if (item1.visible == true)
                {
                    result = -1;
                }
                else
                {
                    result = 1;
                }
            }
            else
            {
                result = item1.name.CompareTo(item2.name);
            }

            return result;
        }

        private int Compare_Stations_Latitude(StationItem item1, StationItem item2)
        {
            int result;

            if (Separate_NonVisibleStations)
            {
                if (item1.visible == item2.visible)
                {
                    // Same visibility -- compare normally
                    result = item2.Coord.Latitude.CompareTo(item1.Coord.Latitude);
                }
                else if (item1.visible == true)
                {
                    result = -1;
                }
                else
                {
                    result = 1;
                }
            }
            else
            {
                result = item2.Coord.Latitude.CompareTo(item1.Coord.Latitude);
            }

            return result;
        }

    }
}
