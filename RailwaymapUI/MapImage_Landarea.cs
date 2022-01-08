using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class MapImage_Landarea : MapImage
    {
        public void Draw(SQLiteConnection conn, BoundsXY bounds, ProgressInfo progress, DrawSettings set)
        {
            if ((gr != null) && (conn != null))
            {
                gr.Clear(Color.Transparent);

                gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            }
        }
    }
}
