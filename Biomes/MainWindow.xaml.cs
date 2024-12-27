using System.Data.SQLite;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using WFCLib;
using WFCLib.Models;

namespace BiomeVisualizer
{
    public partial class MainWindow : Window
    {
        private const string DatabasePath = "biomes.db";

        private Tile[] biomeMap;
        private int tileSize = 20;
        
        private int mapSize;

        public MainWindow()
        {
            InitializeComponent();

            DatabaseInitializer.InitializeDatabase(DatabasePath);

            // Generate the biome map
            mapSize = 25;
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

        private void MapCanvas_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.White);

            if (biomeMap == null)
            {
                return;
            }

            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    var tile = biomeMap[y * mapSize + x];

                    // Draw biome tile
                    var rect = new SKRect(x * tileSize, y * tileSize, (x + 1) * tileSize, (y + 1) * tileSize);
                    var paint = new SKPaint
                    {
                        Color = tile?.Biome != null ? SKColor.Parse(tile.Biome.Color) : SKColors.White,
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
                    if (tile?.Biome?.TradingPost != null &&
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


    }
}
