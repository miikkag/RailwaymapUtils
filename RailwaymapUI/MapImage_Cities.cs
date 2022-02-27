using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Drawing2D;
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

                int color_transparent = 0;
                int color_text = set.Color_Cities_Name.ToArgb();

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

                        int outline_offset_x;
                        int outline_offset_y;

                        string draw_name = stations[i].display_name.Replace(@"\n", Environment.NewLine);

                        SizeF textsize = gr.MeasureString(draw_name, usefont);

                        if (stations[i].Halign_Left)
                        {
                            if (stations[i].rotation == 1)
                            {
                                // Rotate 90 left
                                base_offset_x = textsize.Height * -1;
                                outline_offset_x = (int)base_offset_x;
                            }
                            else if (stations[i].rotation == 2)
                            {
                                // Rotate 90 right
                                base_offset_x = 0;
                                outline_offset_x = (int)textsize.Height * -1;
                            }
                            else
                            {
                                base_offset_x = textsize.Width * -1;
                                outline_offset_x = (int)base_offset_x;
                            }
                        }
                        else if (stations[i].Halign_Right)
                        {
                            if (stations[i].rotation == 1)
                            {
                                // Rotate 90 left
                                base_offset_x = 0;
                                outline_offset_x = 0;
                            }
                            else if (stations[i].rotation == 2)
                            {
                                // Rotate 90 right
                                base_offset_x = textsize.Height;
                                outline_offset_x = 0;
                            }
                            else
                            {
                                base_offset_x = 0;
                                outline_offset_x = 0;
                            }
                        }
                        else
                        {
                            if (stations[i].rotation == 1)
                            {
                                // Rotate 90 left
                                base_offset_x = (textsize.Height / 2) * -1;
                                outline_offset_x = (int)base_offset_x;
                            }
                            else if (stations[i].rotation == 2)
                            {
                                // Rotate 90 right
                                base_offset_x = (textsize.Height / 2);
                                outline_offset_x = (int)base_offset_x * -1;
                            }
                            else
                            {
                                base_offset_x = (textsize.Width / 2) * -1;
                                outline_offset_x = (int)base_offset_x;
                            }
                        }

                        if (stations[i].Valign_Bottom)
                        {
                            if (stations[i].rotation == 1)
                            {
                                // Rotate 90 left
                                base_offset_y = textsize.Width;
                                outline_offset_y = 0;
                            }
                            else if (stations[i].rotation == 2)
                            {
                                // Rotate 90 right
                                base_offset_y = 0;
                                outline_offset_y = 0;
                            }
                            else
                            {
                                base_offset_y = 0;
                                outline_offset_y = 0;
                            }
                        }
                        else if (stations[i].Valign_Top)
                        {
                            if (stations[i].rotation == 1)
                            {
                                // Rotate 90 left
                                base_offset_y = 0;
                                outline_offset_y = (int)textsize.Width * -1;
                            }
                            else if (stations[i].rotation == 2)
                            {
                                // Rotate 90 right
                                base_offset_y = textsize.Width * -1;
                                outline_offset_y = (int)base_offset_y;
                            }
                            else
                            {
                                base_offset_y = (textsize.Height) * -1;
                                outline_offset_y = (int)base_offset_y;
                            }
                        }
                        else
                        {
                            if (stations[i].rotation == 1)
                            {
                                // Rotate 90 left
                                base_offset_y = (textsize.Width / 2);
                                outline_offset_y = (int)base_offset_y * -1;
                            }
                            else if (stations[i].rotation == 2)
                            {
                                // Rotate 90 right
                                base_offset_y = (textsize.Width / 2) * -1;
                                outline_offset_y = (int)base_offset_y;
                            }
                            else
                            {
                                base_offset_y = (textsize.Height / 2) * -1;
                                outline_offset_y = (int)base_offset_y;
                            }
                        }

                        float offset_x = base_offset_x + stations[i].offsetx;
                        float offset_y = base_offset_y + stations[i].offsety;

                        int usex;
                        int usey;

                        if (stations[i].rotation == 1)
                        {
                            usex = 0;
                            usey = 0;

                            gr.RotateTransform(-90);
                            gr.TranslateTransform((float)x + offset_x, (float)y + offset_y, MatrixOrder.Append);
                        }
                        else if (stations[i].rotation == 2)
                        {
                            usex = 0;
                            usey = 0;

                            gr.RotateTransform(90);
                            gr.TranslateTransform((float)x + offset_x, (float)y + offset_y, MatrixOrder.Append);
                        }
                        else
                        {
                            usex = x + (int)offset_x;
                            usey = y + (int)offset_y;
                        }

                        gr.DrawString(draw_name, usefont, brush_font, (float)usex, (float)usey);

                        if (stations[i].rotation != 0)
                        {
                            gr.ResetTransform();
                        }

                        if (stations[i].outline)
                        {
                            int startx = (x + outline_offset_x + stations[i].offsetx) - 1;
                            int starty = (y + outline_offset_y + stations[i].offsety) - 1;

                            int endx;
                            int endy;

                            if (stations[i].rotation == 0)
                            {
                                endx = startx + (int)textsize.Width + 2;
                                endy = starty + (int)textsize.Height + 2;
                            }
                            else
                            {
                                endx = startx + (int)textsize.Height + 2;
                                endy = starty + (int)textsize.Width + 2;
                            }

                            for (int dy = starty; dy < endy; dy++)
                            {
                                for (int dx = startx; dx < endx; dx++)
                                {
                                    if ((dx >= 0) && (dx < bmp.Width) && (dy >= 0) && (dy < bmp.Height))
                                    {
                                        if (Dbmp.GetPixel(dx, dy).ToArgb() == color_text)
                                        {
                                            if ((dx > startx) && (dx > 0) && (Dbmp.GetPixel(dx - 1, dy).ToArgb() == color_transparent))
                                            {
                                                Dbmp.SetPixel(dx - 1, dy, set.Color_Cities_Outline);
                                            }

                                            if ((dy > starty) && (dy > 0) && (Dbmp.GetPixel(dx, dy - 1).ToArgb() == color_transparent))
                                            {
                                                Dbmp.SetPixel(dx, dy - 1, set.Color_Cities_Outline);
                                            }

                                            if (((dx + 1) < endx) && (Dbmp.GetPixel(dx + 1, dy).ToArgb() == color_transparent))
                                            {
                                                Dbmp.SetPixel(dx + 1, dy, set.Color_Cities_Outline);
                                            }

                                            if (((dy + 1) < endy) && (Dbmp.GetPixel(dx, dy + 1).ToArgb() == color_transparent))
                                            {
                                                Dbmp.SetPixel(dx, dy + 1, set.Color_Cities_Outline);
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
