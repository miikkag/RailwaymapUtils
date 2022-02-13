using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class MapImage_Cities : MapImage
    {
        public void Draw(List<StationItem> stations, BoundsXY bounds, ProgressInfo progress, DrawSettings set)
        {
            if (gr != null)
            {
                gr.Clear(Color.Transparent);

                gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                gr.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;


                DateTime prev_update = DateTime.Now;

                int stations_len = stations.Count;

                Brush brush_pt = new SolidBrush(set.Color_Cities_Point);
                Brush brush_font = new SolidBrush(set.Color_Cities_Name);

                Font font_normal = new Font(set.FontName_Cities, (float)set.FontSize_Cities, FontStyle.Regular);
                Font font_bold = new Font(set.FontNameBold_Cities, (float)set.FontSizeBold_Cities, FontStyle.Bold);


                for (int i = 0; i < stations_len; i++)
                {
                    DateTime now = DateTime.Now;

                    if ((now - prev_update).TotalMilliseconds >= Commons.PROGRESS_INTERVAL)
                    {
                        int percent = (i * 30) / stations_len;
                        progress.Set_Info(percent);

                        prev_update = now;
                    }

                    Font usefont;

                    if (stations[i].bold)
                    {
                        usefont = font_bold;
                    }
                    else
                    {
                        usefont = font_normal;
                    }

                    double yf = bounds.Scale * (bounds.Y_max - Commons.Merc_Y(stations[i].Coord.Latitude));
                    double xf = bounds.Scale * (Commons.Merc_X(stations[i].Coord.Longitude) - bounds.X_min);

                    int x = (int)xf;
                    int y = (int)yf;

                    int xdot = x - (stations[i].dotsize / 2);
                    int ydot = y - (stations[i].dotsize / 2);

                    stations[i].Set_CoordinatesXY(x, y);

                    if (stations[i].visible)
                    {
                        gr.FillRectangle(brush_pt, xdot, ydot, stations[i].dotsize, stations[i].dotsize);

                        float base_offset_x;
                        float base_offset_y;

                        string draw_name = stations[i].display_name.Replace(@"\n", Environment.NewLine);

                        SizeF textsize = gr.MeasureString(draw_name, usefont);

                        if (stations[i].Halign_Left)
                        {
                            base_offset_x = textsize.Width * -1;
                        }
                        else if (stations[i].Halign_Right)
                        {
                            base_offset_x = 0;
                        }
                        else
                        {
                            base_offset_x = (textsize.Width / 2) * -1;
                        }

                        if (stations[i].Valign_Bottom)
                        {
                            base_offset_y = 0;
                        }
                        else if (stations[i].Valign_Top)
                        {
                            base_offset_y = (textsize.Height) * -1;
                        }
                        else
                        {
                            base_offset_y = (textsize.Height / 2) * -1;
                        }

                        float offset_x = base_offset_x + stations[i].offsetx;
                        float offset_y = base_offset_y + stations[i].offsety;

                        gr.DrawString(draw_name, usefont, brush_font, (float)x + offset_x, (float)y + offset_y);

                        int usex = (x + (int)offset_x) - 1;
                        int usey = (y + (int)offset_y) - 1;

                        int endx = usex + (int)textsize.Width + 2;
                        int endy = usey + (int)textsize.Height + 2;

                        int color_transparent = 0;
                        int color_text = set.Color_Cities_Name.ToArgb();

                        if (stations[i].outline)
                        {
                            for (int dy = usey; dy < endy; dy++)
                            {
                                for (int dx = usex; dx < endx; dx++)
                                {
                                    if ((dx >= 0) && (dx < bmp.Width) && (dy >= 0) && (dy < bmp.Height))
                                    {
                                        if (bmp.GetPixel(dx, dy).ToArgb() == color_text)
                                        {
                                            if ((dx > usex) && (dx > 0) && (bmp.GetPixel(dx - 1, dy).ToArgb() == color_transparent))
                                            {
                                                bmp.SetPixel(dx - 1, dy, set.Color_Cities_Outline);
                                            }

                                            if ((dy > usey) && (dy > 0) && (bmp.GetPixel(dx, dy - 1).ToArgb() == color_transparent))
                                            {
                                                bmp.SetPixel(dx, dy - 1, set.Color_Cities_Outline);
                                            }

                                            if (((dx + 1) < endx) && (bmp.GetPixel(dx + 1, dy).ToArgb() == color_transparent))
                                            {
                                                bmp.SetPixel(dx + 1, dy, set.Color_Cities_Outline);
                                            }

                                            if (((dy + 1) < endy) && (bmp.GetPixel(dx, dy + 1).ToArgb() == color_transparent))
                                            {
                                                bmp.SetPixel(dx, dy + 1, set.Color_Cities_Outline);
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

    }
}
