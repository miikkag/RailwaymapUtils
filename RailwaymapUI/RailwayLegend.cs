using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public enum RailwayType
    {
        Normal_Electrified_25kV,
        Normal_Electrified_15kV,
        Normal_Electrified_3000V,
        Normal_Electrified_1500V,
        Normal_Electrified_750V,
        Normal_Electrified_Other,
        Normal_Non_Electrified,
        Dual_Gauge,
        Normal_Construction,
        Disused,
        Narrow_Electrified,
        Narrow_Non_Electrified,
        Narrow_Construction,
        RailwayType_MAX
    }

    public class RailwayLegend
    {
        public bool[] Used_Types;

        private readonly bool[] types_normal;
        private readonly bool[] types_narrow;

        private readonly string[] descriptions;

        public const string HEADER_NORMAL = "Normal/wide gauge:";
        public const string HEADER_NARROW = "Narrow gauge:";

        public RailwayLegend()
        {
            Used_Types = new bool[(int)RailwayType.RailwayType_MAX];

            types_normal = new bool[(int)RailwayType.RailwayType_MAX];

            types_normal[(int)RailwayType.Disused] = true;
            types_normal[(int)RailwayType.Dual_Gauge] = true;
            types_normal[(int)RailwayType.Normal_Construction] = true;
            types_normal[(int)RailwayType.Normal_Electrified_750V] = true;
            types_normal[(int)RailwayType.Normal_Electrified_1500V] = true;
            types_normal[(int)RailwayType.Normal_Electrified_3000V] = true;
            types_normal[(int)RailwayType.Normal_Electrified_15kV] = true;
            types_normal[(int)RailwayType.Normal_Electrified_25kV] = true;
            types_normal[(int)RailwayType.Normal_Electrified_Other] = true;
            types_normal[(int)RailwayType.Normal_Non_Electrified] = true;

            types_narrow = new bool[(int)RailwayType.RailwayType_MAX];

            types_narrow[(int)RailwayType.Narrow_Construction] = true;
            types_narrow[(int)RailwayType.Narrow_Electrified] = true;
            types_narrow[(int)RailwayType.Narrow_Non_Electrified] = true;

            descriptions = new string[(int)RailwayType.RailwayType_MAX];

            descriptions[(int)RailwayType.Disused] = "Disused";
            descriptions[(int)RailwayType.Dual_Gauge] = "Dual gauge";
            descriptions[(int)RailwayType.Normal_Construction] = "Under construction";
            descriptions[(int)RailwayType.Narrow_Construction] = "Under construction";
            descriptions[(int)RailwayType.Narrow_Electrified] = "Electrified";
            descriptions[(int)RailwayType.Narrow_Non_Electrified] = "Non-electrified";
            descriptions[(int)RailwayType.Normal_Non_Electrified] = "Non-electrified";
            descriptions[(int)RailwayType.Normal_Electrified_750V] = "Electrified 750 V";
            descriptions[(int)RailwayType.Normal_Electrified_1500V] = "Electrified 1500 V";
            descriptions[(int)RailwayType.Normal_Electrified_3000V] = "Electrified 3000 V";
            descriptions[(int)RailwayType.Normal_Electrified_15kV] = "Electrified 15 kV";
            descriptions[(int)RailwayType.Normal_Electrified_25kV] = "Electrified 25 kV";
            descriptions[(int)RailwayType.Normal_Electrified_Other] = "Electrified other";
        }

        public void Clear()
        {
            for (int i = 0; i < Used_Types.Length; i++)
            {
                Used_Types[i] = false;
            }
        }

        public void Set_UsedType(RailwayType set_type)
        {
            Used_Types[(int)set_type] = true;
        }

        public List<RailwayType> Get_Used_Types(bool normal_gauge)
        {
            List<RailwayType> result = new List<RailwayType>();

            for (int i = 0; i < Used_Types.Length; i++)
            {
                if (normal_gauge)
                {
                    if (Used_Types[i] && types_normal[i])
                    {
                        result.Add((RailwayType)i);
                    }
                }
                else
                {
                    if (Used_Types[i] && types_narrow[i])
                    {
                        result.Add((RailwayType)i);
                    }
                }
            }

            return result;
        }

        public string Get_Type_Description(RailwayType t)
        {
            return descriptions[(int)t];
        }
    }
}
