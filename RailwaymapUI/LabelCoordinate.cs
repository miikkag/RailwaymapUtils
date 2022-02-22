using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class LabelCoordinate : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public string FontName { get; set; }
        public int FontSize { get; set; }
        public bool FontBold { get; set; }

        public bool Outline { get; set; }

        public Guid InstanceID { get; private set; }

        public LabelCoordinate(string fontname, int fontsize, bool fontbold)
        {
            Name = "";
            Latitude = 0;
            Longitude = 0;

            FontName = fontname;
            FontSize = fontsize;
            FontBold = fontbold;

            Outline = false;

            InstanceID = Guid.NewGuid();
        }

        public LabelCoordinate(string name, double lat, double lon, string fontname, int fontsize, bool fontbold, bool outline)
        {
            Name = name;
            Latitude = lat;
            Longitude = lon;

            FontName = fontname;
            FontSize = fontsize;
            FontBold = fontbold;

            Outline = outline;

            InstanceID = Guid.NewGuid();
        }

        public void Refresh()
        {
            OnPropertyChanged(string.Empty);
        }
    }
}
