using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class MapDB_Labels
    {
        public ObservableCollection<LabelCoordinate> Items { get; set; }

        public const string LABEL_CONF_START = "LBL.";

        public string FontName { get; set; }
        public int FontSize { get; set; }
        public bool FontBold { get; set; }

        public Guid? EditInstance { get; private set; }

        private readonly DrawSettings Set;
        private BoundsXY Bxy;


        public MapDB_Labels(DrawSettings set)
        {
            Items = new ObservableCollection<LabelCoordinate>();
            Set = set;

            FontName = set.Label_FontName_Default;
            FontSize = set.Label_FontSize_Default;
            FontBold = set.Label_FontBold_Default;

            EditInstance = null;

            Bxy = null;
        }

        public void NewItem()
        {
            Items.Add(new LabelCoordinate(FontName, FontSize, FontBold));
        }

        public void RemoveItem(Guid g)
        {
            foreach (LabelCoordinate l in Items)
            {
                if (l.InstanceID == g)
                {
                    Items.Remove(l);
                    break;
                }
            }
        }

        public void SetEditInstance(Guid g)
        {
            EditInstance = g;
        }

        public void SetBounds(BoundsXY bxy)
        {
            Bxy = bxy;
        }

        public void SetItemLocation(int x, int y)
        {
            if (Bxy == null)
            {
                return;
            }

            double lon = Commons.MapX2Lon(x, Bxy);
            double lat = Commons.MapY2Lat(y, Bxy);

            if (EditInstance != null)
            {
                foreach (LabelCoordinate l in Items)
                {
                    if (l.InstanceID == EditInstance)
                    {
                        l.Latitude = lat;
                        l.Longitude = lon;

                        l.Refresh();

                        EditInstance = null;
                        break;
                    }
                }
            }
        }

        public bool Is_Editing()
        {
            return (EditInstance != null);
        }

        public void ApplyAllStyle()
        {
            foreach (LabelCoordinate l in Items)
            {
                l.FontName = FontName;
                l.FontSize = FontSize;
                l.FontBold = FontBold;

                l.Refresh();
            }
        }

        public List<string> Get_Config()
        {
            List<string> result = new List<string>();

            foreach (LabelCoordinate l in Items)
            {
                StringBuilder str = new StringBuilder(LABEL_CONF_START);

                str.Append("name"      + Commons.DELIMs + l.Name                 + Commons.DELIMs_ST);
                str.Append("latitude"  + Commons.DELIMs + l.Latitude.ToString()  + Commons.DELIMs_ST);
                str.Append("longitude" + Commons.DELIMs + l.Longitude.ToString() + Commons.DELIMs_ST);
                str.Append("fontname"  + Commons.DELIMs + l.FontName             + Commons.DELIMs_ST);
                str.Append("fontsize"  + Commons.DELIMs + l.FontSize.ToString()  + Commons.DELIMs_ST);
                str.Append("fontbold"  + Commons.DELIMs + l.FontBold.ToString()  + Commons.DELIMs_ST);

                result.Add(str.ToString());
            }

            return result;
        }

        public void Set_Config(List<string> data)
        {
            Items.Clear();

            foreach (string str in data)
            {
                string[] items = str.Substring(LABEL_CONF_START.Length).Split(Commons.DELIM_ST);

                string name = "";
                double latitude = 0;
                double longitude = 0;
                string fontname = Set.Label_FontName_Default;
                int fontsize = Set.Label_FontSize_Default;
                bool fontbold = Set.Label_FontBold_Default;

                foreach (string pair in items)
                {
                    string[] parts = pair.Split(Commons.DELIM, 2);

                    if (parts.Length == 2)
                    {
                        switch (parts[0])
                        {
                            case "name":
                                name = parts[1];
                                break;

                            case "latitude":
                                double.TryParse(parts[1], out latitude);
                                break;

                            case "longitude":
                                double.TryParse(parts[1], out longitude);
                                break;

                            case "fontname":
                                fontname = parts[1];
                                break;

                            case "fontsize":
                                int.TryParse(parts[1], out fontsize);
                                break;

                            case "fontbold":
                                bool.TryParse(parts[1], out fontbold);
                                break;
                        }
                    }
                }

                Items.Add(new LabelCoordinate(name, latitude, longitude, fontname, fontsize, fontbold));
            }
        }
    }
}
