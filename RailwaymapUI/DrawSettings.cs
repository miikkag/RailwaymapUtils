using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class DrawSettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public enum ScalePosition { Off, LeftTop, LeftBottom, RightTop, RightBottom };

        public int Normal_Gauge_Min = 1430;

        public Color Color_Border_Land = Color.Black;
        public Color Color_Border_Coastline = Color.Black;
        public Color Color_Border_Territorial = Color.Black;

        //public Color Color_Water = Color.FromArgb(160, 190, 255);
        public Color Color_Water = Color.FromArgb(190, 224, 255);
        public Color Color_Land = Color.FromArgb(255, 255, 255);

        public Color Color_Railways_Diesel = Color.FromArgb(40, 220, 40);
        public Color Color_Railways_1500V = Color.FromArgb(205, 135, 90);
        public Color Color_Railways_3000V = Color.FromArgb(0, 200, 200);
        public Color Color_Railways_15kV = Color.FromArgb(255, 0, 0);
        public Color Color_Railways_25kV = Color.FromArgb(0, 120, 255);
        public Color Color_Railways_Elecrified_other = Color.FromArgb(0, 224, 255);
        public Color Color_Railways_Narrow_Electric = Color.FromArgb(255, 0, 224);
        public Color Color_Railways_Narrow_Diesel = Color.FromArgb(255, 128, 224);
        public Color Color_Railways_Dualgauge = Color.FromArgb(128, 40, 255);
        public Color Color_Railways_Disused = Color.FromArgb(210, 210, 210);
        public Color Color_Railways_Construction = Color.FromArgb(150, 150, 150);

        public Color Color_Cities_Point = Color.FromArgb(0, 0, 0);
        public Color Color_Cities_Name = Color.FromArgb(0, 0, 0);

        public Color Color_Selection_Area = Color.FromArgb(242, 255, 0);
        public Color Color_Selection_Border = Color.FromArgb(93, 124, 0);

        public const int Dotsize_Station_Default = 2;

        public int Scale_km = 100;
        public int Scale_FontSize = 8;
        public string Scale_FontName = "Microsoft Sans Serif";

        public bool Draw_Border_Land { get; set; }
        public bool Draw_Border_Coastline { get; set; }
        public bool Draw_Border_Territorial { get; set; }

        public bool Draw_Railway_Spur { get; set; }

        public int Filter_Water_Line { get; set; }
        public int Filter_Water_Area { get; set; }
        public bool Draw_WaterLand { get; set; }
        public int Filter_WaterLand_Line { get; set; }
        public int Filter_WaterLand_Area { get; set; }
        public int Filter_Border_Line { get; set; }
        public bool Filter_Border_DrawLine { get; set; }
        public int Filter_Railways_Line { get; set; }
        public bool Filter_Railways_DrawLine { get; set; }

        public bool Debug_Water_ID { get; set; }
        public bool Debug_Water_BorderOnly { get; set; }
        public bool Debug_Water_SingleItem { get; set; }
        public int Debug_Water_SingleItemNumber { get; set; }

        public bool LineMethod_Railways_System { get; set; }

        public string FontName_Cities { get; set; }
        public string FontNameBold_Cities { get; set; }
        public int FontSize_Cities { get; set; }
        public int FontSizeBold_Cities { get; set; }

        public bool AutoRedraw_Cities { get; set; }

        private ScalePosition scale_pos;
        public ScalePosition Scale_Position { get { return scale_pos; } set { scale_pos = value; OnPropertyChanged("Scale_Position"); } }

        public int Scale_MarginX { get; set; }
        public int Scale_MarginY { get; set; }


        public DrawSettings()
        {
            Filter_Water_Line = 3;
            Filter_Water_Area = 3;
            Filter_WaterLand_Line = 3;
            Filter_WaterLand_Area = 3;

            Draw_WaterLand = true;
            Filter_Border_Line = 3;
            Filter_Border_DrawLine = true;

            Filter_Railways_Line = 3;
            Filter_Railways_DrawLine = true;

            FontName_Cities = "Microsoft Sans Serif";
            FontNameBold_Cities = "Arial";
            FontSize_Cities = 8;
            FontSizeBold_Cities = 8;

            AutoRedraw_Cities = true;

            Draw_Border_Land = true;
            Draw_Border_Coastline = true;
            Draw_Border_Territorial = true;

            Draw_Railway_Spur = false;

            Debug_Water_ID = false;
            Debug_Water_BorderOnly = false;
            Debug_Water_SingleItem = false;
            Debug_Water_SingleItemNumber = 0;

            Scale_Position = ScalePosition.LeftBottom;
            Scale_MarginX = 15;
            Scale_MarginY = 15;
        }

        public void Set_ScalePos(ScalePosition new_pos)
        {
            if (new_pos == Scale_Position)
            {
                Scale_Position = ScalePosition.Off;
            }
            else
            {
                Scale_Position = new_pos;
            }
        }


        private const string CONFIG_PREFIX = "Draw.";


        public void Read_Config(string[] conf)
        {
            char[] delim = new char[1] { '=' };

            foreach (string str in conf)
            {
                if (str.StartsWith(CONFIG_PREFIX))
                {
                    string[] items = str.Substring(CONFIG_PREFIX.Length).Split(delim, 2);

                    int.TryParse(items[1], out int val_int);
                    bool.TryParse(items[1], out bool val_bool);

                    switch (items[0])
                    {
                        case "Filter_Water_Line":
                            Filter_Water_Line = val_int;
                            break;

                        case "Filter_WaterLand_Area":
                            Filter_WaterLand_Area = val_int;
                            break;

                        case "Filter_WaterLand_Line":
                            Filter_WaterLand_Line = val_int;
                            break;

                        case "Draw_WaterLand":
                            Draw_WaterLand = val_bool;
                            break;

                        case "Filter_Water_Area":
                            Filter_Water_Area = val_int;
                            break;

                        case "Filter_Border_Line":
                            Filter_Border_Line = val_int;
                            break;

                        case "Filter_Border_DrawLine":
                            Filter_Border_DrawLine = val_bool;
                            break;

                        case "LineMethod_Railways_System":
                            LineMethod_Railways_System = val_bool;
                            break;

                        case "FontName_Cities":
                            FontName_Cities = items[1];
                            break;

                        case "FontSize_Cities":
                            FontSize_Cities = val_int;
                            break;

                        case "FontNameBold_Cities":
                            FontNameBold_Cities = items[1];
                            break;

                        case "FontSizeBold_Cities":
                            FontSizeBold_Cities = val_int;
                            break;

                        case "AutoRedraw_Cities":
                            AutoRedraw_Cities = val_bool;
                            break;

                        case "Draw_Border_Land":
                            Draw_Border_Land = val_bool;
                            break;

                        case "Draw_Border_Coastline":
                            Draw_Border_Coastline = val_bool;
                            break;

                        case "Draw_Border_Territorial":
                            Draw_Border_Territorial = val_bool;
                            break;

                        case "Draw_Railway_Spur":
                            Draw_Railway_Spur = val_bool;
                            break;

                        case "Scale_Position":
                            Enum.TryParse(items[1], out scale_pos);
                            break;

                        case "Scale_MarginX":
                            Scale_MarginX = val_int;
                            break;

                        case "Scale_MarginY":
                            Scale_MarginY = val_int;
                            break;

                        default:
                            break;
                    }
                }
            }

            OnPropertyChanged(string.Empty);
        }

        public List<string> Save_Config()
        {
            List<string> result = new List<string>();

            string str;

            str = CONFIG_PREFIX + "Filter_Water_Line=" + Filter_Water_Line.ToString();
            result.Add(str);

            str = CONFIG_PREFIX + "Filter_Water_Area=" + Filter_Water_Area.ToString();
            result.Add(str);

            str = CONFIG_PREFIX + "Filter_WaterLand_Line=" + Filter_WaterLand_Line.ToString();
            result.Add(str);

            str = CONFIG_PREFIX + "Filter_WaterLand_Area=" + Filter_WaterLand_Area.ToString();
            result.Add(str);

            str = CONFIG_PREFIX + "Draw_WaterLand=" + Draw_WaterLand.ToString();
            result.Add(str);

            str = CONFIG_PREFIX + "Filter_Border_Line=" + Filter_Border_Line.ToString();
            result.Add(str);

            str = CONFIG_PREFIX + "Filter_Border_DrawLine=" + Filter_Border_DrawLine.ToString();
            result.Add(str);

            str = CONFIG_PREFIX + "LineMethod_Railways_System=" + LineMethod_Railways_System.ToString();
            result.Add(str);

            str = CONFIG_PREFIX + "FontName_Cities=" + FontName_Cities;
            result.Add(str);

            str = CONFIG_PREFIX + "FontSize_Cities=" + FontSize_Cities.ToString();
            result.Add(str);

            str = CONFIG_PREFIX + "FontNameBold_Cities=" + FontName_Cities;
            result.Add(str);

            str = CONFIG_PREFIX + "FontSizeBold_Cities=" + FontSize_Cities.ToString();
            result.Add(str);

            str = CONFIG_PREFIX + "AutoRedraw_Cities=" + AutoRedraw_Cities.ToString();
            result.Add(str);

            str = CONFIG_PREFIX + "Draw_Border_Land=" + Draw_Border_Land.ToString();
            result.Add(str);

            str = CONFIG_PREFIX + "Draw_Border_Coastline=" + Draw_Border_Coastline.ToString();
            result.Add(str);

            str = CONFIG_PREFIX + "Draw_Border_Territorial=" + Draw_Border_Territorial.ToString();
            result.Add(str);

            str = CONFIG_PREFIX + "Draw_Railway_Spur=" + Draw_Railway_Spur.ToString();
            result.Add(str);

            str = CONFIG_PREFIX + "Scale_Position=" + Scale_Position.ToString();
            result.Add(str);

            str = CONFIG_PREFIX + "Scale_MarginX=" + Scale_MarginX.ToString();
            result.Add(str);

            str = CONFIG_PREFIX + "Scale_MarginY=" + Scale_MarginY.ToString();
            result.Add(str);


            return result;
        }
    }
}
