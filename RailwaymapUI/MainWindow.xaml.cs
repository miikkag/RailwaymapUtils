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
        public FileHistory History;

        private const string SETTINGS_FILENAME = "settings.conf";

        public MainWindow()
        {
            InitializeComponent();

            History = new FileHistory(SETTINGS_FILENAME);
            DB = new MapDB(History);
            ExpandItems = new ExpandList();

            DataContext = new { DB, ExpandItems, History, zoomer };
        }

        private void MainWindow_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files != null)
            {
                if (files.Length > 0)
                {
                    DB.Load_Area(files[0]);
                }
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

        private void Refresh_ImageRivers(object sender, RoutedEventArgs e)
        {
            DB.Reset_Single(MapItems.Rivers);
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

                DB.Stations.Refresh_Selection(null);
                DB.Refresh_Selection();
            }
            else
            {
                zoomer.Selection_Clear();

                DB.Image_Selection.Enabled = true;

                DB.Stations.Refresh_Selection(zoomer);
                DB.Refresh_Selection();
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
            if (DB.CountryColors.IsEditing())
            {
                DB.CountryColors.SetItemLocation((int)zoomer.Cursor_Point.X, (int)zoomer.Cursor_Point.Y);
            }
            else if (DB.BorderPatch.IsEditing())
            {
                DB.BorderPatch.SetItemLocation((int)zoomer.Cursor_Point.X, (int)zoomer.Cursor_Point.Y);
            }
            else if (DB.Labels.IsEditing())
            {
                DB.Labels.SetItemLocation((int)zoomer.Cursor_Point.X, (int)zoomer.Cursor_Point.Y);

                if (DB.Set.AutoRedraw_Labels)
                {
                    DB.Reset_Single(MapItems.Labels);
                }
            }
            else
            {
                DB.Image_Selection.Draw(zoomer, DB.Set);

                if (DB.Image_Selection.Enabled)
                {
                    DB.Stations.Refresh_Selection(zoomer);
                }
                else
                {
                    DB.Stations.Refresh_Selection(null);
                }

                DB.Refresh_Selection();
            }
        }

        private void zoomer_Coordinate_Update(object sender, RoutedEventArgs e)
        {
            DB.Set_Cursor_Coordinates((int)zoomer.Cursor_Point.X, (int)zoomer.Cursor_Point.Y);
        }


        private void Stations_Sort_Name(object sender, RoutedEventArgs e)
        {
            DB.Stations.Sort_Stations(MapDB_Stations.SortOrder.Name);
        }

        private void Stations_Sort_Lat(object sender, RoutedEventArgs e)
        {
            DB.Stations.Sort_Stations(MapDB_Stations.SortOrder.Lat);
        }

        private void Stations_Deselect_Highlighted(object sender, RoutedEventArgs e)
        {
            DB.Stations.Set_Highlighted_Deselect();
        }

        private void Stations_EN_Highlighted(object sender, RoutedEventArgs e)
        {
            DB.Stations.Set_Highlighted_EN();
        }

        private void Station_valign_bottom(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Button).Tag.ToString(), out Int64 id);

            DB.Stations.Set_Station_Valign(id, StationItem.Valign.Bottom);

            if (DB.Set.AutoRedraw_Cities)
            {
                DB.Reset_Single(MapItems.Cities);
            }
        }

        private void Station_valign_center(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Button).Tag.ToString(), out Int64 id);

            DB.Stations.Set_Station_Valign(id, StationItem.Valign.Center);

            if (DB.Set.AutoRedraw_Cities)
            {
                DB.Reset_Single(MapItems.Cities);
            }
        }

        private void Station_valign_top(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Button).Tag.ToString(), out Int64 id);

            DB.Stations.Set_Station_Valign(id, StationItem.Valign.Top);

            if (DB.Set.AutoRedraw_Cities)
            {
                DB.Reset_Single(MapItems.Cities);
            }
        }

        private void Station_halign_left(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Button).Tag.ToString(), out Int64 id);

            DB.Stations.Set_Station_Halign(id, StationItem.Halign.Left);

            if (DB.Set.AutoRedraw_Cities)
            {
                DB.Reset_Single(MapItems.Cities);
            }
        }

        private void Station_halign_center(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Button).Tag.ToString(), out Int64 id);

            DB.Stations.Set_Station_Halign(id, StationItem.Halign.Center);

            if (DB.Set.AutoRedraw_Cities)
            {
                DB.Reset_Single(MapItems.Cities);
            }
        }

        private void Station_halign_right(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Button).Tag.ToString(), out Int64 id);

            DB.Stations.Set_Station_Halign(id, StationItem.Halign.Right);

            if (DB.Set.AutoRedraw_Cities)
            {
                DB.Reset_Single(MapItems.Cities);
            }
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

            DB.Stations.Flip_Station_Bold(id);

            if (DB.Set.AutoRedraw_Cities)
            {
                DB.Reset_Single(MapItems.Cities);
            }
        }

        private void Station_Outline_Click(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Button).Tag.ToString(), out Int64 id);

            DB.Stations.Flip_Station_Outline(id);

            if (DB.Set.AutoRedraw_Cities)
            {
                DB.Reset_Single(MapItems.Cities);
            }
        }


        private void Station_EN_Click(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Button).Tag.ToString(), out Int64 id);

            DB.Stations.Flip_Station_EN(id);

            if (DB.Set.AutoRedraw_Cities)
            {
                DB.Reset_Single(MapItems.Cities);
            }
        }

        private void Station_Rotation_Click(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Button).Tag.ToString(), out Int64 id);

            DB.Stations.Flip_Station_Rotation(id);

            if (DB.Set.AutoRedraw_Cities)
            {
                DB.Reset_Single(MapItems.Cities);
            }
        }

        private void Station_HideName_Click(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Button).Tag.ToString(), out Int64 id);

            DB.Stations.Flip_Station_HideName(id);

            if (DB.Set.AutoRedraw_Cities)
            {
                DB.Reset_Single(MapItems.Cities);
            }
        }

        private void Station_OffsetXPlus_Click(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Button).Tag.ToString(), out Int64 id);

            DB.Stations.Adjust_Station_OffsetX(id, 1);

            if (DB.Set.AutoRedraw_Cities)
            {
                DB.Reset_Single(MapItems.Cities);
            }
        }
        private void Station_OffsetXMinus_Click(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Button).Tag.ToString(), out Int64 id);

            DB.Stations.Adjust_Station_OffsetX(id, -1);

            if (DB.Set.AutoRedraw_Cities)
            {
                DB.Reset_Single(MapItems.Cities);
            }
        }
        private void Station_OffsetYPlus_Click(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Button).Tag.ToString(), out Int64 id);

            DB.Stations.Adjust_Station_OffsetY(id, 1);

            if (DB.Set.AutoRedraw_Cities)
            {
                DB.Reset_Single(MapItems.Cities);
            }
        }
        private void Station_OffsetYMinus_Click(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Button).Tag.ToString(), out Int64 id);

            DB.Stations.Adjust_Station_OffsetY(id, -1);

            if (DB.Set.AutoRedraw_Cities)
            {
                DB.Reset_Single(MapItems.Cities);
            }
        }

        private void Station_Dot2_Click(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Button).Tag.ToString(), out Int64 id);

            DB.Stations.Set_Station_Dot(id, 2);

            if (DB.Set.AutoRedraw_Cities)
            {
                DB.Reset_Single(MapItems.Cities);
            }
        }

        private void Station_Dot3_Click(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Button).Tag.ToString(), out Int64 id);

            DB.Stations.Set_Station_Dot(id, 3);

            if (DB.Set.AutoRedraw_Cities)
            {
                DB.Reset_Single(MapItems.Cities);
            }
        }

        private void Station_Dot4_Click(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Button).Tag.ToString(), out Int64 id);

            DB.Stations.Set_Station_Dot(id, 4);

            if (DB.Set.AutoRedraw_Cities)
            {
                DB.Reset_Single(MapItems.Cities);
            }
        }

        private void Station_CopyStationName_Click(object sender, RoutedEventArgs e)
        {
            Int64.TryParse((sender as Hyperlink).Tag.ToString(), out Int64 id);

            string usename = DB.Stations.Get_Station_UseName(id);

            if (usename != null)
            {
                Clipboard.SetText(usename);
            }
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

        private void Click_OpenRecent(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            ContextMenu menu = btn.ContextMenu;

            menu.PlacementTarget = btn;
            menu.IsOpen = true;

            System.Diagnostics.Debug.WriteLine(System.AppDomain.CurrentDomain.BaseDirectory);
        }

        private void FileHistoryItem_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                TextBlock tb = sender as TextBlock;

                if (tb.Tag != null)
                {
                    DB.Load_Area(tb.Tag.ToString());
                }
            }
        }

        private void AddCountryColor_Click(object sender, RoutedEventArgs e)
        {
            DB.CountryColors.NewItem();
        }

        private void RemoveCountryColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            {
                Guid g = (Guid)(sender as Button).Tag;

                DB.CountryColors.RemoveItem(g);
            }
        }

        private void EditCountryColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            {
                Guid g = (Guid)(sender as Button).Tag;
                DB.CountryColors.SetEditInstance(g);
            }
        }


        private void AddBorderPatch_Click(object sender, RoutedEventArgs e)
        {
            DB.BorderPatch.NewItem();
        }

        private void RemoveBorderPatch_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            {
                Guid g = (Guid)(sender as Button).Tag;

                DB.BorderPatch.RemoveItem(g);
            }
        }

        private void EditBorderPatch_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            {
                Guid g = (Guid)(sender as Button).Tag;

                DB.BorderPatch.SetEditInstance(g);
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

                if (DB.Set.AutoRedraw_Labels)
                {
                    DB.Reset_Single(MapItems.Labels);
                }
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

            if (DB.Set.AutoRedraw_Labels)
            {
                DB.Reset_Single(MapItems.Labels);
            }
        }
        private void Label_Bold_Click(object sender, RoutedEventArgs e)
        {
            Guid g = (Guid)(sender as Button).Tag;

            DB.Labels.FlipBold(g);

            if (DB.Set.AutoRedraw_Labels)
            {
                DB.Reset_Single(MapItems.Labels);
            }
        }

        private void Label_Outline_Click(object sender, RoutedEventArgs e)
        {
            Guid g = (Guid)(sender as Button).Tag;

            DB.Labels.FlipOutline(g);

            if (DB.Set.AutoRedraw_Labels)
            {
                DB.Reset_Single(MapItems.Labels);
            }
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
                DB.Stations.Set_All_Deselected(StationItemType.Station);
            }
            else if (tagstr == "lightrail")
            {
                DB.Stations.Set_All_Deselected(StationItemType.Lightrail);
            }
            else if (tagstr == "site")
            {
                DB.Stations.Set_All_Deselected(StationItemType.Site);
            }
            else if (tagstr == "yard")
            {
                DB.Stations.Set_All_Deselected(StationItemType.Yard);
            }
            else if (tagstr == "building")
            {
                DB.Stations.Set_All_FromBuilding_Deselected();
            }
            else if (tagstr == "halt")
            {
                DB.Stations.Set_All_Deselected(StationItemType.Halt);
            }

            if (DB.Set.AutoRedraw_Cities)
            {
                DB.Reset_Single(MapItems.Cities);
            }
        }

    }
}
