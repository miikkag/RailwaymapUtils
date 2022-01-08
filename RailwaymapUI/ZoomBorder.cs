using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;


namespace RailwaymapUI
{
    public class ZoomBorder : Border, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public event RoutedEventHandler Selection_Update;

        private UIElement child = null;
        private Point origin;
        private Point start;

        private const int Selection_Size_Default = 80;
        private const int Selection_Size_Min = 20;
        private const int Selection_Size_Max = 300;

        public Point Selection_Point;
        public int Selection_Size;

        private double[] zoomfactors = new double[10]
        { 0.25, 0.50, 0.75, 1.0, 1.5, 2.0, 3.0, 4.0, 5.0, 6.0};

        private const int zoomindex_default = 3;
        private int zoomindex = zoomindex_default;


        private TranslateTransform GetTranslateTransform(UIElement element)
        {
            return (TranslateTransform)((TransformGroup)element.RenderTransform)
              .Children.First(tr => tr is TranslateTransform);
        }

        private ScaleTransform GetScaleTransform(UIElement element)
        {
            return (ScaleTransform)((TransformGroup)element.RenderTransform)
              .Children.First(tr => tr is ScaleTransform);
        }

        public override UIElement Child
        {
            get { return base.Child; }
            set
            {
                if (value != null && value != this.Child)
                    this.Initialize(value);
                base.Child = value;
            }
        }

        public string ZoomString
        {
            get
            {
                return ((int)(zoomfactors[zoomindex] * 100)).ToString() + " %";
            }
        }

        public void Initialize(UIElement element)
        {
            this.child = element;
            if (child != null)
            {
                TransformGroup group = new TransformGroup();
                ScaleTransform st = new ScaleTransform();
                group.Children.Add(st);
                TranslateTransform tt = new TranslateTransform();
                group.Children.Add(tt);
                child.RenderTransform = group;
                child.RenderTransformOrigin = new Point(0.0, 0.0);
                this.MouseWheel += child_MouseWheel;
                this.MouseLeftButtonDown += child_MouseLeftButtonDown;
                this.MouseLeftButtonUp += child_MouseLeftButtonUp;
                this.MouseMove += child_MouseMove;
                this.PreviewMouseRightButtonDown += new MouseButtonEventHandler(
                  child_PreviewMouseRightButtonDown);
            }

            Selection_Point = new Point(-1, -1);
        }

        public void Reset()
        {
            if (child != null)
            {
                // reset zoom
                var st = GetScaleTransform(child);

                zoomindex = zoomindex_default;

                st.ScaleX = zoomfactors[zoomindex];
                st.ScaleY = zoomfactors[zoomindex];

                // reset pan
                var tt = GetTranslateTransform(child);
                tt.X = 0.0;
                tt.Y = 0.0;
            }

            OnPropertyChanged("ZoomString");
        }

        public void Selection_Clear()
        {
            Selection_Point = new Point(-1, -1);
            Selection_Size = Selection_Size_Default;
        }

        public void Selection_Plus()
        {
            Selection_Size += 10;

            if (Selection_Size > Selection_Size_Max)
            {
                Selection_Size = Selection_Size_Max;
            }

            Selection_Update?.Invoke(null, new RoutedEventArgs());
        }

        public void Selection_Minus()
        {
            Selection_Size -= 10;

            if (Selection_Size < Selection_Size_Min)
            {
                Selection_Size = Selection_Size_Min;
            }

            Selection_Update?.Invoke(null, new RoutedEventArgs());
        }

        public void Zoom_In()
        {
            if (zoomindex < (zoomfactors.Length - 1))
            {
                zoomindex++;
            }

            Set_Zoom(null);
        }

        public void Zoom_Out()
        {
            if (zoomindex > 0)
            {
                zoomindex--;
            }

            Set_Zoom(null);
        }

        #region Child Events

        private void Set_Zoom(MouseWheelEventArgs e)
        {
            if (child != null)
            {
                var st = GetScaleTransform(child);
                var tt = GetTranslateTransform(child);


                if (e != null)
                {
                    Point relative = e.GetPosition(child);
                    double absoluteX;
                    double absoluteY;

                    absoluteX = Math.Floor(relative.X * st.ScaleX + tt.X);
                    absoluteY = Math.Floor(relative.Y * st.ScaleY + tt.Y);

                    st.ScaleX = zoomfactors[zoomindex];
                    st.ScaleY = zoomfactors[zoomindex];

                    tt.X = Math.Floor(absoluteX - relative.X * st.ScaleX);
                    tt.Y = Math.Floor(absoluteY - relative.Y * st.ScaleY);
                }
                else
                {
                    st.ScaleX = zoomfactors[zoomindex];
                    st.ScaleY = zoomfactors[zoomindex];
                }
            }

            OnPropertyChanged("ZoomString");
        }

        private void child_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (child != null)
            {
                var st = GetScaleTransform(child);
                var tt = GetTranslateTransform(child);

                if ((e.Delta > 0) && (zoomindex < (zoomfactors.Length-1)))
                {
                    zoomindex++;
                }
                else if ((e.Delta < 0) && (zoomindex > 0))
                {
                    zoomindex--;
                }
                else
                {
                    return;
                }

                Set_Zoom(e);
            }
        }

        private void child_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (child != null)
            {
                var tt = GetTranslateTransform(child);
                start = e.GetPosition(this);
                origin = new Point(tt.X, tt.Y);
                this.Cursor = Cursors.Hand;
                child.CaptureMouse();
            }
        }

        private void child_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (child != null)
            {
                child.ReleaseMouseCapture();
                this.Cursor = Cursors.Arrow;
            }
        }

        void child_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            //this.Reset();
            Point pt = e.GetPosition(this);
            TranslateTransform tt = GetTranslateTransform(child);

            double offsetx = 0;
            double offsety = 0;

            double diffx = ActualWidth - (child as Canvas).Width;
            double diffy = ActualHeight - (child as Canvas).Height;

            if (diffx > 0)
            {
                offsetx = diffx / 2;
            }

            if (diffy > 0)
            {
                offsety = diffy / 2;
            }

            double x = (pt.X - (tt.X + offsetx)) / zoomfactors[zoomindex];
            double y = (pt.Y - (tt.Y + offsety)) / zoomfactors[zoomindex];

            //Console.WriteLine("W,H: " + ActualWidth.ToString() + " " + ActualHeight.ToString() + "  Child: " + (child as Canvas).Width.ToString() + " " + (child as Canvas).Height.ToString());
            //Console.WriteLine("Pt: " + pt.X.ToString() + " " + pt.Y.ToString());
            //Console.WriteLine("tt: " + tt.X.ToString() + " " + tt.Y.ToString());
            //Console.WriteLine("xy: " + x.ToString() + " " + y.ToString());

            Selection_Point = new Point(x, y);

            Selection_Update?.Invoke(sender, new RoutedEventArgs());
        }

        private void child_MouseMove(object sender, MouseEventArgs e)
        {
            if (child != null)
            {
                if (child.IsMouseCaptured)
                {
                    TranslateTransform tt = GetTranslateTransform(child);
                    Vector v = start - e.GetPosition(this);
                    tt.X = Math.Floor(origin.X - v.X);
                    tt.Y = Math.Floor(origin.Y - v.Y);
                }
            }
        }

        #endregion
    }
}
