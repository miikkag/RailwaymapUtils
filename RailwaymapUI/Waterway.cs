using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;

namespace RailwaymapUI
{
    public class Waterway
    {
        public List<Coordinate> Coordinates;
        public Int64 Id;

        public Waterway()
        {
            Coordinates = new List<Coordinate>();
            Id = 0;
        }

        public Waterway(Int64 id, Way way)
        {
            Id = id;
            Coordinates = new List<Coordinate>();

            for (int i = 1; i < way.Coordinates.Count; i++)
            {
                Coordinates.Add(new Coordinate(way.Coordinates[i].NodeID, way.Coordinates[i].Latitude, way.Coordinates[i].Longitude));
            }
        }

        public Waterway(BinaryReader reader)
        {
            Coordinates = new List<Coordinate>();

            try
            {
                Id = reader.ReadInt64();

                int count = reader.ReadInt32();

                for (int i = 0; i < count; i++)
                {
                    Coordinates.Add(new Coordinate(reader));
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Waterway: error reading cache: " + ex.Message);
            }
        }

        public void Write_Cache(BinaryWriter writer)
        {
            writer.Write(Id);

            writer.Write(Coordinates.Count);

            for (int i = 0; i < Coordinates.Count; i++)
            {
                Coordinates[i].Write_Cache(writer);
            }
        }
    }
}
