using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RailwaymapUI
{
    public partial class StationUI : UserControl
    {
        public event RoutedEventHandler Click_Valign_Bottom;
        public event RoutedEventHandler Click_Valign_Center;
        public event RoutedEventHandler Click_Valign_Top;

        public event RoutedEventHandler Click_Halign_Left;
        public event RoutedEventHandler Click_Halign_Center;
        public event RoutedEventHandler Click_Halign_Right;

        public event RoutedEventHandler Click_OffsetX_Plus;
        public event RoutedEventHandler Click_OffsetX_Minus;
        public event RoutedEventHandler Click_OffsetY_Plus;
        public event RoutedEventHandler Click_OffsetY_Minus;

        public event RoutedEventHandler Click_Check;
        public event RoutedEventHandler Click_Bold;
        public event RoutedEventHandler Click_Outline;
        public event RoutedEventHandler Click_EN;

        public event RoutedEventHandler Click_Dot2;
        public event RoutedEventHandler Click_Dot3;
        public event RoutedEventHandler Click_Dot4;

        public StationUI()
        {
            InitializeComponent();
        }

        private void SelectField(object sender, RoutedEventArgs e)
        {
            TextBox tb = (sender as TextBox);
            if (tb != null)
            {
                tb.SelectAll();
            }
        }

        private void SelectivelyIgnoreMouseButton(object sender, MouseButtonEventArgs e)
        {
            TextBox tb = (sender as TextBox);
            if (tb != null)
            {
                if (!tb.IsKeyboardFocusWithin)
                {
                    e.Handled = true;
                    tb.Focus();
                }
            }
        }

        private void Station_valign_bottom(object sender, RoutedEventArgs e)
        {
            Click_Valign_Bottom?.Invoke(sender, new RoutedEventArgs());
        }

        private void Station_valign_center(object sender, RoutedEventArgs e)
        {
            Click_Valign_Center?.Invoke(sender, new RoutedEventArgs());
        }

        private void Station_valign_top(object sender, RoutedEventArgs e)
        {
            Click_Valign_Top?.Invoke(sender, new RoutedEventArgs());
        }

        private void Station_halign_left(object sender, RoutedEventArgs e)
        {
            Click_Halign_Left?.Invoke(sender, new RoutedEventArgs());
        }

        private void Station_halign_center(object sender, RoutedEventArgs e)
        {
            Click_Halign_Center?.Invoke(sender, new RoutedEventArgs());
        }

        private void Station_halign_right(object sender, RoutedEventArgs e)
        {
            Click_Halign_Right?.Invoke(sender, new RoutedEventArgs());
        }

        private void Station_Check_click(object sender, RoutedEventArgs e)
        {
            Click_Check?.Invoke(sender, new RoutedEventArgs());
        }

        private void Station_Bold_Click(object sender, RoutedEventArgs e)
        {
            Click_Bold?.Invoke(sender, new RoutedEventArgs());
        }
        private void Station_Outline_Click(object sender, RoutedEventArgs e)
        {
            Click_Outline?.Invoke(sender, new RoutedEventArgs());
        }

        private void Station_EN_Click(object sender, RoutedEventArgs e)
        {
            Click_EN?.Invoke(sender, new RoutedEventArgs());
        }

        private void Station_offsety_plus(object sender, RoutedEventArgs e)
        {
            Click_OffsetY_Plus?.Invoke(sender, new RoutedEventArgs());
        }

        private void Station_offsety_minus(object sender, RoutedEventArgs e)
        {
            Click_OffsetY_Minus?.Invoke(sender, new RoutedEventArgs());
        }

        private void Station_offsetx_plus(object sender, RoutedEventArgs e)
        {
            Click_OffsetX_Plus?.Invoke(sender, new RoutedEventArgs());
        }

        private void Station_offsetx_minus(object sender, RoutedEventArgs e)
        {
            Click_OffsetX_Minus?.Invoke(sender, new RoutedEventArgs());
        }

        private void Station_Dot2(object sender, RoutedEventArgs e)
        {
            Click_Dot2?.Invoke(sender, new RoutedEventArgs());
        }
        private void Station_Dot3(object sender, RoutedEventArgs e)
        {
            Click_Dot3?.Invoke(sender, new RoutedEventArgs());
        }
        private void Station_Dot4(object sender, RoutedEventArgs e)
        {
            Click_Dot4?.Invoke(sender, new RoutedEventArgs());
        }
    }
}
