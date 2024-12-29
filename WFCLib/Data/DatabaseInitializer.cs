using System;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using WFCLib.Models;

public static class DatabaseInitializer
{
    public static void InitializeDatabase(string dbPath)
    {
        using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
        {
            connection.Open();

            // Create tables if they don't exist
            CreateTables(connection);

            // Insert or update initial data
            InsertOrUpdateInitialData(connection);
        }
    }

    private static void CreateTables(SQLiteConnection connection)
    {
        string createBiomesTable = @"
            CREATE TABLE IF NOT EXISTS Biomes (
                ID INTEGER PRIMARY KEY,
                Name TEXT NOT NULL,
                Color TEXT NOT NULL,
                BaseCost REAL NOT NULL
            )";

        string createCommoditiesTable = @"
            CREATE TABLE IF NOT EXISTS Commodities (
                CommodityID INTEGER PRIMARY KEY,
                Name TEXT NOT NULL
            )";

        string createBiomeCommoditiesTable = @"
            CREATE TABLE IF NOT EXISTS BiomeCommodities (
                BiomeID INTEGER NOT NULL,
                CommodityID INTEGER NOT NULL,
                FOREIGN KEY (BiomeID) REFERENCES Biomes(ID),
                FOREIGN KEY (CommodityID) REFERENCES Commodities(CommodityID)
            )";

        string createTradingPostsTable = @"
            CREATE TABLE IF NOT EXISTS TradingPosts (
                ID INTEGER PRIMARY KEY,
                BiomeID INTEGER NOT NULL,
                Name TEXT NOT NULL,
                X INTEGER NOT NULL,
                Y INTEGER NOT NULL,
                FOREIGN KEY (BiomeID) REFERENCES Biomes(ID)
            )";

        string createBiomeAdjacencyTable = @"
            CREATE TABLE IF NOT EXISTS BiomeAdjacency (
                BiomeID INTEGER NOT NULL,
                AdjacentBiomeID INTEGER NOT NULL,
                Allowed BOOLEAN NOT NULL,
                FOREIGN KEY (BiomeID) REFERENCES Biomes(ID),
                FOREIGN KEY (AdjacentBiomeID) REFERENCES Biomes(ID)
            )";

        using (var cmd = new SQLiteCommand(createBiomesTable, connection))
        {
            cmd.ExecuteNonQuery();
        }

        using (var cmd = new SQLiteCommand(createCommoditiesTable, connection))
        {
            cmd.ExecuteNonQuery();
        }

        using (var cmd = new SQLiteCommand(createBiomeCommoditiesTable, connection))
        {
            cmd.ExecuteNonQuery();
        }

        using (var cmd = new SQLiteCommand(createTradingPostsTable, connection))
        {
            cmd.ExecuteNonQuery();
        }

        using (var cmd = new SQLiteCommand(createBiomeAdjacencyTable, connection))
        {
            cmd.ExecuteNonQuery();
        }
    }

    private static void InsertOrUpdateInitialData(SQLiteConnection connection)
    {
        // Insert or update biomes
        InsertOrUpdateBiome(connection, 1, "Forest", "#228B22", 1.0);
        InsertOrUpdateBiome(connection, 2, "Desert", "#EDC9AF", 1.5);
        InsertOrUpdateBiome(connection, 3, "Mountain", "#A9A9A9", 2.0);
        InsertOrUpdateBiome(connection, 4, "Swamp", "#556B2F", 1.2);
        InsertOrUpdateBiome(connection, 5, "Tundra", "#ADD8E6", 1.8);
        InsertOrUpdateBiome(connection, 6, "Water", "#1E90FF", 1.0);

        // Insert or update commodities
        InsertOrUpdateCommodity(connection, 1, "Wood");
        InsertOrUpdateCommodity(connection, 2, "Stone");
        InsertOrUpdateCommodity(connection, 3, "Gold");
        InsertOrUpdateCommodity(connection, 4, "Herbs");
        InsertOrUpdateCommodity(connection, 5, "Ice");
        InsertOrUpdateCommodity(connection, 6, "Fish");

        // Insert or update biome commodities
        InsertOrUpdateBiomeCommodity(connection, 1, 1); // Forest -> Wood
        InsertOrUpdateBiomeCommodity(connection, 2, 2); // Desert -> Stone
        InsertOrUpdateBiomeCommodity(connection, 3, 3); // Mountain -> Gold
        InsertOrUpdateBiomeCommodity(connection, 4, 4); // Swamp -> Herbs
        InsertOrUpdateBiomeCommodity(connection, 5, 5); // Tundra -> Ice
        InsertOrUpdateBiomeCommodity(connection, 6, 6); // Water -> Fish

        // Insert or update trading posts
        InsertOrUpdateTradingPost(connection, 1, 1, "Forest Trading Post", 0, 0);
        InsertOrUpdateTradingPost(connection, 2, 2, "Desert Trading Post", 1, 1);
        InsertOrUpdateTradingPost(connection, 3, 3, "Mountain Trading Post", 2, 2);
        InsertOrUpdateTradingPost(connection, 4, 4, "Swamp Trading Post", 3, 3);
        InsertOrUpdateTradingPost(connection, 5, 5, "Tundra Trading Post", 4, 4);
        InsertOrUpdateTradingPost(connection, 6, 6, "Water Trading Post", 5, 5);

        // Insert or update adjacency rules
        InsertOrUpdateBiomeAdjacency(connection, 1, 2, false); // Forest <-> Desert
        InsertOrUpdateBiomeAdjacency(connection, 1, 3, true); // Forest <-> Mountain
        InsertOrUpdateBiomeAdjacency(connection, 1, 4, true); // Forest <-> Swamp
        InsertOrUpdateBiomeAdjacency(connection, 1, 5, false); // Forest <-> Tundra
        InsertOrUpdateBiomeAdjacency(connection, 1, 6, true); // Forest <-> Water

        InsertOrUpdateBiomeAdjacency(connection, 2, 3, true); // Desert <-> Mountain
        InsertOrUpdateBiomeAdjacency(connection, 2, 4, false); // Desert <-> Swamp
        InsertOrUpdateBiomeAdjacency(connection, 2, 5, false); // Desert <-> Tundra
        InsertOrUpdateBiomeAdjacency(connection, 2, 6, false); // Desert <-> Water

        InsertOrUpdateBiomeAdjacency(connection, 3, 4, false); // Mountain <-> Swamp
        InsertOrUpdateBiomeAdjacency(connection, 3, 5, true); // Mountain <-> Tundra
        InsertOrUpdateBiomeAdjacency(connection, 3, 6, false); // Mountain <-> Water

        InsertOrUpdateBiomeAdjacency(connection, 4, 5, false); // Swamp <-> Tundra
        InsertOrUpdateBiomeAdjacency(connection, 4, 6, true); // Swamp <-> Water

        InsertOrUpdateBiomeAdjacency(connection, 5, 6, true); // Tundra <-> Water
    }

    private static void InsertOrUpdateBiome(SQLiteConnection connection, int id, string name, string color, double baseCost)
    {
        if (BiomeExists(connection, id))
        {
            string query = "UPDATE Biomes SET Name = @Name, Color = @Color, BaseCost = @BaseCost WHERE ID = @ID";
            using (var cmd = new SQLiteCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@ID", id);
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.Parameters.AddWithValue("@Color", color);
                cmd.Parameters.AddWithValue("@BaseCost", baseCost);
                cmd.ExecuteNonQuery();
            }
        }
        else
        {
            string query = "INSERT INTO Biomes (ID, Name, Color, BaseCost) VALUES (@ID, @Name, @Color, @BaseCost)";
            using (var cmd = new SQLiteCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@ID", id);
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.Parameters.AddWithValue("@Color", color);
                cmd.Parameters.AddWithValue("@BaseCost", baseCost);
                cmd.ExecuteNonQuery();
            }
        }
    }

    private static void InsertOrUpdateCommodity(SQLiteConnection connection, int id, string name)
    {
        if (CommodityExists(connection, id))
        {
            string query = "UPDATE Commodities SET Name = @Name WHERE CommodityID = @ID";
            using (var cmd = new SQLiteCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@ID", id);
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.ExecuteNonQuery();
            }
        }
        else
        {
            string query = "INSERT INTO Commodities (CommodityID, Name) VALUES (@ID, @Name)";
            using (var cmd = new SQLiteCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@ID", id);
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.ExecuteNonQuery();
            }
        }
    }

    private static void InsertOrUpdateBiomeCommodity(SQLiteConnection connection, int biomeId, int commodityId)
    {
        if (!BiomeCommodityExists(connection, biomeId, commodityId))
        {
            string query = "INSERT INTO BiomeCommodities (BiomeID, CommodityID) VALUES (@BiomeID, @CommodityID)";
            using (var cmd = new SQLiteCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@BiomeID", biomeId);
                cmd.Parameters.AddWithValue("@CommodityID", commodityId);
                cmd.ExecuteNonQuery();
            }
        }
    }

    private static void InsertOrUpdateTradingPost(SQLiteConnection connection, int id, int biomeId, string name, int x, int y)
    {
        if (TradingPostExists(connection, id))
        {
            string query = "UPDATE TradingPosts SET BiomeID = @BiomeID, Name = @Name, X = @X, Y = @Y WHERE ID = @ID";
            using (var cmd = new SQLiteCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@ID", id);
                cmd.Parameters.AddWithValue("@BiomeID", biomeId);
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.Parameters.AddWithValue("@X", x);
                cmd.Parameters.AddWithValue("@Y", y);
                cmd.ExecuteNonQuery();
            }
        }
        else
        {
            string query = "INSERT INTO TradingPosts (ID, BiomeID, Name, X, Y) VALUES (@ID, @BiomeID, @Name, @X, @Y)";
            using (var cmd = new SQLiteCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@ID", id);
                cmd.Parameters.AddWithValue("@BiomeID", biomeId);
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.Parameters.AddWithValue("@X", x);
                cmd.Parameters.AddWithValue("@Y", y);
                cmd.ExecuteNonQuery();
            }
        }
    }

    private static void InsertOrUpdateBiomeAdjacency(SQLiteConnection connection, int biomeId, int adjacentBiomeId, bool allowed)
    {
        if (BiomeAdjacencyExists(connection, biomeId, adjacentBiomeId))
        {
            string query = "UPDATE BiomeAdjacency SET Allowed = @Allowed WHERE BiomeID = @BiomeID AND AdjacentBiomeID = @AdjacentBiomeID";
            using (var cmd = new SQLiteCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@BiomeID", biomeId);
                cmd.Parameters.AddWithValue("@AdjacentBiomeID", adjacentBiomeId);
                cmd.Parameters.AddWithValue("@Allowed", allowed);
                cmd.ExecuteNonQuery();
            }
        }
        else
        {
            string query = "INSERT INTO BiomeAdjacency (BiomeID, AdjacentBiomeID, Allowed) VALUES (@BiomeID, @AdjacentBiomeID, @Allowed)";
            using (var cmd = new SQLiteCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@BiomeID", biomeId);
                cmd.Parameters.AddWithValue("@AdjacentBiomeID", adjacentBiomeId);
                cmd.Parameters.AddWithValue("@Allowed", allowed);
                cmd.ExecuteNonQuery();
            }
        }
    }

    private static bool BiomeExists(SQLiteConnection connection, int id)
    {
        string query = "SELECT COUNT(*) FROM Biomes WHERE ID = @ID";
        using (var cmd = new SQLiteCommand(query, connection))
        {
            cmd.Parameters.AddWithValue("@ID", id);
            return (long)cmd.ExecuteScalar() > 0;
        }
    }

    private static bool CommodityExists(SQLiteConnection connection, int id)
    {
        string query = "SELECT COUNT(*) FROM Commodities WHERE CommodityID = @ID";
        using (var cmd = new SQLiteCommand(query, connection))
        {
            cmd.Parameters.AddWithValue("@ID", id);
            return (long)cmd.ExecuteScalar() > 0;
        }
    }

    private static bool BiomeCommodityExists(SQLiteConnection connection, int biomeId, int commodityId)
    {
        string query = "SELECT COUNT(*) FROM BiomeCommodities WHERE BiomeID = @BiomeID AND CommodityID = @CommodityID";
        using (var cmd = new SQLiteCommand(query, connection))
        {
            cmd.Parameters.AddWithValue("@BiomeID", biomeId);
            cmd.Parameters.AddWithValue("@CommodityID", commodityId);
            return (long)cmd.ExecuteScalar() > 0;
        }
    }

    private static bool TradingPostExists(SQLiteConnection connection, int id)
    {
        string query = "SELECT COUNT(*) FROM TradingPosts WHERE ID = @ID";
        using (var cmd = new SQLiteCommand(query, connection))
        {
            cmd.Parameters.AddWithValue("@ID", id);
            return (long)cmd.ExecuteScalar() > 0;
        }
    }

    private static bool BiomeAdjacencyExists(SQLiteConnection connection, int biomeId, int adjacentBiomeId)
    {
        string query = "SELECT COUNT(*) FROM BiomeAdjacency WHERE BiomeID = @BiomeID AND AdjacentBiomeID = @AdjacentBiomeID";
        using (var cmd = new SQLiteCommand(query, connection))
        {
            cmd.Parameters.AddWithValue("@BiomeID", biomeId);
            cmd.Parameters.AddWithValue("@AdjacentBiomeID", adjacentBiomeId);
            return (long)cmd.ExecuteScalar() > 0;
        }
    }

    public static List<Biome> LoadBiomesFromDatabase(string dbPath)
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

    public static List<TradingPost> LoadTradingPosts(SQLiteConnection connection)
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

    public static Dictionary<int, bool> LoadAdjacencyRules(SQLiteConnection connection, int biomeID)
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

    public static List<string> LoadCommoditiesForBiome(SQLiteConnection connection, int biomeID)
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
