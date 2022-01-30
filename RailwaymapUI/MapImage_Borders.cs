using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Data.SQLite;

namespace RailwaymapUI
{
    public class MapImage_Borders : MapImage
    {
        public void Draw(SQLiteConnection conn_border, BoundsXY bounds, ProgressInfo progress, DrawSettings set)
        {
            if ((gr != null)&&(conn_border!=null))
            {
                gr.Clear(Color.Transparent);

                gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

                List<List<Coordinate>> ws = Commons.Generate_Wayset(conn_border);

                Draw_Way_Coordinates(ws, progress, set.Filter_Border_Line, false, set.Color_Border_Land, bounds,
                    set.Filter_Border_DrawLine);
            }
        }
    }
}
