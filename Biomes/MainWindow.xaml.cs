using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Biomes;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;

namespace BiomeVisualizer
{
    public partial class MainWindow : Window
    {
        private const string DatabasePath = "biomes.db";

        private Tile[] biomeMap;
        private int tileSize = 15;
        private static readonly Random rand = new Random(); // Seed for deterministic behavior
        private int mapSize;

        public MainWindow()
        {
            InitializeComponent();

            DatabaseInitializer.InitializeDatabase(DatabasePath);

            // Generate the biome map
            mapSize = 25;
            GenerateBiomeMap();

            // Set the legend items
            var biomes = LoadBiomesFromDatabase(DatabasePath);
            var uniqueBiomes = biomes.GroupBy(b => b.Name).Select(g => g.First()).ToList();
            LegendItemsControl.ItemsSource = uniqueBiomes.Select(b => new
            {
                Name = b.Name,
                Color = (SolidColorBrush)new BrushConverter().ConvertFromString(b.Color)
            }).ToList();
        }

        private void GenerateBiomeMap()
        {
            var biomes = LoadBiomesFromDatabase(DatabasePath); // Load from database
            ValidateAndFixAdjacencyRules(biomes);
            biomeMap = GenerateBiomeMapWFCWithFallback(biomes, mapSize);
            MapCanvas.InvalidateVisual(); // Redraw the map
        }

        private void RegenerateMapButton_Click(object sender, RoutedEventArgs e)
        {
            biomeMap = new Tile[mapSize * mapSize]; // Reset the biome map
            GenerateBiomeMap();
        }

        private void MapCanvas_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.White);

            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    var tile = biomeMap[y * mapSize + x];

                    // Draw biome tile
                    var rect = new SKRect(x * tileSize, y * tileSize, (x + 1) * tileSize, (y + 1) * tileSize);
                    var paint = new SKPaint
                    {
                        Color = tile != null ? SKColor.Parse(tile.Biome.Color) : SKColors.White,
                        Style = SKPaintStyle.Fill
                    };
                    canvas.DrawRect(rect, paint);

                    // Draw grid lines
                    var gridPaint = new SKPaint
                    {
                        Color = SKColors.Gray,
                        Style = SKPaintStyle.Stroke,
                        StrokeWidth = 1
                    };
                    canvas.DrawRect(rect, gridPaint);

                    // Draw trading post
                    if (tile?.Biome.TradingPost != null &&
                        tile.X == tile.Biome.TradingPost.X &&
                        tile.Y == tile.Biome.TradingPost.Y)
                    {
                        var centerX = x * tileSize + tileSize / 2;
                        var centerY = y * tileSize + tileSize / 2;
                        var postPaint = new SKPaint
                        {
                            Color = SKColors.Black,
                            Style = SKPaintStyle.Fill
                        };
                        canvas.DrawCircle(centerX, centerY, tileSize / 4, postPaint);
                    }
                }
            }
        }

        static Tile[] GenerateBiomeMapWFCWithFallback(List<Biome> biomes, int size)
        {
            while (true)
            {
                try
                {
                    return GenerateBiomeMapWFC(biomes, size);
                }
                catch (InvalidOperationException ex)
                {
                    Debug.WriteLine($"Restarting due to error: {ex.Message}");
                }
            }
        }

        static Tile[] GenerateBiomeMapWFC(List<Biome> biomes, int size)
        {
            while (true)
            {
                try
                {
                    return RunWFC(biomes, size);
                }
                catch (InvalidOperationException ex)
                {
                    Debug.WriteLine($"WFC failed: {ex.Message}");
                    // Optional: Print current state for debugging
                    //LogCurrentState(size, null, null);
                }
            }
        }

        static Tile[] RunWFC(List<Biome> biomes, int size)
        {
            Tile[] map = new Tile[size * size];

            // Initialize possibilities
            var possibilities = new List<Biome>[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    possibilities[y * size + x] = new List<Biome>(biomes);
                }
            }

            // Collapse tiles until all are resolved
            while (HasUncollapsedTiles(possibilities, map, size))
            {
                var (x, y) = FindLowestEntropyTile(possibilities, size);

                if (!possibilities[y * size + x].Any())
                {
                    //LogCurrentState(size, map, possibilities);
                    throw new InvalidOperationException($"Tile at ({x}, {y}) has no valid possibilities.");
                }

                var selectedBiome = possibilities[y * size + x][rand.Next(possibilities[y * size + x].Count)];
                map[y * size + x] = new Tile { X = x, Y = y, Biome = selectedBiome, Cost = selectedBiome.BaseCost, IsCollapsed = true };

                //Debug.WriteLine($"Collapsed tile ({x}, {y}) to biome '{selectedBiome.Name}'.");

                PropagateConstraints(possibilities, map, biomes, x, y, size);
            }

            // Check for any uncollapsed tiles
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (map[y * size + x] == null || !map[y * size + x].IsCollapsed)
                    {
                        //LogCurrentState(size, map, possibilities);
                        throw new InvalidOperationException($"Tile at ({x}, {y}) is still null after WFC completion.");
                    }
                }
            }

            return map;
        }

        static void PropagateConstraints(List<Biome>[] possibilities, Tile[] map, List<Biome> biomes, int x, int y, int size)
        {
            int rows = size;
            int cols = size;

            foreach (var (dx, dy) in new[] { (0, -1), (0, 1), (-1, 0), (1, 0) })
            {
                int nx = x + dx, ny = y + dy;

                if (nx < 0 || nx >= cols || ny < 0 || ny >= rows || map[ny * size + nx]?.IsCollapsed == true)
                    continue;

                var currentTile = map[y * size + x];
                if (currentTile == null)
                    continue;

                var currentBiome = currentTile.Biome;
                var allowedBiomes = new List<Biome>();

                foreach (var candidate in possibilities[ny * size + nx])
                {
                    if (currentBiome.AdjacencyRules.TryGetValue(candidate.ID, out bool allowed) && allowed)
                    {
                        allowedBiomes.Add(candidate);
                    }
                }

                if (!allowedBiomes.Any())
                {
                    //Debug.WriteLine($"Tile at ({nx}, {ny}) has no valid possibilities after propagation. Current biome: {currentBiome.Name}");
                    //Debug.WriteLine("Possible candidates before propagation:");
                    foreach (var candidate in possibilities[ny * size + nx])
                    {
                        //Debug.WriteLine($"- {candidate.Name}");
                    }
                    throw new InvalidOperationException($"Tile at ({nx}, {ny}) has no valid possibilities after propagation. Check adjacency rules.");
                }

                if (allowedBiomes.Count < possibilities[ny * size + nx].Count)
                {
                    //Debug.WriteLine($"Updated tile at ({nx}, {ny}) to have {allowedBiomes.Count} possibilities.");
                    possibilities[ny * size + nx] = allowedBiomes;

                    // Recursively propagate constraints to neighbors
                    PropagateConstraints(possibilities, map, biomes, nx, ny, size);
                }
            }
        }

        static void ValidateAndFixAdjacencyRules(List<Biome> biomes)
        {
            foreach (var biome in biomes)
            {
                // Ensure self-adjacency
                if (!biome.AdjacencyRules.ContainsKey(biome.ID))
                {
                    biome.AdjacencyRules[biome.ID] = true;
                    Debug.WriteLine($"Added self-adjacency rule for biome '{biome.Name}'");
                }

                // Ensure bi-directional adjacency rules
                foreach (var neighborBiome in biomes)
                {
                    if (biome.ID != neighborBiome.ID)
                    {
                        if (biome.AdjacencyRules.TryGetValue(neighborBiome.ID, out bool allowed))
                        {
                            if (!neighborBiome.AdjacencyRules.ContainsKey(biome.ID))
                            {
                                neighborBiome.AdjacencyRules[biome.ID] = allowed;
                                Debug.WriteLine($"Added missing reverse adjacency rule: Biome '{neighborBiome.Name}' -> Biome '{biome.Name}' = {allowed}");
                            }
                        }
                    }
                }
            }
        }

        static (int, int) FindLowestEntropyTile(List<Biome>[] possibilities, int size)
        {
            int minEntropy = int.MaxValue;
            var candidates = new List<(int, int)>();

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int entropy = possibilities[y * size + x].Count;
                    if (entropy > 1 && entropy < minEntropy)
                    {
                        minEntropy = entropy;
                        candidates.Clear();
                        candidates.Add((x, y));
                    }
                    else if (entropy == minEntropy)
                    {
                        candidates.Add((x, y));
                    }
                }
            }

            if (!candidates.Any())
            {
                throw new InvalidOperationException("No valid tiles to collapse.");
            }

            return candidates[rand.Next(candidates.Count)];
        }

        static bool HasUncollapsedTiles(List<Biome>[] possibilities, Tile[] map, int size)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int index = y * size + x;
                    if (map[index] == null || !map[index].IsCollapsed)
                    {
                        if (possibilities[index] != null && possibilities[index].Count > 1)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        static List<Biome> LoadBiomesFromDatabase(string dbPath)
        {
            List<Biome> biomes = new List<Biome>();
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Open();
                string query = "SELECT * FROM Biomes";
                var tradingPosts = LoadTradingPosts(connection);

                using (var cmd = new SQLiteCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Biome biome = new Biome
                        {
                            ID = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Color = reader.GetString(2),
                            BaseCost = reader.GetDouble(3)
                        };

                        // Load adjacency rules
                        biome.AdjacencyRules = LoadAdjacencyRules(connection, biome.ID);

                        // Load commodities
                        biome.Commodities = LoadCommoditiesForBiome(connection, biome.ID);

                        // Assign trading post
                        biome.TradingPost = tradingPosts.FirstOrDefault(tp => tp.BiomeID == biome.ID);

                        biomes.Add(biome);
                    }
                }
            }
            return biomes;
        }

        static List<TradingPost> LoadTradingPosts(SQLiteConnection connection)
        {
            var tradingPosts = new List<TradingPost>();
            string query = "SELECT * FROM TradingPosts";
            using (var cmd = new SQLiteCommand(query, connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var tradingPost = new TradingPost
                    {
                        ID = reader.GetInt32(0),
                        BiomeID = reader.GetInt32(1),
                        Name = reader.GetString(2),
                        X = reader.GetInt32(3),
                        Y = reader.GetInt32(4)
                    };
                    tradingPosts.Add(tradingPost);
                }
            }
            return tradingPosts;
        }

        static Dictionary<int, bool> LoadAdjacencyRules(SQLiteConnection connection, int biomeID)
        {
            Dictionary<int, bool> rules = new Dictionary<int, bool>();
            string query = "SELECT AdjacentBiomeID, Allowed FROM BiomeAdjacency WHERE BiomeID = @BiomeID";
            using (var cmd = new SQLiteCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@BiomeID", biomeID);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rules[reader.GetInt32(0)] = reader.GetBoolean(1);
                    }
                }
            }
            return rules;
        }

        static List<string> LoadCommoditiesForBiome(SQLiteConnection connection, int biomeID)
        {
            HashSet<string> commodities = new HashSet<string>();
            string query = @"
                                                SELECT Commodities.Name 
                                                FROM BiomeCommodities
                                                INNER JOIN Commodities ON BiomeCommodities.CommodityID = Commodities.CommodityID
                                                WHERE BiomeCommodities.BiomeID = @BiomeID";
            using (var cmd = new SQLiteCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@BiomeID", biomeID);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        commodities.Add(reader.GetString(0));
                    }
                }
            }
            return commodities.ToList();
        }
    }
}
