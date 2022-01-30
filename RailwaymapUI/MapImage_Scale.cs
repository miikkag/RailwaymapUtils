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
        private readonly int SCALE_H = 20;
        private readonly int SPACER = 15;
        private readonly int MARGIN = 8;
        private readonly int ITEMH = 16;
        private readonly int SAMPLEW = 25;
        private readonly int SAMPLESPACE = 6;

        public void Draw(BoundsXY bounds, Bounds bounds_coord, RailwayLegend legend, DrawSettings set)
        {
            if (gr == null)
            {
                return;
            }

            gr.Clear(Color.Transparent);

            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            gr.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;

            int legend_h = 0;

            if (set.Legend_Enabled)
            {
                legend_h = Draw_Legend(legend, set);
            }

            if (set.Scale_Enabled)
            {
                Draw_Scale(bounds, bounds_coord, set, legend_h);
            }
        }

        private void Draw_Scale(BoundsXY bounds, Bounds bounds_coord, DrawSettings set, int legend_h)
        {
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

            if (set.Legend_Enabled && (set.Legend_Position == set.Scale_Position))
            {
                if ((set.Scale_Position == DrawSettings.ScalePosition.LeftTop) || (set.Scale_Position == DrawSettings.ScalePosition.RightTop))
                {
                    if (!set.Scale_On_Top)
                    {
                        starty += legend_h + SPACER;
                    }
                }
                else if ((set.Scale_Position == DrawSettings.ScalePosition.LeftBottom) || (set.Scale_Position == DrawSettings.ScalePosition.RightBottom))
                {
                    if (set.Scale_On_Top)
                    {
                        starty -= legend_h + SPACER;
                    }
                }
            }

            if ((startx >= 0) && (starty >= 0))
            {
                Pen usepen = new Pen(Brushes.Black, 1);
                Brush usebrush = Brushes.Black;

                gr.DrawLine(usepen, startx, starty + bar_major_h, startx + (int)pixelwidth, starty + bar_major_h);
                gr.DrawLine(usepen, startx, starty, startx, starty + bar_major_h);
                gr.DrawLine(usepen, startx + (int)pixelwidth, starty, startx + (int)pixelwidth, starty + bar_major_h);

                int use_sections = set.Scale_Sections;

                if (use_sections <= 0)
                {
                    use_sections = 2;
                }

                double section_w = pixelwidth / use_sections;

                double usex = startx;

                for (int i = 0; i < use_sections; i++)
                {
                    gr.DrawLine(usepen, (int)Math.Round(usex), starty + (bar_major_h - bar_minor_h), (int)Math.Round(usex), starty + bar_major_h);

                    usex += section_w;
                }

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


        private int Draw_Legend(RailwayLegend legend, DrawSettings set)
        {
            Brush brush_text = Brushes.Black;
            Font font_text = new Font(set.Legend_FontName, (float)set.Legend_FontSize, FontStyle.Regular);

            List<RailwayType> used_normal = legend.Get_Used_Types(true);
            List<RailwayType> used_narrow = legend.Get_Used_Types(false);

            // Calculate height
            int h = 0;

            if (used_normal.Count > 0)
            {
                h = ITEMH; // header
                h += (used_normal.Count * ITEMH);
            }

            if (used_narrow.Count > 0)
            {
                h += ITEMH; // header
                h += (used_narrow.Count * ITEMH);
            }

            h += (MARGIN * 2);

            // Calculate width
            float maxw = 0;

            foreach (RailwayType t in used_normal)
            {
                float itemw = gr.MeasureString(legend.Get_Type_Description(t), font_text).Width;

                maxw= Math.Max(maxw, itemw);
            }

            foreach (RailwayType t in used_narrow)
            {
                float itemw = gr.MeasureString(legend.Get_Type_Description(t), font_text).Width;

                maxw = Math.Max(maxw, itemw);
            }

            int w = (int)Math.Ceiling(maxw) + (3 * MARGIN) + (2 * SAMPLEW) + ( 2 * SAMPLESPACE );

            int basex;
            int basey;

            if (set.Legend_Position == DrawSettings.ScalePosition.LeftTop)
            {
                basex = set.Scale_MarginX;
                basey = set.Scale_MarginY;
            }
            else if (set.Legend_Position == DrawSettings.ScalePosition.LeftBottom)
            {
                basex = set.Scale_MarginX;
                basey = bmp.Height - (set.Scale_MarginY + h);
            }
            else if (set.Legend_Position == DrawSettings.ScalePosition.RightTop)
            {
                basex = bmp.Width - (set.Scale_MarginX + w);
                basey = set.Scale_MarginY;
            }
            else if (set.Legend_Position == DrawSettings.ScalePosition.RightBottom)
            {
                basex = bmp.Width - (set.Scale_MarginX + w);
                basey = bmp.Height - (set.Scale_MarginY + h);
            }
            else
            {
                basex = -1;
                basey = -1;
            }

            if ((set.Scale_Enabled) && (set.Scale_Position == set.Legend_Position))
            {
                if ((set.Legend_Position == DrawSettings.ScalePosition.LeftTop) || (set.Legend_Position == DrawSettings.ScalePosition.RightTop))
                {
                    if (set.Scale_On_Top)
                    {
                        basey += (SCALE_H + SPACER);
                    }
                }
                else if ((set.Legend_Position == DrawSettings.ScalePosition.LeftBottom) || (set.Legend_Position == DrawSettings.ScalePosition.RightBottom))
                {
                    if (!set.Scale_On_Top)
                    {
                        basey -= (SCALE_H + SPACER);
                    }
                }
                else
                {
                    // Do Nothing
                }
            }

            if ((basex >= 0) && (basey >= 0))
            {
                gr.FillRectangle(Brushes.White, basex, basey, w, h);
                gr.DrawRectangle(Pens.Black, basex, basey, w, h);

                int usey = basey + MARGIN;
                int usex = basex + MARGIN;

                if (used_normal.Count > 0)
                {
                    usey += Draw_Legend_ListPart(usex, usey, RailwayLegend.HEADER_NORMAL, used_normal, legend, set, font_text, brush_text);
                }

                if (used_narrow.Count > 0)
                {
                    _ = Draw_Legend_ListPart(usex, usey, RailwayLegend.HEADER_NARROW, used_narrow, legend, set, font_text, brush_text);
                }

            }

            return h;
        }

        private int Draw_Legend_ListPart(int x, int y, string header, List<RailwayType> items, RailwayLegend legend, DrawSettings set, Font usefont, Brush usebrush)
        {
            int usex = x;
            int usey = y;

            gr.DrawString(header, usefont, usebrush, usex, usey);

            usey += ITEMH;

            foreach (RailwayType t in items)
            {
                usex = x + 2;

                Color usecolor = Commons.Get_Draw_Color(t, set);

                int centery = usey + ((ITEMH / 2) - 1);

                gr.DrawLine(new Pen(usecolor, 3), usex, centery, usex + SAMPLEW, centery);
                usex += (SAMPLEW + SAMPLESPACE);

                gr.DrawLine(new Pen(usecolor, 1), usex, centery, usex + SAMPLEW, centery);
                usex += (SAMPLEW + SAMPLESPACE);

                gr.DrawString(legend.Get_Type_Description(t), usefont, usebrush, usex, usey);
                usey += ITEMH;
            }

            return (usey - y);
        }
    }
}
