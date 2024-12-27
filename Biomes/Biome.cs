using BiomeVisualizer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biomes
{
    public class Biome
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
        public double BaseCost { get; set; }
        public Dictionary<int, bool> AdjacencyRules { get; set; }
        public List<string> Commodities { get; set; }
        public TradingPost TradingPost { get; set; }
    }
}
