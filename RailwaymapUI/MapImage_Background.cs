using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class MapImage_Background : MapImage
    {
        public void Fill()
        {
            if (gr != null)
            {
                gr.Clear(System.Drawing.Color.White);
            }
        }
    }
}
