using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WFCLib.Models;

namespace WFCLib
{
    public static class WaveFunctonCollapse
    {
        private static readonly Random rand = new Random(); // Seed for deterministic behavior

        public static Tile[] GenerateBiomeMapWFCWithFallback(List<Biome> biomes, int size)
        {
            while (true)
            {
                try
                {
                    return RunWFC(biomes, size);
                }
                catch (InvalidOperationException ex)
                {
                    Debug.WriteLine($"Restarting due to error: {ex.Message}");
                }
            }
        }

        public static Tile[] RunWFC(List<Biome> biomes, int size)
        {
            Tile[] map = new Tile[size * size];

            // Initialize possibilities
            var possibilities = new Biome[size * size][];
            for (int i = 0; i < possibilities.Length; i++)
            {
                possibilities[i] = biomes.ToArray();
            }

            // Collapse tiles until all are resolved
            while (HasUncollapsedTiles(possibilities, map, size))
            {
                var (x, y) = FindLowestEntropyTile(possibilities, size);

                if (possibilities[y * size + x].Length == 0)
                {
                    throw new InvalidOperationException($"Tile at ({x}, {y}) has no valid possibilities.");
                }

                var selectedBiome = possibilities[y * size + x][rand.Next(possibilities[y * size + x].Length)];
                map[y * size + x] = new Tile { X = x, Y = y, Biome = selectedBiome, Cost = selectedBiome.BaseCost, IsCollapsed = true };

                PropagateConstraints(possibilities, map, biomes, x, y, size);
            }

            // Check for any uncollapsed tiles
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (map[y * size + x] == null || !map[y * size + x].IsCollapsed)
                    {
                        throw new InvalidOperationException($"Tile at ({x}, {y}) is still null after WFC completion.");
                    }
                }
            }

            return map;
        }

        public static void PropagateConstraints(Biome[][] possibilities, Tile[] map, List<Biome> biomes, int x, int y, int size)
        {
            int rows = size;
            int cols = size;
            var queue = new Queue<(int, int)>();
            queue.Enqueue((x, y));

            while (queue.Count > 0)
            {
                var (cx, cy) = queue.Dequeue();
                var currentTile = map[cy * size + cx];
                if (currentTile == null) continue;

                var currentBiome = currentTile.Biome;

                foreach (var (dx, dy) in new[] { (0, -1), (0, 1), (-1, 0), (1, 0) })
                {
                    int nx = cx + dx, ny = cy + dy;

                    if (nx < 0 || nx >= cols || ny < 0 || ny >= rows || map[ny * size + nx]?.IsCollapsed == true)
                        continue;

                    var allowedBiomes = new List<Biome>();
                    foreach (var candidate in possibilities[ny * size + nx])
                    {
                        if (currentBiome.AdjacencyRules.TryGetValue(candidate.ID, out bool allowed) && allowed)
                        {
                            allowedBiomes.Add(candidate);
                        }
                    }

                    if (allowedBiomes.Count == 0)
                    {
                        throw new InvalidOperationException($"Tile at ({nx}, {ny}) has no valid possibilities after propagation. Check adjacency rules.");
                    }

                    if (allowedBiomes.Count < possibilities[ny * size + nx].Length)
                    {
                        possibilities[ny * size + nx] = allowedBiomes.ToArray();
                        queue.Enqueue((nx, ny));
                    }
                }
            }
        }

        public static void ValidateAndFixAdjacencyRules(List<Biome> biomes)
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

        public static (int, int) FindLowestEntropyTile(Biome[][] possibilities, int size)
        {
            int minEntropy = int.MaxValue;
            var candidates = new List<(int, int)>();

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int entropy = possibilities[y * size + x].Length;
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

            if (candidates.Count == 0)
            {
                throw new InvalidOperationException("No valid tiles to collapse.");
            }

            return candidates[rand.Next(candidates.Count)];
        }

        public static bool HasUncollapsedTiles(Biome[][] possibilities, Tile[] map, int size)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int index = y * size + x;
                    if (map[index] == null || !map[index].IsCollapsed)
                    {
                        if (possibilities[index] != null && possibilities[index].Length > 1)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
