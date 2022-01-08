using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class ExpandList : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public bool Landarea { get; set; }
        public bool Borders { get; set; }
        public bool Water { get; set; }
        public bool Railways { get; set; }
        public bool Cities { get; set; }

        public ExpandList()
        {
            Landarea = true;
            Borders = true;
            Water = true;
            Railways = true;
            Cities = true;
        }

        public void Expand_All()
        {
            Landarea = true;
            Borders = true;
            Water = true;
            Railways = true;
            Cities = true;

            OnPropertyChanged("Landarea");
            OnPropertyChanged("Borders");
            OnPropertyChanged("Water");
            OnPropertyChanged("Railways");
            OnPropertyChanged("Cities");
        }
    }
}
