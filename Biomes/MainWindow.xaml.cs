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
            if (tileX < 0 || tileX >= mapSize * dpiScale.DpiScaleX || tileY < 0 || tileY >= mapSize * dpiScale.DpiScaleY)
            {
                return;
            }

            //Find the tile center
            var tileCenter = new Point(tileX * tileSize + tileSize / 2, 
                                        tileY * tileSize + tileSize / 2);

            if (startPoint == null)
            {
                startPoint = tileCenter;
            }
            else if (endPoint == null)
            {
                endPoint = tileCenter;
            }
            else
            {
                startPoint = tileCenter;
                endPoint = null;
            }

            MapCanvas.InvalidateVisual(); // Trigger a redraw to show the points
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
