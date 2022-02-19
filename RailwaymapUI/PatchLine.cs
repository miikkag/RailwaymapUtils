using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class PatchLine : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public string Name { get; set; }

        public Coordinate Start { get; set; }
        public Coordinate End { get; set; }

        public System.Drawing.Color LineColor;

        public int Thickness;

        public Guid InstanceID { get; private set; }


        public PatchLine(System.Drawing.Color color)
        {
            Name = "";

            Start = new Coordinate(0, 0);
            End = new Coordinate(0, 0);

            LineColor = color;

            InstanceID = Guid.NewGuid();
        }

        public void Refresh()
        {
            OnPropertyChanged(string.Empty);
        }
    }
}
