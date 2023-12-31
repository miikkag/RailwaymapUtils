﻿using System;
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

        public const string CONFIG_PREFIX = "LBL.";

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

        public void FlipBold(Guid g)
        {
            foreach (LabelCoordinate l in Items)
            {
                if (l.InstanceID == g)
                {
                    l.FontBold = !l.FontBold;
                    l.Refresh();

                    break;
                }
            }
        }

        public void FlipOutline(Guid g)
        {
            foreach (LabelCoordinate l in Items)
            {
                if (l.InstanceID == g)
                {
                    l.Outline = !l.Outline;
                    l.Refresh();

                    break;
                }
            }
        }

        public bool IsEditing()
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

        public List<string> GetConfig()
        {
            List<string> result = new List<string>();

            foreach (LabelCoordinate l in Items)
            {
                StringBuilder str = new StringBuilder(CONFIG_PREFIX);

                str.Append("name"      + Commons.DELIM_EQ + l.Name                 + Commons.DELIM_CONFITEMS);
                str.Append("latitude"  + Commons.DELIM_EQ + l.Latitude.ToString()  + Commons.DELIM_CONFITEMS);
                str.Append("longitude" + Commons.DELIM_EQ + l.Longitude.ToString() + Commons.DELIM_CONFITEMS);
                str.Append("fontname"  + Commons.DELIM_EQ + l.FontName             + Commons.DELIM_CONFITEMS);
                str.Append("fontsize"  + Commons.DELIM_EQ + l.FontSize.ToString()  + Commons.DELIM_CONFITEMS);
                str.Append("fontbold"  + Commons.DELIM_EQ + l.FontBold.ToString()  + Commons.DELIM_CONFITEMS);
                str.Append("outline"   + Commons.DELIM_EQ + l.Outline.ToString());

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
                string fontname = Set.Label_FontName_Default;
                int fontsize = Set.Label_FontSize_Default;
                bool fontbold = Set.Label_FontBold_Default;
                bool outline = false;

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

                            case "outline":
                                bool.TryParse(parts[1], out outline);
                                break;
                        }
                    }
                }

                Items.Add(new LabelCoordinate(name, latitude, longitude, fontname, fontsize, fontbold, outline));
            }
        }
    }
}
