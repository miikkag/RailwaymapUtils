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

        protected DirectBitmap Dbmp;
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

            //bmp = new Bitmap(w, h);
            Dbmp = new DirectBitmap(w, h);
            bmp = Dbmp.Bitmap;

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

        public DirectBitmap GetDBitmap()
        {
            return Dbmp;
        }

        public static ImageSource BitmapToImageSource(Bitmap bmp)
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

        protected void Debug_Way_EndCoordinates(List<List<Coordinate>> ws, System.Drawing.Color color, BoundsXY bounds)
        {
            for (int w = 0; w < ws.Count; w++)
            {
                if (ws[w].Count > 1)
                {
                    double y1 = bounds.Scale * (bounds.Y_max - Commons.Merc_Y(ws[w][0].Latitude));
                    double x1 = bounds.Scale * (Commons.Merc_X(ws[w][0].Longitude) - bounds.X_min);

                    double y2 = bounds.Scale * (bounds.Y_max - Commons.Merc_Y(ws[w].Last().Latitude));
                    double x2 = bounds.Scale * (Commons.Merc_X(ws[w].Last().Longitude) - bounds.X_min);

                    DrawLine.Draw_Line(new System.Drawing.Point((int)x1, (int)y1), new System.Drawing.Point((int)x2, (int)y2), Dbmp, color, 1);
                }
                else
                {
                    Console.Write("coordset " + w.ToString() + " length=" + ws[w].Count.ToString());
                }
            }
        }


        protected void Draw_Way_Coordinates(List<List<Coordinate>> ws, ProgressInfo progress, double filter_px, int thickness, System.Drawing.Color color, BoundsXY bounds, bool filter_drawline)
        {
            DateTime prev_update = DateTime.Now;

            for (int w = 0; w < ws.Count; w++)
            {
                if (progress != null)
                {
                    DateTime now = DateTime.Now;

                    if ((now - prev_update).TotalMilliseconds >= Commons.PROGRESS_INTERVAL)
                    {
                        int percent = ((w * 100) / ws.Count);
                        progress.Set_Info(percent);

                        prev_update = now;
                    }
                }


                List<System.Drawing.Point> set_lines = new List<System.Drawing.Point>();

                double prev_x = double.MinValue;
                double prev_y = double.MinValue;

                int lenc = ws[w].Count;

                for (int c = 0; c < lenc; c++)
                {
                    double y = bounds.Scale * (bounds.Y_max - Commons.Merc_Y(ws[w][c].Latitude));
                    double x = bounds.Scale * (Commons.Merc_X(ws[w][c].Longitude) - bounds.X_min);

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
                        DrawLine.Draw_Line(set_lines[l - 1], set_lines[l], Dbmp, color, thickness);
                    }
                }
            }
        }
    }
}
