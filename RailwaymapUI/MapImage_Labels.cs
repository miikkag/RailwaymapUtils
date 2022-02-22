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
        public void Draw(List<LabelCoordinate> labels, Color color, Color color_outline, BoundsXY bxy)
        {
            gr.Clear(Color.Transparent);

            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            gr.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;


            const int colorval_transparent = 0;
            int colorval_text = color.ToArgb();

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

                Font usefont = new Font(l.FontName, (float)l.FontSize, usestyle);

                string draw_name = l.Name.Replace(@"\n", Environment.NewLine);

                double y = bxy.Scale * (bxy.Y_max - Commons.Merc_Y(l.Latitude));
                double x = bxy.Scale * (Commons.Merc_X(l.Longitude) - bxy.X_min);

                SizeF textsize = gr.MeasureString(draw_name, usefont);

                double offset_x = (textsize.Width / 2) * -1;
                double offset_y = (textsize.Height / 2) * -1;

                int usex = (int)(x + offset_x);
                int usey = (int)(y + offset_y);

                int endx = usex + (int)textsize.Width + 2;
                int endy = usey + (int)textsize.Height + 2;

                gr.DrawString(draw_name, usefont, usebrush, usex, usey);

                usefont.Dispose();


                if (l.Outline)
                {
                    for (int dy = usey; dy < endy; dy++)
                    {
                        for (int dx = usex; dx < endx; dx++)
                        {
                            if ((dx >= 0) && (dx < bmp.Width) && (dy >= 0) && (dy < bmp.Height))
                            {
                                if (Dbmp.GetPixel(dx, dy).ToArgb() == colorval_text)
                                {
                                    if ((dx > usex) && (dx > 0) && (Dbmp.GetPixel(dx - 1, dy).ToArgb() == colorval_transparent))
                                    {
                                        Dbmp.SetPixel(dx - 1, dy, color_outline);
                                    }

                                    if ((dy > usey) && (dy > 0) && (Dbmp.GetPixel(dx, dy - 1).ToArgb() == colorval_transparent))
                                    {
                                        Dbmp.SetPixel(dx, dy - 1, color_outline);
                                    }

                                    if (((dx + 1) < endx) && (Dbmp.GetPixel(dx + 1, dy).ToArgb() == colorval_transparent))
                                    {
                                        Dbmp.SetPixel(dx + 1, dy, color_outline);
                                    }

                                    if (((dy + 1) < endy) && (Dbmp.GetPixel(dx, dy + 1).ToArgb() == colorval_transparent))
                                    {
                                        Dbmp.SetPixel(dx, dy + 1, color_outline);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
