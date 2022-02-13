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
        private int COLOR_TRANSPARENT = 0;
        private int COLOR_LAND;

        public void Draw(BoundsXY bounds, Bounds bounds_coord, DrawSettings set, ProgressInfo progress, List<ColorCoordinate> coloritems, MapImage_Landarea landarea, MapImage_Borders border)
        {
            if (gr == null)
            {
                return;
            }

            gr.Clear(Color.Transparent);

            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

            COLOR_LAND = set.Color_Land.ToArgb();

            progress.Set_Info(true, "Coloring countries", 0);

            for(int c = 0; c < coloritems.Count; c++)
            {
                int startx = (int)(bounds.Scale * (bounds.Y_max - Commons.Merc_Y(coloritems[c].Latitude)));
                int starty = (int)(bounds.Scale * (Commons.Merc_X(coloritems[c].Longitude) - bounds.X_min));

                Color usecolor = Color.FromArgb((int)(coloritems[c].ColorValue | 0xFF000000));


                progress.Set_Info(((c + 1) * 100) / coloritems.Count);

                Queue<Point> q = new Queue<Point>();

                q.Enqueue(new Point(startx, starty));

                while (q.Count > 0)
                {
                    Point pt = q.Dequeue();

#if !DEBUG
                    if (Check_FillValidity(pt.X, pt.Y, landarea, border))
                    {
#endif
                        bmp.SetPixel(pt.X, pt.Y, usecolor);
#if !DEBUG
                    }
                    else
                    {
                        throw new Exception("Country color " + coloritems[c].Name + " is out of bounds:" +
                            Environment.NewLine +
                            "Lat=" + coloritems[c].Latitude.ToString() + " Lon=" + coloritems[c].Longitude.ToString() +
                            Environment.NewLine +
                            "x=" + pt.X.ToString() + " y=" + pt.Y.ToString());
                    }
#endif
                    if (Check_FillValidity(pt.X + 1, pt.Y, landarea, border))
                    {
                        q.Enqueue(new Point(pt.X + 1, pt.Y));
                    }

                    if (Check_FillValidity(pt.X - 1, pt.Y, landarea, border))
                    {
                        q.Enqueue(new Point(pt.X - 1, pt.Y));
                    }

                    if (Check_FillValidity(pt.X, pt.Y + 1, landarea, border))
                    {
                        q.Enqueue(new Point(pt.X, pt.Y + 1));
                    }

                    if (Check_FillValidity(pt.X, pt.Y - 1, landarea, border))
                    {
                        q.Enqueue(new Point(pt.X, pt.Y - 1));
                    }
                }
            }
        }

        private bool Check_FillValidity(int x, int y, MapImage_Landarea landarea, MapImage_Borders border)
        {
            bool result = false;

            if ((x < bmp.Width) && (y < bmp.Height) && (x >= 0) && (y >= 0))
            {
                if (bmp.GetPixel(x, y).ToArgb() == COLOR_TRANSPARENT)
                {
                    if (landarea.GetBitmap().GetPixel(x, y).ToArgb() == COLOR_LAND)
                    {
                        if (border.GetBitmap().GetPixel(x, y).ToArgb() == COLOR_TRANSPARENT)
                        {
                            result = true;
                        }
                    }
                }
            }

            return result;
        }
    }
}
