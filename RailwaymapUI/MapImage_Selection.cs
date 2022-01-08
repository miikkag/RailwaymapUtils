using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class MapImage_Selection : MapImage
    {
        public void Draw(ZoomBorder zoomer, DrawSettings set)
        {
            if (gr != null)
            {
                gr.Clear(Color.Transparent);

                gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

                if ((zoomer.Selection_Point.X >= 0) && (zoomer.Selection_Point.Y >= 0))
                {
                    float x1 = (float)(zoomer.Selection_Point.X - (zoomer.Selection_Size / 2));
                    float y1 = (float)(zoomer.Selection_Point.Y - (zoomer.Selection_Size / 2));

                    gr.FillRectangle(new SolidBrush(set.Color_Selection_Area), x1, y1, (float)zoomer.Selection_Size, (float)zoomer.Selection_Size);

                    gr.DrawRectangle(new Pen(set.Color_Selection_Border, 1), x1, y1, (float)zoomer.Selection_Size, (float)zoomer.Selection_Size);
                }
            }
        }

        
    }
}
