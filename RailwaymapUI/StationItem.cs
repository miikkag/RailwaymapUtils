using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public enum StationItemType { None, Station, Site, Yard, Lightrail, Halt }

    public class StationItem : INotifyPropertyChanged
    {
        private StationItemType _type;
        public StationItemType Type { get { return _type; }
            set { if (_type != value)
                { _type = value;
                    OnPropertyChanged("Type");
                    OnPropertyChanged("IsSite");
                    OnPropertyChanged("IsYard");
                    OnPropertyChanged("IsStation");
                    OnPropertyChanged("IsLightrail");
                    OnPropertyChanged("IsHalt");
                }
            } }

        public bool IsStation { get { return (Type == StationItemType.Station); } }
        public bool IsSite { get { return (Type==StationItemType.Site); } }
        public bool IsYard { get { return (Type==StationItemType.Yard); } }
        public bool IsLightrail { get { return (Type == StationItemType.Lightrail); } }
        public bool IsHalt { get { return (Type == StationItemType.Halt); } }
        public enum Valign { Top, Center, Bottom };
        public enum Halign { Left, Center, Right };

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public Valign valign;
        public Halign halign;

        private bool _highlighted;
        public bool Highlighted { get { return _highlighted; } set { if (_highlighted != value) { _highlighted = value; OnPropertyChanged("Highlighted"); } } }

        public bool Valign_Top { get { return (valign == Valign.Top); } }
        public bool Valign_Center { get { return (valign == Valign.Center); } }
        public bool Valign_Bottom { get { return (valign == Valign.Bottom); } }

        public bool Halign_Left { get { return (halign == Halign.Left); } }
        public bool Halign_Center { get { return (halign == Halign.Center); } }
        public bool Halign_Right { get { return (halign == Halign.Right); } }

        public Int64 id { get; set; }

        public string use_name { get { if (english) return name_en; else return name; } }
        public string name { get; set; }
        public string name_en { get; set; }
        public string display_name { get; set; }

        public bool from_building { get; set; }
        public bool visible { get; set; }
        public bool bold { get; set; }
        public bool outline { get; set; }

        public int rotation { get; set; }

        private bool _english;
        public bool english { get { return _english; } set { _english = value; display_name = use_name; } }
        public bool has_english { get; set; }

        public string xy { get; private set; }

        public int coordX { get; private set; }
        public int coordY { get; private set; }

        public int offsetx { get; set; }
        public int offsety { get; set; }

        public int dotsize { get; set; }

        public Coordinate Coord { get; set; }

        public void Set_CoordinatesXY(int x, int y)
        {
            xy = string.Format("({0},{1})", x, y);

            if (from_building)
            {
                xy += " b";
            }

            coordX = x;
            coordY = y;

            OnPropertyChanged("xy");
        }

        public void Set_Valign(Valign align)
        {
            valign = align;

            switch (valign)
            {
                case Valign.Top:
                    offsety = -1;
                    break;
                case Valign.Center:
                    offsety = 0;
                    break;
                case Valign.Bottom:
                    offsety = 1;
                    break;
                default:
                    break;
            }
            OnPropertyChanged("offsety");
            OnPropertyChanged("Valign_Top");
            OnPropertyChanged("Valign_Center");
            OnPropertyChanged("Valign_Bottom");
        }

        public void Set_Halign(Halign align)
        {
            halign = align;

            // Set default offset
            switch (halign)
            {
                case Halign.Left:
                    offsetx = 0;
                    break;
                case Halign.Center:
                    offsetx = 0;
                    break;
                case Halign.Right:
                    offsetx = 2;
                    break;
                default:
                    break;
            }

            OnPropertyChanged("offsetx");
            OnPropertyChanged("Halign_Left");
            OnPropertyChanged("Halign_Center");
            OnPropertyChanged("Halign_Right");
        }

        public void Force_Refresh()
        {
            OnPropertyChanged(string.Empty);
        }

        public StationItem(StationItemType item_type)
        {
            Type = item_type;

            Set_Valign(Valign.Top);
            Set_Halign(Halign.Right);

            dotsize = DrawSettings.Dotsize_Station_Default;

            Highlighted = false;
            rotation = 0;
        }
    }
}
