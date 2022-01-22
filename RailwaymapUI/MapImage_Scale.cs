using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class MapImage_Scale : MapImage
    {
        public void Draw(BoundsXY bounds, Bounds bounds_coord, DrawSettings set)
        {
            gr.Clear(Color.Transparent);

            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            gr.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;

            const int bar_major_h = 6;
            const int bar_minor_h = 3;
            const int bar_mrg = 2;

            double lon_middle = bounds_coord.Lon_min + ((bounds_coord.Lon_max - bounds_coord.Lon_min) / 2);
            double lat_middle = bounds_coord.Lat_min + ((bounds_coord.Lat_max - bounds_coord.Lat_min) / 2);

            double lon_use = lon_middle + 1.0;
            double dist_1_degree = Commons.Haversine_km(new Coordinate(lat_middle, lon_middle), new Coordinate(lat_middle, lon_use));

            double target_degree = set.Scale_Km / dist_1_degree;
            lon_use = lon_middle + target_degree;

            double pixelwidth = Math.Abs(bounds.Scale * (Commons.Merc_X(lon_middle) - Commons.Merc_X(lon_use)));

            int startx = -1;
            int starty = -1;

            string start_str = "0";
            string end_str = set.Scale_Km.ToString();
            string mid_str = "km";

            Font usefont = new Font(set.Scale_FontName, (float)set.Scale_FontSize, FontStyle.Regular);

            SizeF start_str_size = gr.MeasureString(start_str, usefont);
            SizeF end_str_size = gr.MeasureString(end_str, usefont);
            SizeF mid_str_size = gr.MeasureString(mid_str, usefont);

            switch (set.Scale_Position)
            {
                case DrawSettings.ScalePosition.LeftTop:
                    startx = set.Scale_MarginX;
                    starty = set.Scale_MarginY;
                    break;

                case DrawSettings.ScalePosition.LeftBottom:
                    startx = set.Scale_MarginX;
                    starty = bmp.Height - (set.Scale_MarginY + (int)end_str_size.Height + bar_major_h + bar_mrg);
                    break;

                case DrawSettings.ScalePosition.RightTop:
                    startx = bmp.Width - (set.Scale_MarginX + (int)pixelwidth);
                    starty = set.Scale_MarginY;
                    break;

                case DrawSettings.ScalePosition.RightBottom:
                    startx = bmp.Width - (set.Scale_MarginX + (int)pixelwidth);
                    starty = bmp.Height - (set.Scale_MarginY + (int)end_str_size.Height + bar_major_h + bar_mrg);
                    break;

                default:
                    // Don't draw
                    break;
            }

            if ((startx >= 0) && (starty >= 0))
            {
                Pen usepen = new Pen(Brushes.Black, 1);
                Brush usebrush = Brushes.Black;

                gr.DrawLine(usepen, startx, starty + bar_major_h, startx + (int)pixelwidth, starty + bar_major_h);
                gr.DrawLine(usepen, startx, starty, startx, starty + bar_major_h);
                gr.DrawLine(usepen, startx + (int)pixelwidth, starty, startx + (int)pixelwidth, starty + bar_major_h);
                gr.DrawLine(usepen, startx + (int)(pixelwidth / 2), starty + (bar_major_h - bar_minor_h), startx + (int)(pixelwidth / 2), starty + bar_major_h);

                int textx;
                int texty;

                textx = startx - (int)(start_str_size.Width / 2);
                texty = starty + bar_major_h + bar_mrg;

                gr.DrawString(start_str, usefont, usebrush, (float)textx, (float)texty);

                textx = (startx + (int)pixelwidth) - (int)(end_str_size.Width / 2);
                gr.DrawString(end_str, usefont, usebrush, (float)textx, (float)texty);

                textx = (startx + (int)(pixelwidth / 2)) - (int)(mid_str_size.Width / 2);
                gr.DrawString(mid_str, usefont, usebrush, (float)textx, (float)texty);
            }
        }
    }
}
