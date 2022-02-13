using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class DrawLine
    {
        static private List<Point> Get_Linepoints(Point p1, Point p2)
        {
            List<Point> result = new List<Point>();

            int x0 = p1.X;
            int x1 = p2.X;
            int y0 = p1.Y;
            int y1 = p2.Y;

            int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = (dx > dy ? dx : -dy) / 2, e2;

            for (; ; )
            {
                result.Add(new Point(x0, y0));

                if (x0 == x1 && y0 == y1) break;
                e2 = err;
                if (e2 > -dx) { err -= dy; x0 += sx; }
                if (e2 < dy) { err += dx; y0 += sy; }
            }


            return result;
        }

        static public void Draw_Line(Point p1, Point p2, Bitmap bmp, Color color, int thickness)
        {
            List<Point> points = Get_Linepoints(p1, p2);

            foreach (Point pt in points)
            {
                if ((pt.X >= 0) && (pt.X < bmp.Width) && (pt.Y >= 0) && (pt.Y < bmp.Height))
                {
                    bmp.SetPixel(pt.X, pt.Y, color);

                    if (thickness > 1)
                    {
                        if (pt.X > 0)
                        {
                            bmp.SetPixel(pt.X - 1, pt.Y, color);
                        }

                        if (pt.Y > 0)
                        {
                            bmp.SetPixel(pt.X, pt.Y - 1, color);
                        }

                        if (thickness > 2)
                        {
                            if (pt.Y < (bmp.Height - 1))
                            {
                                bmp.SetPixel(pt.X, pt.Y + 1, color);
                            }

                            if (pt.X < (bmp.Width - 1))
                            {
                                bmp.SetPixel(pt.X + 1, pt.Y, color);
                            }
                        }
                    }
                }
            }
        }
    }
}
