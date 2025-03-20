using XRL.Rules;
using XRL.World;
using XRL.World.ZoneBuilders;

public class BlightedChasm : ZoneBuilderSandbox
{
    private static FastNoise TerrainNoise;
    private static double[,] ZoneNoise;

    private const int MaxWidth = 1200;
    private const int MaxHeight = 375;
    
    private const int GAS_POCKET_CHANCE = 10;
    private const int BLIGHTED_SLUDGE_CHANCE = 15;
    private const int RELIC_CHANCE = 5;
    private const int TWISTED_VEGETATION_CHANCE = 12;
    private const int COLLAPSING_TERRAIN_CHANCE = 8;

    public bool Underground = true;

    public static void Save(SerializationWriter Writer)
    {
        if (ZoneNoise == null)
        {
            Writer.Write(0);
            return;
        }

        Writer.Write(1);
        foreach (double value in ZoneNoise)
        {
            Writer.Write(value);
        }
    }

    public static void Load(SerializationReader Reader)
    {
        if (Reader.ReadInt32() == 0)
        {
            ZoneNoise = null;
            return;
        }

        ZoneNoise = new double[MaxWidth, MaxHeight];
        for (int i = 0; i < MaxWidth; i++)
        {
            for (int j = 0; j < MaxHeight; j++)
            {
                ZoneNoise[i, j] = Reader.ReadDouble();
            }
        }
    }

    public bool BuildZone(Zone Z)
    {
        EnsureNoiseInitialized();

        int offsetX = (Z.wX * 240 + Z.X * 80) % MaxWidth;
        int offsetY = (Z.wY * 75 + Z.Y * 25) % MaxHeight;

        PopulateZone(Z, offsetX, offsetY);

        // Add unique environmental effect
        Z.GetCell(0, 0).AddObject("Toxic Miasma Effect");

        // Ensure proper connectivity
        EnsureAllVoidsConnected(Z);

        return true;
    }

    // === 🔥 HELPER METHODS ===

    /// <summary>
    /// Ensures that the noise map is initialized.
    /// </summary>
    private static void EnsureNoiseInitialized()
    {
        if (ZoneNoise != null) return;

        TerrainNoise = new FastNoise();
        TerrainNoise.SetSeed(RandomUtils.GetSeedForTerrainNoise());
        TerrainNoise.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
        TerrainNoise.SetFrequency(0.12f);
        TerrainNoise.SetFractalType(FastNoise.FractalType.FBM);
        TerrainNoise.SetFractalOctaves(4);
        TerrainNoise.SetFractalLacunarity(0.6f);
        TerrainNoise.SetFractalGain(1.2f);

        ZoneNoise = new double[MaxWidth, MaxHeight];

        for (int x = 0; x < MaxWidth; x++)
        {
            for (int y = 0; y < MaxHeight; y++)
            {
                ZoneNoise[x, y] = TerrainNoise.GetNoise(x, y);
            }
        }
    }

    /// <summary>
    /// Populates the given zone with terrain features based on noise values.
    /// </summary>
    private void PopulateZone(Zone Z, int offsetX, int offsetY)
    {
        for (int i = 0; i < Z.Height; i++)
        {
            for (int j = 0; j < Z.Width; j++)
            {
                if (!Z.GetCell(j, i).IsPassable()) continue;

                double noiseValue = ZoneNoise[j + offsetX, i + offsetY];

                ApplyTerrainFeatures(Z, j, i, noiseValue);
                ApplyRandomRelic(Z, j, i);
            }
        }
    }

    /// <summary>
    /// Applies terrain features based on noise value.
    /// </summary>
    private void ApplyTerrainFeatures(Zone Z, int x, int y, double noiseValue)
    {
        if (noiseValue >= 0.80)
        {
            if (BLIGHTED_SLUDGE_CHANCE.in100()) Z.GetCell(x, y).AddObject("Blighted Mire");
            if (GAS_POCKET_CHANCE.in100()) Z.GetCell(x, y).AddObject("LaurusGasBlightedMire");
        }
        else if (noiseValue >= 0.60)
        {
            Z.GetCell(x, y).AddObject("LaurusZTHollowStalk");
            if (TWISTED_VEGETATION_CHANCE.in100()) Z.GetCell(x, y).AddObject("LaurusPlantBleedingThornbush");
        }
        else if (noiseValue >= 0.40)
        {
            Z.GetCell(x, y).AddObject("Chasm Floor");
            if (COLLAPSING_TERRAIN_CHANCE.in100()) Z.GetCell(x, y).AddObject("LaurusZTUnstableGround");
        }
    }

    /// <summary>
    /// Randomly places ancient relics.
    /// </summary>
    private void ApplyRandomRelic(Zone Z, int x, int y)
    {
        if (RELIC_CHANCE.in100())
        {
            Z.GetCell(x, y).AddObject("RelicChest");
        }
    }
}
