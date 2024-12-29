using System.Data.SQLite;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using WFCLib;
using WFCLib.Models;

namespace BiomeVisualizer
{
    public partial class MainWindow : Window
    {
        private const string DatabasePath = "biomes.db";

        private Tile[] biomeMap;
        private int mapSize;
        private double tileSize = 20;

        private Point? startPoint = null;
        private Point? endPoint = null;
        private List<Point> pathPoints = new List<Point>();

        public MainWindow()
        {
            InitializeComponent();
            DatabaseInitializer.InitializeDatabase(DatabasePath);

            // Generate the biome map
            mapSize = 30;
            GenerateBiomeMapAsync();

            // Set the legend items
            var biomes = DatabaseInitializer.LoadBiomesFromDatabase(DatabasePath);
            var uniqueBiomes = biomes.GroupBy(b => b.Name).Select(g => g.First()).ToList();
            LegendItemsControl.ItemsSource = uniqueBiomes.Select(b => new
            {
                Name = b.Name,
                Color = (SolidColorBrush)new BrushConverter().ConvertFromString(b.Color)
            }).ToList();
        }

        private async void GenerateBiomeMapAsync()
        {
            ShowLoadingSpinner();
            var biomes = await Task.Run(() => DatabaseInitializer.LoadBiomesFromDatabase(DatabasePath)); // Load from database
            WaveFunctonCollapse.ValidateAndFixAdjacencyRules(biomes);
            biomeMap = await Task.Run(() => WaveFunctonCollapse.GenerateBiomeMapWFCWithFallback(biomes, mapSize));
            MapCanvas.InvalidateVisual(); // Redraw the map
            HideLoadingSpinner();
        }

        private void RegenerateMapButton_Click(object sender, RoutedEventArgs e)
        {
            biomeMap = new Tile[mapSize * mapSize]; // Reset the biome map
            startPoint = null; // Clear the start point
            endPoint = null; // Clear the end point
            pathPoints.Clear(); // Clear the path points
            GenerateBiomeMapAsync();
        }

        private void ShowLoadingSpinner()
        {
            LoadingSpinner.Visibility = Visibility.Visible;
        }

        private void HideLoadingSpinner()
        {
            LoadingSpinner.Visibility = Visibility.Collapsed;
        }

        private void MapCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(MapCanvas);

            // Get the DPI scaling factor
            var dpiScale = VisualTreeHelper.GetDpi(MapCanvas);
            var scaleX = dpiScale.DpiScaleX;
            var scaleY = dpiScale.DpiScaleY;

            // Adjust the position based on the DPI scaling factor
            var adjustedPosition = new Point(position.X * scaleX, position.Y * scaleY);

            // Calculate the center of the tile
            int tileX = (int)(adjustedPosition.X / tileSize);
            int tileY = (int)(adjustedPosition.Y / tileSize);

            // Ensure the click is within the bounds of the map
            if (tileX < 0 || tileX >= mapSize || tileY < 0 || tileY >= mapSize)
            {
                return;
            }

            // Find the tile center
            var tileCenter = new Point(tileX * tileSize + tileSize / 2, tileY * tileSize + tileSize / 2);

            if (startPoint == null)
            {
                startPoint = tileCenter;
            }
            else if (endPoint == null)
            {
                endPoint = tileCenter;
                // Perform A* pathfinding
                pathPoints = PerformAStarPathfinding(startPoint.Value, endPoint.Value);
            }
            else
            {
                startPoint = tileCenter;
                endPoint = null;
                pathPoints.Clear();
            }

            MapCanvas.InvalidateVisual(); // Trigger a redraw to show the points
        }

        private List<Point> PerformAStarPathfinding(Point start, Point end)
        {
            // Convert start and end points to tile coordinates
            int startX = (int)(start.X / tileSize);
            int startY = (int)(start.Y / tileSize);
            int endX = (int)(end.X / tileSize);
            int endY = (int)(end.Y / tileSize);

            // Initialize the open and closed lists
            var openList = new List<Tile>();
            var closedList = new HashSet<Tile>();

            // Add the start tile to the open list
            var startTile = biomeMap[startY * mapSize + startX];
            openList.Add(startTile);

            // Initialize the g, h, and f scores
            var gScores = new Dictionary<Tile, double>();
            var hScores = new Dictionary<Tile, double>();
            var fScores = new Dictionary<Tile, double>();
            gScores[startTile] = 0;
            hScores[startTile] = GetHeuristic(startTile, endX, endY);
            fScores[startTile] = hScores[startTile];

            // Initialize the cameFrom dictionary
            var cameFrom = new Dictionary<Tile, Tile>();

            while (openList.Count > 0)
            {
                // Get the tile with the lowest f score
                var currentTile = openList.OrderBy(t => fScores[t]).First();

                // If the current tile is the end tile, reconstruct the path
                if (currentTile.X == endX && currentTile.Y == endY)
                {
                    return ReconstructPath(cameFrom, currentTile);
                }

                // Move the current tile from the open list to the closed list
                openList.Remove(currentTile);
                closedList.Add(currentTile);

                // Get the neighbors of the current tile
                var neighbors = GetNeighbors(currentTile);

                foreach (var neighbor in neighbors)
                {
                    if (closedList.Contains(neighbor))
                    {
                        continue;
                    }

                    // Calculate the tentative g score
                    var tentativeGScore = gScores[currentTile] + neighbor.Cost;

                    if (!openList.Contains(neighbor))
                    {
                        openList.Add(neighbor);
                    }
                    else if (tentativeGScore >= gScores[neighbor])
                    {
                        continue;
                    }

                    // Update the cameFrom, g, h, and f scores
                    cameFrom[neighbor] = currentTile;
                    gScores[neighbor] = tentativeGScore;
                    hScores[neighbor] = GetHeuristic(neighbor, endX, endY);
                    fScores[neighbor] = gScores[neighbor] + hScores[neighbor];
                }
            }

            // Return an empty path if no path is found
            return new List<Point>();
        }

        private double GetHeuristic(Tile tile, int endX, int endY)
        {
            // Use the Manhattan distance as the heuristic
            return Math.Abs(tile.X - endX) + Math.Abs(tile.Y - endY);
        }

        private List<Tile> GetNeighbors(Tile tile)
        {
            var neighbors = new List<Tile>();

            // Get the coordinates of the neighbors
            var neighborCoords = new List<(int, int)>
                {
                    (tile.X - 1, tile.Y),
                    (tile.X + 1, tile.Y),
                    (tile.X, tile.Y - 1),
                    (tile.X, tile.Y + 1)
                };

            foreach (var (x, y) in neighborCoords)
            {
                if (x >= 0 && x < mapSize && y >= 0 && y < mapSize)
                {
                    neighbors.Add(biomeMap[y * mapSize + x]);
                }
            }

            return neighbors;
        }

        private List<Point> ReconstructPath(Dictionary<Tile, Tile> cameFrom, Tile currentTile)
        {
            var path = new List<Point>();

            while (cameFrom.ContainsKey(currentTile))
            {
                var tileCenter = new Point(currentTile.X * tileSize + tileSize / 2, currentTile.Y * tileSize + tileSize / 2);
                path.Add(tileCenter);
                currentTile = cameFrom[currentTile];
            }

            path.Reverse();
            return path;
        }

        private void MapCanvas_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.White);

            if (biomeMap == null)
            {
                return;
            }

            // Calculate the actual tile size based on the MapCanvas size and the number of tiles
            tileSize = Math.Min(e.Info.Width / (float)mapSize, e.Info.Height / (float)mapSize);

            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    var tile = biomeMap[y * mapSize + x];

                    // Draw biome tile
                    var rect = new SKRect(x * (float)tileSize, y * (float)tileSize, (x + 1) * (float)tileSize, (y + 1) * (float)tileSize);
                    var paint = new SKPaint
                    {
                        Color = !tile.Equals(default(Tile)) && !tile.Biome.Equals(default(Biome)) ? SKColor.Parse(tile.Biome.Color) : SKColors.White,
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
                    if (!tile.Equals(default(Tile)) && !tile.Biome.Equals(default(Biome)) && !tile.Biome.TradingPost.Equals(default(TradingPost)) &&
                        tile.X == tile.Biome.TradingPost.X &&
                        tile.Y == tile.Biome.TradingPost.Y)
                    {
                        var centerX = x * (float)tileSize + (float)tileSize / 2;
                        var centerY = y * (float)tileSize + (float)tileSize / 2;
                        var postPaint = new SKPaint
                        {
                            Color = SKColors.Black,
                            Style = SKPaintStyle.Fill
                        };
                        canvas.DrawCircle(centerX, centerY, (float)tileSize / 4, postPaint);
                    }
                }
            }

            // Draw the start and end points if they exist
            if (startPoint.HasValue)
            {
                DrawPoint(canvas, startPoint.Value, SKColors.Red);
            }

            if (endPoint.HasValue)
            {
                DrawPoint(canvas, endPoint.Value, SKColors.Blue);
            }

            // Draw the path points if they exist
            foreach (var point in pathPoints)
            {
                DrawPoint(canvas, point, SKColors.Yellow);
            }
        }

        private void DrawPoint(SKCanvas canvas, Point point, SKColor color)
        {
            using (var paint = new SKPaint())
            {
                paint.Color = color;
                paint.IsAntialias = true;
                canvas.DrawCircle((float)point.X, (float)point.Y, 5, paint);
            }
        }
    }
}
