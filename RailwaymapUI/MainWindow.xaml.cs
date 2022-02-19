using Microsoft.Win32;
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
    public partial class MainWindow : Window
    {
        public MapDB DB;
        public ExpandList ExpandItems;

        public MainWindow()
        {
            InitializeComponent();

            DB = new MapDB();
            ExpandItems = new ExpandList();

            DataContext = new { DB, ExpandItems, zoomer };
        }

        private void MainWindow_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files.Length > 0)
            {
                DB.Load_Area(files[0]);
            }
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


        private void Refresh_ImageSize(object sender, RoutedEventArgs e)
        {
            DB.Reset_Size();
        }

        private void Refresh_Scale(object sender, RoutedEventArgs e)
        {
            DB.Reset_Single(MapItems.Scale);
        }

        private void Refresh_ImageWater(object sender, RoutedEventArgs e)
        {
            DB.Reset_Single(MapItems.Water);
        }

        private void Refresh_ImageBorder(object sender, RoutedEventArgs e)
        {
            DB.Reset_Single(MapItems.Borders);
        }

        private void Refresh_ImageLandarea(object sender, RoutedEventArgs e)
        {
            DB.Reset_Single(MapItems.Landarea);
        }

        private void Refresh_ImageRailways(object sender, RoutedEventArgs e)
        {
            DB.Reset_Single(MapItems.Railways);
        }
        private void Refresh_ImageCities(object sender, RoutedEventArgs e)
        {
            DB.Reset_Single(MapItems.Cities);
        }

        private void Refresh_ImageLabels(object sender, RoutedEventArgs e)
        {
            DB.Reset_Single(MapItems.Labels);
        }

        private void Refresh_ImageCountryColors(object sender, RoutedEventArgs e)
        {
            DB.Reset_Single(MapItems.CountryColors);
        }


        private void Expand_All(object sender, RoutedEventArgs e)
        {
            ExpandItems.Expand_All();
        }

        private void Zoom_Minus(object sender, RoutedEventArgs e)
        {
            zoomer.Zoom_Out();
        }

        private void Zoom_Plus(object sender, RoutedEventArgs e)
        {
            zoomer.Zoom_In();
        }

        private void Zoom_Reset(object sender, RoutedEventArgs e)
        {
            zoomer.Reset();
        }

        private void Selection_Toggle(object sender, RoutedEventArgs e)
        {
            if (DB.Image_Selection.Enabled)
            {
                DB.Image_Selection.Enabled = false;

                DB.Refresh_Selection(null);
            }
            else
            {
                zoomer.Selection_Clear();

                DB.Image_Selection.Enabled = true;

                DB.Refresh_Selection(zoomer);
            }
        }

        private void Selection_Plus(object sender, RoutedEventArgs e)
        {
            zoomer.Selection_Plus();
        }

        private void Selection_Minus(object sender, RoutedEventArgs e)
        {
            zoomer.Selection_Minus();
        }

        private void Selection_Handle_Update(object sender, RoutedEventArgs e)
        {
            if (DB.IsEditing_CountryColor())
            {
                DB.Set_CountryColorLocation((int)zoomer.Cursor_Point.X, (int)zoomer.Cursor_Point.Y);
            }
            else if (DB.IsEditing_BorderPatchLine())
            {
                DB.Set_BorderPatchLineLocation((int)zoomer.Cursor_Point.X, (int)zoomer.Cursor_Point.Y);
            }
            else if (DB.Labels.Is_Editing())
            {
                DB.Labels.SetItemLocation((int)zoomer.Cursor_Point.X, (int)zoomer.Cursor_Point.Y);
            }
            else
            {
                DB.Image_Selection.Draw(zoomer, DB.Set);

                if (DB.Image_Selection.Enabled)
                {
                    DB.Refresh_Selection(zoomer);
                }
                else
                {
                    DB.Refresh_Selection(null);
                }
            }
        }

        private void zoomer_Coordinate_Update(object sender, RoutedEventArgs e)
        {
            DB.Set_Cursor_Coordinates((int)zoomer.Cursor_Point.X, (int)zoomer.Cursor_Point.Y);
        }


        private void Stations_Sort_Name(object sender, RoutedEventArgs e)
        {
            DB.Sort_Stations(MapDB.SortOrder.Name);
        }

        private void Stations_Sort_Lat(object sender, RoutedEventArgs e)
        {
            DB.Sort_Stations(MapDB.SortOrder.Lat);
        }

        private void Stations_Deselect_Highlighted(object sender, RoutedEventArgs e)
        {
            DB.Deselect_Highlighted();
        }

        private void Stations_EN_Highlighted(object sender, RoutedEventArgs e)
        {
            DB.EN_Highlighted();
        }

        private void Station_valign_bottom(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Button).Tag.ToString(), out Int64 id);

            DB.Set_Station_Valign(id, StationItem.Valign.Bottom);
        }

        private void Station_valign_center(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Button).Tag.ToString(), out Int64 id);

            DB.Set_Station_Valign(id, StationItem.Valign.Center);
        }

        private void Station_valign_top(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Button).Tag.ToString(), out Int64 id);

            DB.Set_Station_Valign(id, StationItem.Valign.Top);
        }

        private void Station_halign_left(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Button).Tag.ToString(), out Int64 id);

            DB.Set_Station_Halign(id, StationItem.Halign.Left);
        }

        private void Station_halign_center(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Button).Tag.ToString(), out Int64 id);

            DB.Set_Station_Halign(id, StationItem.Halign.Center);
        }

        private void Station_halign_right(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Button).Tag.ToString(), out Int64 id);

            DB.Set_Station_Halign(id, StationItem.Halign.Right);
        }

        private void Station_Check_Click(object sender, RoutedEventArgs e)
        {
            if (DB.Set.AutoRedraw_Cities)
            {
                DB.Reset_Single(MapItems.Cities);
            }
        }

        private void Station_Bold_Click(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Button).Tag.ToString(), out Int64 id);

            DB.Change_Bold(id);
        }

        private void Station_Outline_Click(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Button).Tag.ToString(), out Int64 id);

            DB.Change_Outline(id);
        }


        private void Station_EN_Click(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Button).Tag.ToString(), out Int64 id);

            DB.Change_EN(id);
        }



        private void Station_OffsetXPlus_Click(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Button).Tag.ToString(), out Int64 id);

            DB.Adjust_Station_OffsetX(id, 1);
        }
        private void Station_OffsetXMinus_Click(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Button).Tag.ToString(), out Int64 id);

            DB.Adjust_Station_OffsetX(id, -1);
        }
        private void Station_OffsetYPlus_Click(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Button).Tag.ToString(), out Int64 id);

            DB.Adjust_Station_OffsetY(id, 1);
        }
        private void Station_OffsetYMinus_Click(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Button).Tag.ToString(), out Int64 id);

            DB.Adjust_Station_OffsetY(id, -1);
        }

        private void Station_Dot2_Click(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Button).Tag.ToString(), out Int64 id);

            DB.Set_Station_Dot(id, 2);
        }

        private void Station_Dot3_Click(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Button).Tag.ToString(), out Int64 id);

            DB.Set_Station_Dot(id, 3);
        }

        private void Station_Dot4_Click(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Button).Tag.ToString(), out Int64 id);

            DB.Set_Station_Dot(id, 4);
        }

        private void ScalePosition_Click(object sender, RoutedEventArgs e)
        {
            DrawSettings.ScalePosition newpos = (DrawSettings.ScalePosition)(sender as Button).Tag;

            DB.Set.Set_ScalePos(newpos);

            DB.Reset_Single(MapItems.Scale);
        }

        private void LegendPosition_Click(object sender, RoutedEventArgs e)
        {
            DrawSettings.ScalePosition newpos = (DrawSettings.ScalePosition)(sender as Button).Tag;

            DB.Set.Set_LegendPos(newpos);

            DB.Reset_Single(MapItems.Scale);
        }

        private void Export_Image_Click(object sender, RoutedEventArgs e)
        {
            DB.Export_Image(false);
        }

        private void Copy_Image_Click(object sender, RoutedEventArgs e)
        {
            DB.Export_Image(true);
        }

        private void AutoSizeW_Click(object sender, RoutedEventArgs e)
        {
            DB.AutoSize(true, false);
        }

        private void AutoSizeH_Click(object sender, RoutedEventArgs e)
        {
            DB.AutoSize(false, true);
        }


        private void Click_SaveConfig(object sender, RoutedEventArgs e)
        {
            DB.Save_Config();
        }

        private void AddCountryColor_Click(object sender, RoutedEventArgs e)
        {
            DB.AddCountryColor();
        }

        private void RemoveCountryColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            {
                Guid g = (Guid)(sender as Button).Tag;

                DB.RemoveCountryColor(g);
            }
        }

        private void EditCountryColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            {
                Guid g = (Guid)(sender as Button).Tag;

                DB.Set_CountryColorInstance(g);
            }
        }


        private void AddBorderPatch_Click(object sender, RoutedEventArgs e)
        {
            DB.AddBorderPatchLine();
        }

        private void RemoveBorderPatch_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            {
                Guid g = (Guid)(sender as Button).Tag;

                DB.RemoveBorderPatchLine(g);
            }
        }

        private void EditBorderPatch_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            {
                Guid g = (Guid)(sender as Button).Tag;

                DB.Set_BorderPatchLineInstance(g);
            }
        }

        private void AddLabel_Click(object sender, RoutedEventArgs e)
        {
            DB.Labels.NewItem();
        }

        private void RemoveLabel_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            {
                Guid g = (Guid)(sender as Button).Tag;

                DB.Labels.RemoveItem(g);
            }
        }

        private void EditLabel_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            {
                Guid g = (Guid)(sender as Button).Tag;

                DB.Labels.SetEditInstance(g);
            }
        }

        private void ApplyAllLabel_Click(object sender, RoutedEventArgs e)
        {
            DB.Labels.ApplyAllStyle();
        }


        private void Show_HideStationMenu_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu cm = this.FindResource("HideStationsMenu") as ContextMenu;
            cm.PlacementTarget = sender as Button;
            cm.IsOpen = true;
        }

        private void HideStationsMenu_Click(object sender, RoutedEventArgs e)
        {
            string tagstr = (sender as MenuItem).Tag.ToString();

            if (tagstr == "station")
            {
                DB.HideAll(StationItemType.Station);
            }
            else if (tagstr == "lightrail")
            {
                DB.HideAll(StationItemType.Lightrail);
            }
            else if (tagstr == "site")
            {
                DB.HideAll(StationItemType.Site);
            }
            else if (tagstr == "yard")
            {
                DB.HideAll(StationItemType.Yard);
            }
        }
    }
}
