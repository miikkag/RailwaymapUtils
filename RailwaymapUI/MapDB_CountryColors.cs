using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class MapDB_CountryColors
    {
        public const string CONFIG_PREFIX = "CC.";

        public ObservableCollection<ColorCoordinate> Items { get; set; }
        public Guid? EditInstance { get; private set; }

        private readonly DrawSettings Set;
        private BoundsXY Bxy;

        public MapDB_CountryColors(DrawSettings set)
        {
            Set = set;
            Bxy = null;

            Items = new ObservableCollection<ColorCoordinate>();

            EditInstance = null;
        }



        public void NewItem()
        {
            Items.Add(new ColorCoordinate(Set.ColorValue_CountryColorDefault));
        }

        public void RemoveItem(Guid g)
        {
            foreach (ColorCoordinate cc in Items)
            {
                if (cc.InstanceID == g)
                {
                    Items.Remove(cc);
                    break;
                }
            }
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
                foreach (ColorCoordinate cc in Items)
                {
                    if (cc.InstanceID == EditInstance)
                    {
                        cc.Latitude = lat;
                        cc.Longitude = lon;

                        cc.Refresh();

                        EditInstance = null;
                        break;
                    }
                }
            }
        }


        public List<string> GetConfig()
        {
            List<string> result = new List<string>();

            foreach (ColorCoordinate cc in Items)
            {
                StringBuilder str = new StringBuilder(CONFIG_PREFIX);

                str.Append("name"  + Commons.DELIM_EQ + cc.Name                 + Commons.DELIM_CONFITEMS);
                str.Append("lat"   + Commons.DELIM_EQ + cc.Latitude.ToString()  + Commons.DELIM_CONFITEMS);
                str.Append("lon"   + Commons.DELIM_EQ + cc.Longitude.ToString() + Commons.DELIM_CONFITEMS);
                str.Append("color" + Commons.DELIM_EQ + cc.ColorHex);

                result.Add(str.ToString());
            }

            return result;
        }


        public void SetConfig(List<string> data)
        {
            Items.Clear();

            foreach (string str in data)
            {
                string[] items = str.Substring(CONFIG_PREFIX.Length).Split(Commons.DELIM_CONFITEMS.ToCharArray());

                string name = "";
                double latitude = 0;
                double longitude = 0;
                string colorhex = "";

                foreach (string pair in items)
                {
                    string[] parts = pair.Split(Commons.DELIM_EQ.ToCharArray(), 2);

                    if (parts.Length == 2)
                    {
                        switch (parts[0])
                        {
                            case "name":
                                name = parts[1];
                                break;

                            case "lat":
                                double.TryParse(parts[1], out latitude);
                                break;

                            case "lon":
                                double.TryParse(parts[1], out longitude);
                                break;

                            case "color":
                                colorhex = parts[1];
                                break;
                        }
                    }
                }

                if (colorhex != "")
                {
                    ColorCoordinate cc = new ColorCoordinate(name, latitude, longitude, colorhex);

                    Items.Add(cc);
                }
            }
        }

        public void SetEditInstance(Guid g)
        {
            EditInstance = g;
        }

        public bool IsEditing()
        {
            return (EditInstance != null);
        }

        public void SetBounds(BoundsXY bxy)
        {
            Bxy = bxy;
        }
    }
}
