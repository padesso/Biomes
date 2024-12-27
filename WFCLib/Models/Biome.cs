namespace WFCLib.Models
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
