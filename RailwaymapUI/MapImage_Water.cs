using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class MapImage_Water : MapImage
    {
        public void Draw(SQLiteConnection conn, BoundsXY bounds, ProgressInfo progress, DrawSettings set)
        {
            if ((gr != null) && (conn != null))
            {
                gr.Clear(Color.Transparent);

                gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

                WaySet ws_water = Commons.Make_Wayset(conn, "water");

                int use_singlenumber = -1;

                if (set.Debug_Water_SingleItem)
                {
                    use_singlenumber = set.Debug_Water_SingleItemNumber;
                }

                Draw_Way_Polygons(ws_water, progress, set.Filter_Water_Area, set.Filter_Water_Line, set.Color_Water, bounds,
                    set.Debug_Water_BorderOnly,
                    set.Debug_Water_ID,
                    use_singlenumber);

                if (set.Draw_WaterLand)
                {
                    WaySet ws_waterland = Commons.Make_Wayset(conn, "waterland");

                    //Draw_Way_Coordinates(ws_waterland, progress, set.Filter_WaterLand_Line, false, Color.Red, bounds, false);
                    //Debug_Way_EndCoordinates(ws_waterland, Color.Red, bounds);

                    Draw_Way_Polygons(ws_waterland, progress, set.Filter_WaterLand_Area, set.Filter_WaterLand_Line, set.Color_Land, bounds,
                        set.Debug_Water_BorderOnly, set.Debug_Water_ID, use_singlenumber);
                }
            }
        }

    }
}
