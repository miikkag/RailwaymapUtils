using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class MapImage_CountryColors : MapImage
    {
        private int COLOR_TRANSPARENT;
        private int COLOR_LAND;

        public void Draw(BoundsXY bounds, Bounds bounds_coord, DrawSettings set, ProgressInfo progress, List<ColorCoordinate> coloritems, MapImage_Landarea landarea, MapImage_Lakes lakes, MapImage_Borders border)
        {
            if (gr == null)
            {
                return;
            }

            gr.Clear(Color.Transparent);

            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

            COLOR_LAND = set.Color_Land.ToArgb();
            COLOR_TRANSPARENT = 0;

            progress.Set_Info(true, "Coloring countries", 0);

            for(int c = 0; c < coloritems.Count; c++)
            {
                if ((coloritems[c].Latitude == 0) && (coloritems[c].Longitude == 0))
                {
                    continue;
                }

                int starty = (int)(bounds.Scale * (bounds.Y_max - Commons.Merc_Y(coloritems[c].Latitude)));
                int startx = (int)(bounds.Scale * (Commons.Merc_X(coloritems[c].Longitude) - bounds.X_min));

                Color usecolor = Color.FromArgb((int)(coloritems[c].ColorValue | 0xFF000000));

                progress.Set_Info((c * 100) / coloritems.Count);

                DirectBitmap dbmp_border = border.GetDBitmap();
                DirectBitmap dbmp_landarea = landarea.GetDBitmap();
                DirectBitmap dbmp_lakes = lakes.GetDBitmap();

                Queue<Point> q = new Queue<Point>();

                Point startpt = new Point(startx, starty);

                if((startpt.X >= 0) && (startpt.Y >= 0) && (startpt.X < bmp.Width) && (startpt.Y < bmp.Height))
                {
                    Dbmp.SetPixel(startpt.X, startpt.Y, usecolor);
                    q.Enqueue(startpt);
                }
                else
                {
                    System.Windows.MessageBox.Show("Country color " + coloritems[c].Name + " is out of bounds:" +
                        Environment.NewLine +
                        "Lat=" + coloritems[c].Latitude.ToString() + " Lon=" + coloritems[c].Longitude.ToString() +
                        Environment.NewLine +
                        "x=" + startpt.X.ToString() + " y=" + startpt.Y.ToString());
                }

                DateTime prev = DateTime.Now;

                while (q.Count > 0)
                {
                    Point pt = q.Dequeue();

                    if (Check_FillValidity(pt.X + 1, pt.Y, dbmp_landarea, dbmp_lakes, dbmp_border))
                    {
                        Dbmp.SetPixel(pt.X + 1, pt.Y, usecolor);
                        q.Enqueue(new Point(pt.X + 1, pt.Y));
                    }

                    if (Check_FillValidity(pt.X - 1, pt.Y, dbmp_landarea, dbmp_lakes, dbmp_border))
                    {
                        Dbmp.SetPixel(pt.X - 1, pt.Y, usecolor);
                        q.Enqueue(new Point(pt.X - 1, pt.Y));
                    }

                    if (Check_FillValidity(pt.X, pt.Y + 1, dbmp_landarea, dbmp_lakes, dbmp_border))
                    {
                        Dbmp.SetPixel(pt.X, pt.Y + 1, usecolor);
                        q.Enqueue(new Point(pt.X, pt.Y + 1));
                    }

                    if (Check_FillValidity(pt.X, pt.Y - 1, dbmp_landarea, dbmp_lakes, dbmp_border))
                    {
                        Dbmp.SetPixel(pt.X, pt.Y - 1, usecolor);
                        q.Enqueue(new Point(pt.X, pt.Y - 1));
                    }

                    if ((DateTime.Now - prev).TotalMilliseconds > 500)
                    {
                        progress.Set_Info(true, pt.ToString(), 0);

                        prev = DateTime.Now;
                    }
                }
            }
        }

        private bool Check_FillValidity(int x, int y, DirectBitmap dbmp_landarea, DirectBitmap dbmp_lakes, DirectBitmap dbmp_border)
        {
            bool result = false;

            if ((x < Dbmp.Width) && (y < Dbmp.Height) && (x >= 0) && (y >= 0))
            {
                if (Dbmp.GetPixelInt(x, y) == COLOR_TRANSPARENT)
                {
                    if (dbmp_landarea.GetPixelInt(x, y) == COLOR_LAND)
                    {
                        int lakes_pixel = dbmp_lakes.GetPixelInt(x, y);

                        if ((lakes_pixel == COLOR_TRANSPARENT) || (lakes_pixel == COLOR_LAND))
                        {
                            if (dbmp_border.GetPixelInt(x, y) == COLOR_TRANSPARENT)
                            {
                                result = true;
                            }
                        }
                    }
                }
            }

            return result;
        }
    }
}
