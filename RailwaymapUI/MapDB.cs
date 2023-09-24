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


        private const string CONFIG_FILENAME = "area.config";
        private const string BOUNDS_FILENAME = "bbox.txt";

        private const string DB_FILENAME_RAILWAYS = "railway.db";
        private const string DB_FILENAME_LIGHTRAIL = "lightrail.db";
        private const string DB_FILENAME_BORDER = "border.db";
        private const string DB_FILENAME_COASTLINE = "coastline.db";
        private const string DB_FILENAME_COASTLINE_CACHE = "coastline_cache.bin";
        private const string DB_FILENAME_LAKES = "lakes.db";
        private const string DB_FILENAME_LAKES_CACHE = "lakes_cache.bin";
        private const string DB_FILENAME_RIVERS = "rivers.db";
        private const string DB_FILENAME_RIVERS_CACHE = "rivers_cache.bin";


        public string Area { get; private set; }
        private string AreaPath;
        private string AreaConfigFilename;
        private string BoundsFilename;

        private object drawlock;

        public string OutputFileName { get; set; }

        public int OutputSizeWidth { get; set; }
        public int OutputSizeHeight { get; set; }

        public double CursorLatitude { get; private set; }
        public double CursorLongitude { get; private set; }

        public int CursorX { get; private set; }
        public int CursorY { get; private set; }

        public MapImage_Background Image_Background { get; private set; }
        public MapImage_Landarea Image_Landarea { get; private set; }
        public MapImage_Lakes Image_Water { get; private set; }
        public MapImage_Rivers Image_Rivers { get; private set; }
        public MapImage_Borders Image_Borders { get; private set; }
        public MapImage_Railways Image_Railways { get; private set; }
        public MapImage_Cities Image_Cities { get; private set; }
        public MapImage_CountryColors Image_CountryColors { get; private set; }
        public MapImage_Scale Image_Scale { get; private set; }
        public MapImage_Labels Image_Labels { get; private set; }

        public MapImage_Selection Image_Selection { get; private set; }

        public ProgressInfo Progress { get; private set; }

        public DrawSettings Set { get; private set; }

        public MapDB_Labels Labels { get; private set; }
        public MapDB_CountryColors CountryColors { get; private set; }
        public MapDB_BorderPatch BorderPatch { get; private set; }
        public MapDB_Stations Stations { get; private set; }

        private DateTime railways_timestamp;

        private Bounds bounds;
        private BoundsXY bxy;

        private RailwayLegend legend;

        private FileHistory history;

        private struct DrawItems
        {
            public bool use_cache;

            public bool landarea;
            public bool water;
            public bool rivers;
            public bool borders;
            public bool railways;
            public bool cities;
            public bool scale;
            public bool countrycolors;
            public bool labels;
        }

        private DrawItems draw_items;

        public MapDB(FileHistory file_history)
        {
            OutputSizeWidth = 1000;
            OutputSizeHeight = 1000;

            Progress = new ProgressInfo();

            Image_Background = new MapImage_Background();
            Image_Landarea = new MapImage_Landarea();
            Image_Water = new MapImage_Lakes();
            Image_Rivers = new MapImage_Rivers();
            Image_Borders = new MapImage_Borders();
            Image_Railways = new MapImage_Railways();
            Image_Cities = new MapImage_Cities();
            Image_CountryColors = new MapImage_CountryColors();
            Image_Scale = new MapImage_Scale();
            Image_Labels = new MapImage_Labels();
            Image_Selection = new MapImage_Selection();

            Image_Selection.Enabled = false;

            Set = new DrawSettings();

            Labels = new MapDB_Labels(Set);
            CountryColors = new MapDB_CountryColors(Set);
            BorderPatch = new MapDB_BorderPatch(Set);
            Stations = new MapDB_Stations();

            bounds = new Bounds();
            bxy = null;

            legend = new RailwayLegend();

            draw_items = new DrawItems();

            railways_timestamp = DateTime.MinValue;

            drawlock = new object();

            history = file_history;
        }

        public void Reset_Size()
        {
            Image_Background.Set_Size(OutputSizeWidth, OutputSizeHeight);
            Image_Landarea.Set_Size(OutputSizeWidth, OutputSizeHeight);
            Image_Water.Set_Size(OutputSizeWidth, OutputSizeHeight);
            Image_Rivers.Set_Size(OutputSizeWidth, OutputSizeHeight);
            Image_Borders.Set_Size(OutputSizeWidth, OutputSizeHeight);
            Image_Railways.Set_Size(OutputSizeWidth, OutputSizeHeight);
            Image_Cities.Set_Size(OutputSizeWidth, OutputSizeHeight);
            Image_CountryColors.Set_Size(OutputSizeWidth, OutputSizeHeight);
            Image_Scale.Set_Size(OutputSizeWidth, OutputSizeHeight);
            Image_Labels.Set_Size(OutputSizeWidth, OutputSizeHeight);
            Image_Selection.Set_Size(OutputSizeWidth, OutputSizeHeight);

            Image_Background.Fill();

            OnPropertyChanged("Image_Background");

            draw_items.use_cache = true;

            draw_items.landarea = true;
            draw_items.water = true;
            draw_items.rivers = true;
            draw_items.borders = true;
            draw_items.railways = true;
            draw_items.cities = true;
            draw_items.scale = true;
            draw_items.labels = true;
            draw_items.countrycolors = true;

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
            draw_items.labels = (item == MapItems.Labels);
            draw_items.countrycolors = (item == MapItems.CountryColors);

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


        public void Set_Cursor_Coordinates(int x, int y)
        {
            CursorLongitude = Commons.MapX2Lon(x, bxy);
            CursorLatitude = Commons.MapY2Lat(y,bxy);
            CursorX = x;
            CursorY = y;
            
            OnPropertyChanged("CursorLongitude");
            OnPropertyChanged("CursorLatitude");
            OnPropertyChanged("CursorX");
            OnPropertyChanged("CursorY");
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

#if !DEBUG
                    try
                    {
#endif
                    Stations.Load_Stations(Path.Combine(area_path, DB_FILENAME_RAILWAYS), Path.Combine(AreaPath, DB_FILENAME_LIGHTRAIL));
#if !DEBUG
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error opening railway/lightrail database" + Environment.NewLine + Environment.NewLine + ex.Message,
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
#endif
                    Load_Config();
                    Load_Bounds();
                }
                else
                {
                    MessageBox.Show(area_path + Environment.NewLine + "is not a valid directory", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                OnPropertyChanged("Area");
                OnPropertyChanged("OutputFileName");

                history.Add_Item(area_path);
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

            if (Image_Rivers.Enabled)
            {
                gr.DrawImage(Image_Rivers.GetBitmap(), 0, 0);
            }

            if (Image_Borders.Enabled)
            {
                gr.DrawImage(Image_Borders.GetBitmap(), 0, 0);
            }

            if (Image_CountryColors.Enabled)
            {
                gr.DrawImage(Image_CountryColors.GetBitmap(), 0, 0);
            }

            if (Image_Railways.Enabled)
            {
                gr.DrawImage(Image_Railways.GetBitmap(), 0, 0);
            }

            if (Image_Cities.Enabled)
            {
                gr.DrawImage(Image_Cities.GetBitmap(), 0, 0);
            }

            if (Image_Labels.Enabled)
            {
                gr.DrawImage(Image_Labels.GetBitmap(), 0, 0);
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

        public void Save_Config()
        {
            if (AreaConfigFilename != "")
            {
                List<string> all = new List<string>();

                List<string> items_set = Set.Save_Config();

                all.AddRange(items_set);

                all.Add(DB_CONFIG_PREFIX + "OutputWidth"  + Commons.DELIM_EQ + OutputSizeWidth.ToString());
                all.Add(DB_CONFIG_PREFIX + "OutputHeight" + Commons.DELIM_EQ + OutputSizeHeight.ToString());

                all.Add(DB_CONFIG_PREFIX + "Separate_NonVisibleStations" + Commons.DELIM_EQ + Stations.Separate_NonVisibleStations.ToString());

                all.AddRange(Stations.GetConfig());
                all.AddRange(CountryColors.GetConfig());
                all.AddRange(Labels.GetConfig());
                all.AddRange(BorderPatch.GetConfig());

                File.WriteAllLines(AreaConfigFilename, all);
            }
        }

        public void Load_Config()
        {
            if (AreaConfigFilename != "")
            {
                if (!File.Exists(AreaConfigFilename))
                {
                    return;
                }

                string[] lines = File.ReadAllLines(AreaConfigFilename);

                List<string> label_items = new List<string>();
                List<string> cc_items = new List<string>();
                List<string> bpatch_items = new List<string>();
                List<string> st_items = new List<string>();

                Set.Read_Config(lines);

                foreach (string str in lines)
                {
                    if (str.StartsWith(DB_CONFIG_PREFIX))
                    {
                        string[] items = str.Substring(DB_CONFIG_PREFIX.Length).Split(Commons.DELIM_EQ.ToCharArray(), 2);

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
                                Stations.Separate_NonVisibleStations = tmpval;
                                break;

                            default:
                                break;
                        }
                    }
                    else if (str.StartsWith(MapDB_Stations.CONFIG_PREFIX))
                    {
                        st_items.Add(str);
                    }
                    else if (str.StartsWith(MapDB_Labels.CONFIG_PREFIX))
                    {
                        label_items.Add(str);
                    }
                    else if (str.StartsWith(MapDB_CountryColors.CONFIG_PREFIX))
                    {
                        cc_items.Add(str);
                    }
                    else if (str.StartsWith(MapDB_BorderPatch.CONFIG_PREFIX))
                    {
                        bpatch_items.Add(str);
                    }
                }

                Stations.SetConfig(st_items);
                Labels.SetConfig(label_items);
                CountryColors.SetConfig(cc_items);
                BorderPatch.SetConfig(bpatch_items);
            }

            OnPropertyChanged(string.Empty);
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

                            Labels.SetBounds(bxy);
                            CountryColors.SetBounds(bxy);
                            BorderPatch.SetBounds(bxy);
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


        private void DrawAllThread()
        {
            lock (drawlock)
            {
                string db_file = "Init";

#if !DEBUG
                try
#endif
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

                            Image_Borders.Draw(sqlite_border, BorderPatch.Items.ToList(), bxy, Progress, Set);

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

                    if (draw_items.rivers)
                    {
                        db_file = Path.Combine(AreaPath, DB_FILENAME_RIVERS);

                        string db_cache = Path.Combine(AreaPath, DB_FILENAME_RIVERS_CACHE);

                        if (!Commons.Check_Cache_DB(db_file, db_cache))
                        {
                            RiverCache.Convert_Rivers(db_file, db_cache, Progress);
                        }

                        string img_cache = Commons.Cache_ImageName(AreaPath, "rivers");

                        bool need_draw = true;

                        if (draw_items.use_cache && Commons.Check_Cache_Image(db_file, img_cache))
                        {
                            need_draw = !Image_Water.DrawFromCache(img_cache);
                        }

                        if (need_draw)
                        {
                            Image_Rivers.Draw(db_cache, bxy, Progress, Set);
                            Image_Rivers.Save_ImageCache(img_cache, db_file);
                        }

                        OnPropertyChanged("Image_Rivers");
                    }


                    if (draw_items.countrycolors)
                    {
                        db_file = "country colors";

                        Image_CountryColors.Draw(bxy, bounds, Set, Progress, CountryColors.Items.ToList(), Image_Landarea, Image_Water, Image_Borders);

                        OnPropertyChanged("Image_CountryColors");
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

                        railways_timestamp = Commons.Get_DB_Timestamp(db_file_railways);

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

                        Image_Cities.Draw(Stations.Items, bxy, Progress, Set);

                        OnPropertyChanged("Image_Cities");
                    }

                    if (draw_items.labels)
                    {
                        db_file = "Labels";

                        Progress.Set_Info(true, "Drawing labels", 0);

                        Image_Labels.Draw(Labels.Items.ToList(), Set.Color_LabelText, Set.Color_LabelOutline, bxy);

                        OnPropertyChanged("Image_Labels");

                    }

                    if (draw_items.scale)
                    {
                        db_file = "Scale";

                        Image_Scale.Draw(bxy, bounds, legend, Set, railways_timestamp);

                        OnPropertyChanged("Image_Scale");
                    }
                }
#if !DEBUG
                catch (Exception ex)
                {
                    MessageBox.Show("Error processing " + db_file + Environment.NewLine + Environment.NewLine + ex.Message,
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                
                    return;
                }
#endif

                Progress.Set_Info(false, "", 0);
            }
        }

        public void Refresh_Selection()
        {
            OnPropertyChanged("Image_Selection");
        }
    }
}
