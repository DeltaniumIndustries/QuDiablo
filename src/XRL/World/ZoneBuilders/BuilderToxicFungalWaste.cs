using XRL.Rules;
using XRL.World;
using XRL.World.ZoneBuilders;

public class ToxicFungalWastes : ZoneBuilderSandbox
{
    private static FastNoise TerrainNoise;
    private static readonly string[] FungalSporeGasTypes =
{
        "FungalSporeGasLuminous",
        "FungalSporeGasLuminous80",
        "FungalSporeGasMumbles",
        "FungalSporeGasMumbles80",
        "FungalSporeGasPuff",
        "FungalSporeGasPuff80",
        "FungalSporeGasWax",
        "FungalSporeGasWax80"
    };
    private static readonly string[] SafeMushroomTypes =
{
        "Brightshroom",
        "Hoarshroom",
    };
    private static readonly string[] SporeMushroomTypes =
{
        "FungusPuffer1",
        "FungusPuffer2",
        "FungusPuffer3",
        "FungusPuffer4",
        "LaurusFungusPufferVoidspore",
        "LaurusFungusPufferBlightcap",
        "LaurusFungusPufferBurnspore",
        "LaurusFungusPufferMooncap",
        "LaurusFungusPufferDreamroot"
    };
    private static readonly string[] Weeps =
{
        "acidLichen Minor",
        "asphaltLichen Minor",
        "saltLichen Minor",
        "slimeLichen Minor",
    };

    private static double[,] ZoneNoise;

    private const int MaxWidth = 1200;
    private const int MaxHeight = 375;
    private const int SPORE_CHANCE = 20;
    private const int ACID_POOL_CHANCE = 5;
    private const int RELIC_CHANCE = 2;

    public bool Underground = true;

    // === 🌍 SERIALIZATION METHODS ===
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

    // === 🌍 ZONE GENERATION ===
    public bool BuildZone(Zone Z)
    {
        EnsureNoiseInitialized();

        int offsetX = (Z.wX * 240 + Z.X * 80) % MaxWidth;
        int offsetY = (Z.wY * 75 + Z.Y * 25) % MaxHeight;

        PopulateZone(Z, offsetX, offsetY);

        // Add unique environmental effect
        //Z.GetCell(0, 0).AddObject("Poisonous Air Effect");

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
        TerrainNoise.SetFrequency(RandomUtils.NextFloat(0.08f, 0.12f));
        TerrainNoise.SetFractalType(FastNoise.FractalType.FBM);
        TerrainNoise.SetFractalOctaves(RandomUtils.NextInt(2, 4));
        TerrainNoise.SetFractalLacunarity(RandomUtils.NextFloat(0.6f, 0.8f));
        TerrainNoise.SetFractalGain(RandomUtils.NextFloat(1f, 1.1f));

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
        if (noiseValue >= 0.75)
        {
            if (SPORE_CHANCE.in100()) Z.GetCell(x, y).AddObject(GetRandomFungalSporeGas());
            if (ACID_POOL_CHANCE.in100()) Z.GetCell(x, y).AddObject("AcidPool");
        }
        else if (noiseValue >= 0.55)
        {
            Z.GetCell(x, y).AddObject(GetRandomMushroomPuffOrWeep());
            if (10.in100()) Z.GetCell(x, y).AddObject("Mycelial Horror");
        }
        else if (noiseValue >= 0.35)
        {
            Z.GetCell(x, y).AddObject("Toxic Sludge");
            if (5.in100()) Z.GetCell(x, y).AddObject("Sporeling Swarm");
        }
    }

    public static string GetRandomFungalSporeGas()
    {
        return FungalSporeGasTypes[RandomUtils.NextInt(FungalSporeGasTypes.Length)];
    }
    public static string GetRandomMushroomPuffOrWeep()
    {
        int roll = RandomUtils.NextInt(1, 20);
        string[] source = roll <= 15 ? SafeMushroomTypes
                        : roll <= 19 ? SporeMushroomTypes
                        : Weeps;

        return source[RandomUtils.NextInt(source.Length)];
    }


    /// <summary>
    /// Randomly places ancient relics.
    /// </summary>
    private void ApplyRandomRelic(Zone Z, int x, int y)
    {
        if (RELIC_CHANCE.in100())
        {
            Z.GetCell(x, y).AddObject("Ancient Relic");
        }
    }
}
