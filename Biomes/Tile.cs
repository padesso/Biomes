using BiomeVisualizer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biomes
{
    public class Tile
    {
        public int X { get; set; }
        public int Y { get; set; }
        public Biome Biome { get; set; }
        public double Cost { get; set; }
        public double FScore { get; set; } // For A* sorting
        public bool IsCollapsed { get; set; } // Indicates if the tile has been collapsed
    }
}
