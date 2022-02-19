using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class MapImage_Labels : MapImage
    {
        public void Draw(List<LabelCoordinate> labels, Color color, BoundsXY bxy)
        {
            gr.Clear(Color.Transparent);

            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            gr.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;


            Brush usebrush = new SolidBrush(color);

            foreach (LabelCoordinate l in labels)
            {
                FontStyle usestyle;

                if (l.FontBold)
                {
                    usestyle = FontStyle.Bold;
                }
                else
                {
                    usestyle = FontStyle.Regular;
                }


                using (Font usefont = new Font(l.FontName, (float)l.FontSize, usestyle))
                {
                    double y = bxy.Scale * (bxy.Y_max - Commons.Merc_Y(l.Latitude));
                    double x = bxy.Scale * (Commons.Merc_X(l.Longitude) - bxy.X_min);

                    SizeF textsize = gr.MeasureString(l.Name, usefont);

                    double offset_x = (textsize.Width / 2) * -1;
                    double offset_y = (textsize.Height / 2) * -1;

                    gr.DrawString(l.Name, usefont, usebrush, (float)(x + offset_x), (float)(y + offset_y));


                }
            }
        }
    }
}
