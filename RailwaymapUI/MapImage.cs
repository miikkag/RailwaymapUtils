using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace RailwaymapUI
{
    public class MapImage : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ImageSource ImageSrc { get { return BitmapToImageSource(bmp); } }

        public double Image_Width { get { if (ImageSrc != null) return ImageSrc.Width; else return 0; } }
        public double Image_Height { get { if (ImageSrc != null) return ImageSrc.Height; else return 0; } }

        protected bool _enabled;
        public bool Enabled { get { return _enabled; } set { _enabled = value; OnPropertyChanged("Visible"); } }

        public Visibility Visible { get { if (Enabled) return Visibility.Visible; else return Visibility.Hidden; } }

        protected Bitmap bmp;
        protected Graphics gr;

        protected int w, h;

        public MapImage()
        {
            w = 0;
            h = 0;
            Enabled = true;
        }

        public void Set_Size(int width, int height)
        {
            w = width;
            h = height;

            if (gr != null)
            {
                gr.Dispose();
            }

            if (bmp != null)
            {
                bmp.Dispose();
            }

            bmp = new Bitmap(w, h);

            gr = Graphics.FromImage(bmp);

            OnPropertyChanged("ImageSrc");
            OnPropertyChanged("Image_Width");
            OnPropertyChanged("Image_Height");
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        public Bitmap GetBitmap()
        {
            return bmp;
        }

        private ImageSource BitmapToImageSource(Bitmap bmp)
        {
            if (bmp != null)
            {
                var hOldBitmap = bmp.GetHbitmap(System.Drawing.Color.Transparent);
                var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                     hOldBitmap,
                     IntPtr.Zero,
                     new Int32Rect(0, 0, bmp.Width, bmp.Height),
                     null);
                DeleteObject(hOldBitmap);

                return bitmapSource;
            }
            else
            {
                return null;
            }
        }

        protected void Debug_Way_EndCoordinates(WaySet ws, System.Drawing.Color color, BoundsXY bounds)
        {
            for (int w = 0; w < ws.WayCoordSets.Length; w++)
            {
                if (ws.WayCoordSets[w].Coords.Length > 1)
                {
                    double y1 = bounds.Scale * (bounds.Y_max - Commons.Merc_Y(ws.WayCoordSets[w].Coords[0].Latitude));
                    double x1 = bounds.Scale * (Commons.Merc_X(ws.WayCoordSets[w].Coords[0].Longitude) - bounds.X_min);

                    double y2 = bounds.Scale * (bounds.Y_max - Commons.Merc_Y(ws.WayCoordSets[w].Coords.Last().Latitude));
                    double x2 = bounds.Scale * (Commons.Merc_X(ws.WayCoordSets[w].Coords.Last().Longitude) - bounds.X_min);

                    DrawLine.Draw_Line_1px(new System.Drawing.Point((int)x1, (int)y1), new System.Drawing.Point((int)x2, (int)y2), bmp, color);
                }
                else
                {
                    Console.Write("coordset " + w.ToString() + " length=" + ws.WayCoordSets[w].Coords.Length.ToString());
                }
            }
        }

        protected void Draw_Way_Coordinates(WaySet ws, ProgressInfo progress, double filter_px, bool thick, System.Drawing.Color color, BoundsXY bounds, bool filter_drawline)
        {
            DateTime prev_update = DateTime.Now;

            for (int w = 0; w < ws.WayCoordSets.Length; w++)
            {
                if (progress != null)
                {
                    DateTime now = DateTime.Now;

                    if ((now - prev_update).TotalMilliseconds >= Commons.PROGRESS_INTERVAL)
                    {
                        int percent = ((w * 100) / ws.WayCoordSets.Length);
                        progress.Set_Info(percent);

                        prev_update = now;
                    }
                }


                List<System.Drawing.Point> set_lines = new List<System.Drawing.Point>();

                double prev_x = double.MinValue;
                double prev_y = double.MinValue;

                int lenc = ws.WayCoordSets[w].Coords.Length;

                for (int c = 0; c < lenc; c++)
                {
                    double y = bounds.Scale * (bounds.Y_max - Commons.Merc_Y(ws.WayCoordSets[w].Coords[c].Latitude));
                    double x = bounds.Scale * (Commons.Merc_X(ws.WayCoordSets[w].Coords[c].Longitude) - bounds.X_min);

                    // Filter lines too close to each other
                    double dist = Math.Abs(x - prev_x) + Math.Abs(y - prev_y);

                    if ((dist > filter_px) || (c == (lenc - 1)))
                    {
                        set_lines.Add(new System.Drawing.Point((int)x, (int)y));
                        prev_x = x;
                        prev_y = y;
                    }
                }

                if (set_lines.Count > 1)
                {
                    BoundsPoint filter_bpt = new BoundsPoint();

                    foreach (System.Drawing.Point pt in set_lines)
                    {
                        filter_bpt.TryXY(pt);
                    }

                    int bpt_w = filter_bpt.X_max - filter_bpt.X_min;
                    int bpt_h = filter_bpt.Y_max - filter_bpt.Y_min;
                    int bpt = Math.Max(bpt_w, bpt_h);

                    // Draw line if no draw filtering is selected OR wayset size is more than 1 px
                    bool draw_this = (!filter_drawline) || (bpt > 0);

                    //System.Drawing.Pen usepen = new System.Drawing.Pen(color, 1);

                    for (int l = 1; l < set_lines.Count; l++)
                    {
                        if (thick)
                        {
                            DrawLine.Draw_Line_3px(set_lines[l - 1], set_lines[l], bmp, color);
                        }
                        else
                        {
                            if (draw_this)
                            {
                                DrawLine.Draw_Line_1px(set_lines[l - 1], set_lines[l], bmp, color);
                            }
                        }
                    }
                }
            }
        }

        protected void Draw_Way_Polygons(WaySet ws, ProgressInfo progress, double areafilter_px, double linefilter_px, System.Drawing.Color fillcolor, BoundsXY bounds, bool border_only, bool draw_id, int single_itemnumber)
        {
            DateTime prev_update = DateTime.Now;


            for (int w = 0; w < ws.WayCoordSets.Length; w++)
            {
                // Debug feature to draw only single item
                if ((single_itemnumber >= 0) && (single_itemnumber != w))
                {
                    continue;
                }

                DateTime now = DateTime.Now;

                if ((now - prev_update).TotalMilliseconds >= Commons.PROGRESS_INTERVAL)
                {
                    int percent = ((w * 100) / ws.WayCoordSets.Length);
                    progress.Set_Info(percent);

                    prev_update = now;
                }

                List<System.Drawing.Point> set_lines = new List<System.Drawing.Point>();

                double prev_x = double.MinValue;
                double prev_y = double.MinValue;

                int lenc = ws.WayCoordSets[w].Coords.Length;

                BoundsXY bxy = new BoundsXY();

                for (int c = 0; c < lenc; c++)
                {
                    double y = bounds.Scale * (bounds.Y_max - Commons.Merc_Y(ws.WayCoordSets[w].Coords[c].Latitude));
                    double x = bounds.Scale * (Commons.Merc_X(ws.WayCoordSets[w].Coords[c].Longitude) - bounds.X_min);

                    // Filter lines too close to each other
                    double dist = Math.Abs(x - prev_x) + Math.Abs(y - prev_y);

                    if ((dist > linefilter_px) || (c == (lenc - 1)))
                    {
                        set_lines.Add(new System.Drawing.Point((int)x, (int)y));
                        prev_x = x;
                        prev_y = y;

                        bxy.TryXY(x, y);
                    }
                }

                if (set_lines.Count > 2)
                {
                    double areasize = Math.Max(bxy.X_max - bxy.X_min, bxy.Y_max - bxy.Y_min);

                    if (areasize > areafilter_px)
                    {
                        if (border_only)
                        {
                            gr.DrawPolygon(new System.Drawing.Pen(fillcolor, 1), set_lines.ToArray());
                        }
                        else
                        {
                            gr.FillPolygon(new SolidBrush(fillcolor), set_lines.ToArray());
                        }

                        if (draw_id)
                        {
                            double centerx = bxy.X_min + ((bxy.X_max - bxy.X_min) / 2);
                            double centery = bxy.Y_min + ((bxy.Y_max - bxy.Y_min) / 2);

                            Font usefont = new Font("Arial", 8);

                            gr.DrawString(w.ToString(), usefont, System.Drawing.Brushes.SteelBlue, (float)centerx, (float)centery);
                        }
                    }
                }
            }
        }
    }
}
