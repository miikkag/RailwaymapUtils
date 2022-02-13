using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class ColorCoordinate : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public string Name { get; set; }

        public System.Windows.Media.Brush FillColor {
            get {
                int r = (ColorValue & 0xFF0000) >> 16;
                int g = (ColorValue & 0x00FF00) >> 8;
                int b = (ColorValue & 0x0000FF);

                return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)r,(byte)g,(byte)b));
            }
        }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public int ColorValue;
        public string ColorHex { get { return ColorValue.ToString("X6"); } set { _ = int.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ColorValue); OnPropertyChanged("FillColor"); } }


        public Guid InstanceID { get; private set; }

        public ColorCoordinate()
        {
            Name = "";
            Latitude = 0.0;
            Longitude = 0.0;

            ColorValue = 0xFFFFFF;

            InstanceID = Guid.NewGuid();
        }

        public ColorCoordinate(string name, double lat, double lon, string colorhex)
        {
            Name = name;
            Latitude = lat;
            Longitude = lon;

            ColorHex = colorhex;

            InstanceID = Guid.NewGuid();
        }

        public void Refresh()
        {
            OnPropertyChanged(string.Empty);
        }
    }
}
