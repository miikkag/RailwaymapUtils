using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class MapDB_BorderPatch
    {
        public const string CONFIG_PREFIX = "BPATCH.";

        public ObservableCollection<PatchLine> Items { get; set; }
        public Guid? EditInstance { get; private set; }

        private readonly DrawSettings Set;
        private BoundsXY Bxy;


        public MapDB_BorderPatch(DrawSettings set)
        {
            Set = set;
            Bxy = null;

            Items = new ObservableCollection<PatchLine>();

            EditInstance = null;
        }

        public void NewItem()
        {
            Items.Add(new PatchLine(Set.Color_Border));
        }

        public void RemoveItem(Guid g)
        {
            foreach (PatchLine p in Items)
            {
                if (p.InstanceID == g)
                {
                    Items.Remove(p);
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
                foreach (PatchLine p in Items)
                {
                    if (p.InstanceID == EditInstance)
                    {
                        if ((p.Start.Latitude == 0) && (p.Start.Longitude == 0))
                        {
                            p.Start.Latitude = lat;
                            p.Start.Longitude = lon;
                        }
                        else
                        {
                            p.End.Latitude = lat;
                            p.End.Longitude = lon;

                            EditInstance = null;
                        }

                        p.Refresh();

                        break;
                    }
                }
            }
        }


        public List<string> GetConfig()
        {
            List<string> result = new List<string>();

            foreach (PatchLine p in Items)
            {
                StringBuilder str = new StringBuilder(CONFIG_PREFIX);

                str.Append("name"      + Commons.DELIM_EQ + p.Name                       + Commons.DELIM_CONFITEMS);
                str.Append("start.lat" + Commons.DELIM_EQ + p.Start.Latitude.ToString()  + Commons.DELIM_CONFITEMS);
                str.Append("start.lon" + Commons.DELIM_EQ + p.Start.Longitude.ToString() + Commons.DELIM_CONFITEMS);
                str.Append("end.lat"   + Commons.DELIM_EQ + p.End.Latitude.ToString()    + Commons.DELIM_CONFITEMS);
                str.Append("end.lon"   + Commons.DELIM_EQ + p.End.Longitude.ToString());

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
                double start_latitude = 0;
                double start_longitude = 0;
                double end_latitude = 0;
                double end_longitude = 0;

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

                            case "start.lat":
                                double.TryParse(parts[1], out start_latitude);
                                break;

                            case "start.lon":
                                double.TryParse(parts[1], out start_longitude);
                                break;

                            case "end.lat":
                                double.TryParse(parts[1], out end_latitude);
                                break;

                            case "end.lon":
                                double.TryParse(parts[1], out end_longitude);
                                break;
                        }
                    }
                }

                PatchLine p = new PatchLine(Set.Color_Border);

                p.Name = name;
                p.Start.Latitude = start_latitude;
                p.Start.Longitude = start_longitude;
                p.End.Latitude = end_latitude;
                p.End.Longitude = end_longitude;

                Items.Add(p);
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
