namespace WFCLib.Models
{
    public struct Tile
    {
        public int X { get; set; }
        public int Y { get; set; }
        public Biome Biome { get; set; }
        public double Cost { get; set; }
        public double FScore { get; set; }
        public bool IsCollapsed { get; set; }
    }
}
