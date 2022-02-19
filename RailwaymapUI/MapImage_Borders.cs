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
        public void Draw(SQLiteConnection conn_border, List<PatchLine> patches, BoundsXY bounds, ProgressInfo progress, DrawSettings set)
        {
            if ((gr != null)&&(conn_border!=null))
            {
                gr.Clear(Color.Transparent);

                gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

                if (set.Draw_Border)
                {
                    List<List<Coordinate>> ws = Commons.Generate_Wayset(conn_border);

                    if (set.Border_Outlined)
                    {
                        Draw_Way_Coordinates(ws, progress, set.Filter_Border_Line, 3, set.Color_Border_Outline, bounds, set.Filter_Border_DrawLine);
                        Draw_Way_Coordinates(ws, progress, set.Filter_Border_Line, 1, set.Color_Border, bounds, set.Filter_Border_DrawLine);
                    }
                    else
                    {
                        Draw_Way_Coordinates(ws, progress, set.Filter_Border_Line, set.Border_Thickness, set.Color_Border, bounds, set.Filter_Border_DrawLine);
                    }

                    foreach (PatchLine p in patches)
                    {
                        if (((p.Start.Latitude == 0) && (p.Start.Longitude == 0)) || ((p.End.Latitude == 0) && (p.End.Longitude == 0)))
                        {
                            continue;
                        }

                        int starty = (int)(bounds.Scale * (bounds.Y_max - Commons.Merc_Y(p.Start.Latitude)));
                        int startx = (int)(bounds.Scale * (Commons.Merc_X(p.Start.Longitude) - bounds.X_min));

                        int endy = (int)(bounds.Scale * (bounds.Y_max - Commons.Merc_Y(p.End.Latitude)));
                        int endx = (int)(bounds.Scale * (Commons.Merc_X(p.End.Longitude) - bounds.X_min));

                        DrawLine.Draw_Line(new Point(startx, starty), new Point(endx, endy), Dbmp, p.LineColor, set.Border_Thickness);
                    }
                }
            }
        }
    }
}
