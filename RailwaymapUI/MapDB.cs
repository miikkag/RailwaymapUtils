using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Data.SQLite;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Drawing;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Drawing.Imaging;
using System.Threading;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace RailwaymapUI
{
    public class MapDB : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public enum SortOrder { Name, Lat }

        private const string CONFIG_FILENAME = "area.config";
        private const string BOUNDS_FILENAME = "bbox.txt";

        private const string DB_FILENAME_RAILWAYS = "railway.db";
        private const string DB_FILENAME_LIGHTRAIL = "lightrail.db";
        private const string DB_FILENAME_BORDER = "border.db";
        private const string DB_FILENAME_COASTLINE = "coastline.db";
        private const string DB_FILENAME_COASTLINE_CACHE = "coastline_cache.bin";
        private const string DB_FILENAME_LAKES = "lakes.db";
        private const string DB_FILENAME_LAKES_CACHE = "lakes_cache.bin";

        public string Area { get; private set; }
        private string AreaPath;
        private string AreaConfigFilename;
        private string BoundsFilename;

        private object drawlock;

        public string OutputFileName { get; set; }

        public int OutputSizeWidth { get; set; }
        public int OutputSizeHeight { get; set; }

        private int selection_x1;
        private int selection_y1;
        private int selection_x2;
        private int selection_y2;
        private int selection_ptX;
        private int selection_ptY;

        public MapImage_Background Image_Background { get; private set; }
        public MapImage_Landarea Image_Landarea { get; private set; }
        public MapImage_Lakes Image_Water { get; private set; }
        public MapImage_Borders Image_Borders { get; private set; }
        public MapImage_Railways Image_Railways { get; private set; }
        public MapImage_Cities Image_Cities { get; private set; }
        public MapImage_Scale Image_Scale { get; private set; }
        public MapImage_Selection Image_Selection { get; private set; }

        public ProgressInfo Progress { get; private set; }

        public DrawSettings Set { get; private set; }

        public List<StationItem> Stations { get; private set; }
        public List<StationItem> Stations_Highlight { get; private set; }

        private bool _edit_stations;
        public bool EditStations { get { return _edit_stations; } set { _edit_stations = value; } }

        private bool _edit_sites;
        public bool EditSites { get { return _edit_sites; } set { _edit_sites = value; } }

        private bool _edit_yeards;
        public bool EditYards { get { return _edit_yeards; } set { _edit_yeards = value; } }

        private bool _edit_lightrail;
        public bool EditLightrail { get { return _edit_lightrail; } set { _edit_lightrail = value; } }

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

        private Bounds bounds;
        private BoundsXY bxy;

        private RailwayLegend legend;

        private struct DrawItems
        {
            public bool use_cache;

            public bool landarea;
            public bool water;
            public bool borders;
            public bool railways;
            public bool cities;
            public bool scale;
            public bool margins;
        }

        private DrawItems draw_items;

        public MapDB()
        {
            OutputSizeWidth = 1000;
            OutputSizeHeight = 1000;

            Progress = new ProgressInfo();

            Image_Background = new MapImage_Background();
            Image_Landarea = new MapImage_Landarea();
            Image_Water = new MapImage_Lakes();
            Image_Borders = new MapImage_Borders();
            Image_Railways = new MapImage_Railways();
            Image_Cities = new MapImage_Cities();
            Image_Scale = new MapImage_Scale();
            Image_Selection = new MapImage_Selection();

            Image_Selection.Enabled = false;

            Set = new DrawSettings();

            bounds = new Bounds();
            bxy = null;

            legend = new RailwayLegend();

            draw_items = new DrawItems();

            Stations = new List<StationItem>();
            Stations_Highlight = new List<StationItem>();

            Separate_NonVisibleStations = true;
            ShowAllStations = false;

            EditStations = true;
            EditSites = true;
            EditYards = true;
            EditLightrail = true;

            selection_ptX = -1;
            selection_ptY = -1;
            selection_x1 = 0;
            selection_x2 = 0;
            selection_y1 = 0;
            selection_y2 = 0;
            SearchStationText = "";

            drawlock = new object();
        }

        public void Reset_Size()
        {
            Image_Background.Set_Size(OutputSizeWidth, OutputSizeHeight);
            Image_Landarea.Set_Size(OutputSizeWidth, OutputSizeHeight);
            Image_Water.Set_Size(OutputSizeWidth, OutputSizeHeight);
            Image_Borders.Set_Size(OutputSizeWidth, OutputSizeHeight);
            Image_Railways.Set_Size(OutputSizeWidth, OutputSizeHeight);
            Image_Cities.Set_Size(OutputSizeWidth, OutputSizeHeight);
            Image_Scale.Set_Size(OutputSizeWidth, OutputSizeHeight);
            Image_Selection.Set_Size(OutputSizeWidth, OutputSizeHeight);

            Image_Background.Fill();

            OnPropertyChanged("Image_Background");

            draw_items.use_cache = true;

            draw_items.landarea = true;
            draw_items.water = true;
            draw_items.borders = true;
            draw_items.railways = true;
            draw_items.cities = true;
            draw_items.scale = true;
            draw_items.margins = true;

            Thread thr = new Thread(DrawAllThread);

            thr.Start();
        }

        public void Reset_Single(MapItems item)
        {
            draw_items.use_cache = false;

            draw_items.landarea = (item == MapItems.Landarea);
            draw_items.borders = (item == MapItems.Borders);
            draw_items.water = (item == MapItems.Water);
            draw_items.railways = (item == MapItems.Railways);
            draw_items.cities = (item == MapItems.Cities);
            draw_items.scale = (item == MapItems.Scale);
            draw_items.margins = (item == MapItems.Margins);

            Thread thr = new Thread(DrawAllThread);

            thr.Start();
        }

        public void AutoSize(bool set_width, bool set_height)
        {
            if (bxy != null)
            {
                double ratio = Math.Abs(bxy.X_max - bxy.X_min) / Math.Abs(bxy.Y_max - bxy.Y_min);

                if (set_width)
                {
                    OutputSizeWidth = (int)Math.Round((double)OutputSizeHeight * ratio);
                }

                if (set_height)
                {
                    OutputSizeHeight = (int)Math.Round((double)OutputSizeWidth / ratio);
                }

                Load_Bounds();

                OnPropertyChanged("OutputSizeWidth");
                OnPropertyChanged("OutputSizeHeight");
            }
        }


        public void Set_Station_Valign(Int64 id, StationItem.Valign valign)
        {
            foreach (StationItem st in Stations)
            {
                if (st.id == id)
                {
                    st.Set_Valign(valign);

                    break;
                }
            }

            if (Set.AutoRedraw_Cities)
            {
                Reset_Single(MapItems.Cities);
            }
        }

        public void Set_Station_Halign(Int64 id, StationItem.Halign halign)
        {
            foreach (StationItem st in Stations)
            {
                if (st.id == id)
                {
                    st.Set_Halign(halign);

                    break;
                }
            }

            if (Set.AutoRedraw_Cities)
            {
                Reset_Single(MapItems.Cities);
            }
        }

        public void Adjust_Station_OffsetX(Int64 id, int adjust)
        {
            foreach (StationItem st in Stations)
            {
                if (st.id == id)
                {
                    st.offsetx += adjust;
                    st.Force_Refresh();

                    break;
                }
            }

            if (Set.AutoRedraw_Cities)
            {
                Reset_Single(MapItems.Cities);
            }
        }

        public void Adjust_Station_OffsetY(Int64 id, int adjust)
        {
            foreach (StationItem st in Stations)
            {
                if (st.id == id)
                {
                    st.offsety += adjust;
                    st.Force_Refresh();

                    break;
                }
            }

            if (Set.AutoRedraw_Cities)
            {
                Reset_Single(MapItems.Cities);
            }
        }

        public void Change_Bold(Int64 id)
        {
            foreach (StationItem st in Stations)
            {
                if (st.id == id)
                {
                    st.bold = !st.bold;
                    st.Force_Refresh();

                    break;
                }
            }

            if (Set.AutoRedraw_Cities)
            {
                Reset_Single(MapItems.Cities);
            }
        }

        public void Change_Outline(Int64 id)
        {
            foreach (StationItem st in Stations)
            {
                if (st.id == id)
                {
                    st.outline = !st.outline;
                    st.Force_Refresh();

                    break;
                }
            }

            if (Set.AutoRedraw_Cities)
            {
                Reset_Single(MapItems.Cities);
            }
        }


        public void Change_EN(Int64 id)
        {
            foreach (StationItem st in Stations)
            {
                if (st.id == id)
                {
                    st.english = !st.english;
                    st.Force_Refresh();

                    break;
                }
            }

            if (Set.AutoRedraw_Cities)
            {
                Reset_Single(MapItems.Cities);
            }
        }

        public void HideAll(StationItemType hide_type)
        {
            foreach (StationItem st in Stations)
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

            if (Set.AutoRedraw_Cities)
            {
                Reset_Single(MapItems.Cities);
            }
        }


        public void Set_Station_Dot(Int64 id, int dotsize)
        {
            foreach (StationItem st in Stations)
            {
                if (st.id == id)
                {
                    st.dotsize = dotsize;
                    st.Force_Refresh();

                    break;
                }
            }

            if (Set.AutoRedraw_Cities)
            {
                Reset_Single(MapItems.Cities);
            }
        }

        private void UpdateSelection()
        {
            Stations_Highlight = new List<StationItem>();

            if ((selection_ptX >= 0) && (selection_ptY >= 0))
            {
                foreach (StationItem st in Stations)
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
                                Stations_Highlight.Add(st);

                                set = true;
                            }
                        }
                    }

                    st.Highlighted = set;
                }
            }
            else
            {
                foreach (StationItem st in Stations)
                {
                    st.Highlighted = false;
                }
            }

            Stations_Highlight.Sort(Compare_Stations_Latitude);

            OnPropertyChanged("Image_Selection");
            OnPropertyChanged("Stations_Highlight");
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

        public void Sort_Stations(SortOrder order)
        {
            if (order == SortOrder.Name)
            {
                Stations.Sort(Compare_Stations_Name);
            }
            else
            {
                Stations.Sort(Compare_Stations_Latitude);
            }

            Stations = new List<StationItem>(Stations);

            OnPropertyChanged("Stations");
        }

        public void Deselect_Highlighted()
        {
            foreach (StationItem st in Stations_Highlight)
            {
                st.visible = false;
                st.Force_Refresh();
            }

            if (Set.AutoRedraw_Cities)
            {
                Reset_Single(MapItems.Cities);
            }
        }

        public void EN_Highlighted()
        {
            foreach (StationItem st in Stations_Highlight)
            {
                if (st.has_english)
                {
                    st.english = true;
                    st.Force_Refresh();
                }
            }

            if (Set.AutoRedraw_Cities)
            {
                Reset_Single(MapItems.Cities);
            }

        }


        public void Load_Area(string area_path)
        {
            if (area_path != "")
            {
                if (Directory.Exists(area_path))
                {
                    Area = Path.GetFileName(area_path);
                    AreaPath = area_path;
                    AreaConfigFilename = Path.Combine(area_path, CONFIG_FILENAME);
                    BoundsFilename = Path.Combine(area_path, BOUNDS_FILENAME);

                    OutputFileName = Area + ".png";

                    try
                    {
                        using (SQLiteConnection sqlite_railways = new SQLiteConnection("Data Source=" + Path.Combine(area_path, DB_FILENAME_RAILWAYS)))
                        using (SQLiteConnection sqlite_lightrail = new SQLiteConnection("Data Source=" + Path.Combine(area_path, DB_FILENAME_LIGHTRAIL)))
                        {
                            sqlite_railways.Open();
                            sqlite_lightrail.Open();

                            Stations.Clear();
                            Stations = new List<StationItem>();

                            Load_Stations(sqlite_railways, false);
                            Load_Stations(sqlite_lightrail, true);

                            Stations.Sort((x, y) => x.name.CompareTo(y.name));

                            OnPropertyChanged("Stations");

                            sqlite_lightrail.Close();
                            sqlite_railways.Close();

                            GC.Collect();
                        }

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error opening railway/lightrail database" + Environment.NewLine + Environment.NewLine + ex.Message,
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    Load_Config();
                    Load_Bounds();
                }
                else
                {
                    MessageBox.Show(area_path + Environment.NewLine + "is not a valid directory", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                OnPropertyChanged("Area");
                OnPropertyChanged("OutputFileName");
            }
        }

        public void Export_Image(bool clipboard)
        {
            Bitmap bmp = new Bitmap(OutputSizeWidth, OutputSizeHeight);
            Graphics gr = Graphics.FromImage(bmp);

            gr.Clear(Set.Color_Land);

            if (Image_Landarea.Enabled)
            {
                gr.DrawImage(Image_Landarea.GetBitmap(), 0, 0);
            }

            if (Image_Water.Enabled)
            {
                gr.DrawImage(Image_Water.GetBitmap(), 0, 0);
            }

            if (Image_Borders.Enabled)
            {
                gr.DrawImage(Image_Borders.GetBitmap(), 0, 0);
            }

            if (Image_Railways.Enabled)
            {
                gr.DrawImage(Image_Railways.GetBitmap(), 0, 0);
            }

            if (Image_Cities.Enabled)
            {
                gr.DrawImage(Image_Cities.GetBitmap(), 0, 0);
            }

            gr.DrawImage(Image_Scale.GetBitmap(), 0, 0);

            if (clipboard)
            {
                // Export to clipboard
                Clipboard.SetImage(Commons.Bitmap2BitmapSource(bmp));
            }
            else
            {
                // Export to file
                string parent = Directory.GetParent(AreaPath).FullName;
                string output_pathname = Path.Combine(parent, OutputFileName);

                bmp.Save(output_pathname, ImageFormat.Png);
            }

            bmp.Dispose();
            gr.Dispose();
        }

        private const string DB_CONFIG_PREFIX = "DB.";
        private const string STATION_CONFIG_PREFIX = "Station.";

        public void Save_Config()
        {
            if (AreaConfigFilename != "")
            {
                List<string> all = new List<string>();

                List<string> items_set = Set.Save_Config();

                all.AddRange(items_set);

                all.Add(DB_CONFIG_PREFIX + "OutputWidth=" + OutputSizeWidth.ToString());
                all.Add(DB_CONFIG_PREFIX + "OutputHeight=" + OutputSizeHeight.ToString());

                all.Add(DB_CONFIG_PREFIX + "Separate_NonVisibleStations=" + Separate_NonVisibleStations.ToString());

                foreach (StationItem st in Stations)
                {
                    StringBuilder str = new StringBuilder(STATION_CONFIG_PREFIX);
                    str.Append("id=" + st.id.ToString() + ";");
                    str.Append("display_name=" + st.display_name + ";");
                    str.Append("valign=" + st.valign.ToString() + ";");
                    str.Append("halign=" + st.halign.ToString() + ";");
                    str.Append("visible=" + st.visible.ToString() + ";");
                    str.Append("bold=" + st.bold.ToString() + ";");
                    str.Append("outline=" + st.outline.ToString() + ";");
                    str.Append("offsetx=" + st.offsetx.ToString() + ";");
                    str.Append("offsety=" + st.offsety.ToString() + ";");
                    str.Append("dotsize=" + st.dotsize.ToString() + ";");
                    str.Append("english=" + st.english.ToString());

                    all.Add(str.ToString());
                }

                File.WriteAllLines(AreaConfigFilename, all);
            }
        }

        public void Load_Bounds()
        {
            if (BoundsFilename != "")
            {
                string[] lines = File.ReadAllLines(BoundsFilename);

                if (lines.Length > 0)
                {
                    string line = lines[0].Trim();

                    if (line.StartsWith("(") && line.EndsWith(")"))
                    {
                        line = line.Substring(1, line.Length - 2);

                        string[] parts = line.Split(',');

                        if (parts.Length == 4)
                        {
                            double.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double lat1);
                            double.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double lon1);
                            double.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double lat2);
                            double.TryParse(parts[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double lon2);

                            bounds = new Bounds(Math.Min(lat1, lat2), Math.Max(lat1, lat2), Math.Min(lon1, lon2), Math.Max(lon1, lon2));

                            double y_max = Commons.Merc_Y(bounds.Lat_max);
                            double y_min = Commons.Merc_Y(bounds.Lat_min);
                            double x_max = Commons.Merc_X(bounds.Lon_max);
                            double x_min = Commons.Merc_X(bounds.Lon_min);

                            double delta_x = x_max - x_min;
                            double delta_y = y_max - y_min;

                            double scalex = (double)OutputSizeWidth / delta_x;
                            double scaley = (double)OutputSizeHeight / delta_y;

                            double scale = Math.Min(scalex, scaley);

                            bxy = new BoundsXY(x_max, x_min, y_max, y_min, scale);
                        }
                        else
                        {
                            MessageBox.Show("Invalid bbox file (cannot split parts).", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Invalid bbox file (invalid line format).", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Invalid bbox file (no data).", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        public void Load_Config()
        {
            char[] delim = new char[1] { '=' };
            char[] delim_st = new char[1] { ';' };

            if (AreaConfigFilename != "")
            {
                if (!File.Exists(AreaConfigFilename))
                {
                    return;
                }

                string[] lines = File.ReadAllLines(AreaConfigFilename);

                Set.Read_Config(lines);

                foreach (string str in lines)
                {
                    if (str.StartsWith(DB_CONFIG_PREFIX))
                    {
                        string[] items = str.Substring(DB_CONFIG_PREFIX.Length).Split(delim, 2);

                        int.TryParse(items[1], out int val);

                        switch (items[0])
                        {
                            case "OutputWidth":
                                OutputSizeWidth = val;
                                break;

                            case "OutputHeight":
                                OutputSizeHeight = val;
                                break;

                            case "Separate_NonVisibleStations":
                                bool.TryParse(items[1], out bool tmpval);
                                Separate_NonVisibleStations = tmpval;
                                break;

                            default:
                                break;
                        }
                    }
                    else if (str.StartsWith(STATION_CONFIG_PREFIX))
                    {
                        string[] items = str.Substring(STATION_CONFIG_PREFIX.Length).Split(delim_st);

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

                        foreach (string pair in items)
                        {
                            string[] parts = pair.Split(delim, 2);

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

                                    default:
                                        break;
                                }
                            }
                        }

                        //Console.WriteLine("Config line " + str + "  id " + id.ToString());

                        if (id >= 0)
                        {
                            foreach (StationItem st in Stations)
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
                                    st.display_name = display_name; // setting english flag resets display name

                                    st.Force_Refresh();

                                    break;
                                }
                            }
                        }
                    }
                }
            }

            OnPropertyChanged(string.Empty);
        }


        private void DrawAllThread()
        {
            lock (drawlock)
            {
                string db_file = "Init";

                try
                {
                    if (draw_items.landarea)
                    {
                        db_file = Path.Combine(AreaPath, DB_FILENAME_COASTLINE);

                        string db_cache = Path.Combine(AreaPath, DB_FILENAME_COASTLINE_CACHE);

                        if (!Commons.Check_Cache_DB(db_file, db_cache))
                        {
                            LandareaCache.Convert_Coastline(db_file, db_cache, Progress);
                        }

                        string img_cache = Commons.Cache_ImageName(AreaPath, "landarea");

                        bool need_draw = true;

                        if (draw_items.use_cache && Commons.Check_Cache_Image(db_file, img_cache))
                        {
                            need_draw = !Image_Landarea.DrawFromCache(img_cache);
                        }

                        if(need_draw)
                        {
                            Image_Landarea.Draw(db_cache, bxy, Progress, Set);
                            Image_Landarea.Save_ImageCache(img_cache, db_file);
                        }

                        OnPropertyChanged("Image_Landarea");
                    }

                    if (draw_items.borders)
                    {
                        db_file = Path.Combine(AreaPath, DB_FILENAME_BORDER);

                        using (SQLiteConnection sqlite_border = new SQLiteConnection("Data Source=" + db_file))
                        {
                            sqlite_border.Open();

                            Image_Borders.Draw(sqlite_border, bxy, Progress, Set);

                            sqlite_border.Close();
                        }

                        OnPropertyChanged("Image_Borders");
                    }

                    if (draw_items.water)
                    {
                        db_file = Path.Combine(AreaPath, DB_FILENAME_LAKES);

                        string db_cache = Path.Combine(AreaPath, DB_FILENAME_LAKES_CACHE);

                        if (!Commons.Check_Cache_DB(db_file, db_cache))
                        {
                            LakeCache.Convert_Lakes(db_file, db_cache, Progress);
                        }

                        string img_cache = Commons.Cache_ImageName(AreaPath, "lakes");

                        bool need_draw = true;

                        if (draw_items.use_cache && Commons.Check_Cache_Image(db_file, img_cache))
                        {
                            need_draw = !Image_Water.DrawFromCache(img_cache);
                        }

                        if (need_draw)
                        {
                            Image_Water.Draw(db_cache, bxy, Progress, Set);
                            Image_Water.Save_ImageCache(img_cache, db_file);
                        }

                        OnPropertyChanged("Image_Water");
                    }

                    if (draw_items.railways)
                    {
                        legend.Clear();

                        db_file = "railway";

                        string img_cache = Commons.Cache_ImageName(AreaPath, "railway");
                        string legend_cache = Path.Combine(AreaPath, "legend_cache.bin");

                        string db_file_railways = Path.Combine(AreaPath, DB_FILENAME_RAILWAYS);
                        string db_file_lightrail = Path.Combine(AreaPath, DB_FILENAME_LIGHTRAIL);

                        bool need_draw = true;
                        bool cache_status_railways = Commons.Check_Cache_Image(db_file_railways, img_cache);

                        if (draw_items.use_cache && cache_status_railways)
                        {
                            need_draw = !Image_Railways.DrawFromCache(img_cache);
                        }

                        if (need_draw)
                        {
                            Progress.Set_Info(true, "Processing railways", 0);

                            using (SQLiteConnection sqlite_railways = new SQLiteConnection("Data Source=" + db_file_railways))
                            using (SQLiteConnection sqlite_lightrail = new SQLiteConnection("Data Source=" + db_file_lightrail))
                            {
                                sqlite_railways.Open();
                                sqlite_lightrail.Open();

                                Image_Railways.Draw(sqlite_railways, bxy, Progress, Set, legend, true);

                                if (Set.Draw_Railway_Lightrail)
                                {
                                    Image_Railways.Draw(sqlite_lightrail, bxy, Progress, Set, legend, false);
                                }

                                sqlite_lightrail.Close();
                                sqlite_railways.Close();
                            }

                            Image_Railways.Save_ImageCache(img_cache, db_file_railways);
                            legend.Save_Cache(legend_cache);
                        }
                        else
                        {
                            legend.Load_Cache(legend_cache);
                        }

                        OnPropertyChanged("Image_Railways");
                    }

                    if (draw_items.cities)
                    {
                        db_file = "Cities";

                        Progress.Set_Info(true, "Drawing cities", 0);

                        Image_Cities.Draw(Stations, bxy, Progress, Set);

                        OnPropertyChanged("Image_Cities");
                    }

                    if (draw_items.scale)
                    {
                        db_file = "Scale";

                        Image_Scale.Draw(bxy, bounds, legend, Set);

                        OnPropertyChanged("Image_Scale");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error processing " + db_file + Environment.NewLine + Environment.NewLine + ex.Message,
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                    return;
                }

                Progress.Set_Info(false, "", 0);
            }
        }

        private void Load_Stations(SQLiteConnection conn, bool lightrail)
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

                        if (name_en != "")
                        {
                            st.has_english = true;
                        }
                        else
                        {
                            st.has_english = false;
                        }

                        st.visible = true;

                        Stations.Add(st);
                    }
                }

                rdr.Close();
            }
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
