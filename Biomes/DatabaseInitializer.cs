using System;
using System.Data.SQLite;
using System.IO;
using System.Reflection;

public static class DatabaseInitializer
{
    public static void InitializeDatabase(string dbPath)
    {
        using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
        {
            connection.Open();

            // Create tables if they don't exist
            CreateTables(connection);

            // Insert initial data if it doesn't exist
            InsertInitialData(connection);
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

    private static void InsertInitialData(SQLiteConnection connection)
    {
        // Insert biomes
        InsertBiome(connection, 1, "Forest", "#228B22", 1.0);
        InsertBiome(connection, 2, "Desert", "#EDC9AF", 1.5);
        InsertBiome(connection, 3, "Mountain", "#A9A9A9", 2.0);
        InsertBiome(connection, 4, "Swamp", "#556B2F", 1.2);
        InsertBiome(connection, 5, "Tundra", "#ADD8E6", 1.8);
        InsertBiome(connection, 6, "Water", "#1E90FF", 1.0);

        // Insert commodities
        InsertCommodity(connection, 1, "Wood");
        InsertCommodity(connection, 2, "Stone");
        InsertCommodity(connection, 3, "Gold");
        InsertCommodity(connection, 4, "Herbs");
        InsertCommodity(connection, 5, "Ice");
        InsertCommodity(connection, 6, "Fish");

        // Insert biome commodities
        InsertBiomeCommodity(connection, 1, 1); // Forest -> Wood
        InsertBiomeCommodity(connection, 2, 2); // Desert -> Stone
        InsertBiomeCommodity(connection, 3, 3); // Mountain -> Gold
        InsertBiomeCommodity(connection, 4, 4); // Swamp -> Herbs
        InsertBiomeCommodity(connection, 5, 5); // Tundra -> Ice
        InsertBiomeCommodity(connection, 6, 6); // Water -> Fish

        // Insert trading posts
        InsertTradingPost(connection, 1, 1, "Forest Trading Post", 0, 0);
        InsertTradingPost(connection, 2, 2, "Desert Trading Post", 1, 1);
        InsertTradingPost(connection, 3, 3, "Mountain Trading Post", 2, 2);
        InsertTradingPost(connection, 4, 4, "Swamp Trading Post", 3, 3);
        InsertTradingPost(connection, 5, 5, "Tundra Trading Post", 4, 4);
        InsertTradingPost(connection, 6, 6, "Water Trading Post", 5, 5);

        // Insert adjacency rules
        InsertBiomeAdjacency(connection, 1, 2, true); // Forest <-> Desert
        InsertBiomeAdjacency(connection, 1, 3, true); // Forest <-> Mountain
        InsertBiomeAdjacency(connection, 1, 4, true); // Forest <-> Swamp
        InsertBiomeAdjacency(connection, 1, 5, false); // Forest <-> Tundra
        InsertBiomeAdjacency(connection, 1, 6, true); // Forest <-> Water

        InsertBiomeAdjacency(connection, 2, 3, true); // Desert <-> Mountain
        InsertBiomeAdjacency(connection, 2, 4, false); // Desert <-> Swamp
        InsertBiomeAdjacency(connection, 2, 5, true); // Desert <-> Tundra
        InsertBiomeAdjacency(connection, 2, 6, true); // Desert <-> Water

        InsertBiomeAdjacency(connection, 3, 4, true); // Mountain <-> Swamp
        InsertBiomeAdjacency(connection, 3, 5, true); // Mountain <-> Tundra
        InsertBiomeAdjacency(connection, 3, 6, true); // Mountain <-> Water

        InsertBiomeAdjacency(connection, 4, 5, true); // Swamp <-> Tundra
        InsertBiomeAdjacency(connection, 4, 6, true); // Swamp <-> Water

        InsertBiomeAdjacency(connection, 5, 6, true); // Tundra <-> Water
    }

    private static void InsertBiome(SQLiteConnection connection, int id, string name, string color, double baseCost)
    {
        if (!BiomeExists(connection, id))
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

    private static void InsertCommodity(SQLiteConnection connection, int id, string name)
    {
        if (!CommodityExists(connection, id))
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

    private static void InsertBiomeCommodity(SQLiteConnection connection, int biomeId, int commodityId)
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

    private static void InsertTradingPost(SQLiteConnection connection, int id, int biomeId, string name, int x, int y)
    {
        if (!TradingPostExists(connection, id))
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

    private static void InsertBiomeAdjacency(SQLiteConnection connection, int biomeId, int adjacentBiomeId, bool allowed)
    {
        if (!BiomeAdjacencyExists(connection, biomeId, adjacentBiomeId))
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
}
