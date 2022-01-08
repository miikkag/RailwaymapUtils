using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RailwaymapUI
{
    public class ProgressInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public Visibility Visible { get; private set; }
        public string Text { get; private set; }
        public int Value { get; set; }

        private bool vis;

        public ProgressInfo()
        {
            Visible = Visibility.Hidden;
            vis = false;
            Text = "";
            Value = 0;
        }

        public void Set_Info(bool visible, string text, int val)
        {
            if (vis != visible)
            {
                vis = visible;

                if (vis)
                {
                    Visible = Visibility.Visible;
                }
                else
                {
                    Visible = Visibility.Hidden;
                }

                OnPropertyChanged("Visible");
            }

            if (Text != text)
            {
                Text = text;
                OnPropertyChanged("Text");
            }

            if (val != Value)
            {
                Value = val;
                OnPropertyChanged("Value");
            }
        }

        public void Set_Info(int val)
        {
            if (val != Value)
            {
                Value = val;
                OnPropertyChanged("Value");
            }
        }

        public void Clear()
        {
            vis = false;
            Visible = Visibility.Hidden;
            Text = "";

            OnPropertyChanged("Visible");
            OnPropertyChanged("Text");
            OnPropertyChanged("Value");
        }
    }
}
