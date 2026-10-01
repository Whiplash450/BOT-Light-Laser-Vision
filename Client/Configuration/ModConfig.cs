using BepInEx.Configuration;

namespace BOT_Light_Laser_Vision.Configuration;

public static class ModConfig
{
    public static ConfigEntry<bool> ModEnabled { get; private set; }
    public static ConfigEntry<bool> FlashlightReactionEnabled { get; private set; }
    public static ConfigEntry<bool> LaserReactionEnabled { get; private set; }
    public static ConfigEntry<bool> RelativeBrightnessGatingEnabled { get; private set; }
    public static ConfigEntry<float> ContrastThreshold { get; private set; }
    
    // Beacon & Transverse Laser
    public static ConfigEntry<bool> ExtendedBeaconEnabled { get; private set; }
    public static ConfigEntry<float> MaxBeaconRange { get; private set; }
    public static ConfigEntry<bool> TransverseLaserEnabled { get; private set; }
    public static ConfigEntry<float> MaxTransverseRangeTopTier { get; private set; }
    public static ConfigEntry<float> MaxTransverseRangeBottomTier { get; private set; }

    // NVG Tiers & Weather
    public static ConfigEntry<bool> NvgDistanceScalingEnabled { get; private set; }
    public static ConfigEntry<float> RainSnowNvgDistanceCap { get; private set; }

    // Dazzle & Blackout
    public static ConfigEntry<bool> DynamicBlindnessEnabled { get; private set; }
    public static ConfigEntry<float> NvgTubeBlackoutMinSeconds { get; private set; }
    public static ConfigEntry<float> NvgTubeBlackoutMaxSeconds { get; private set; }
    public static ConfigEntry<float> DirectFaceDazzleRange { get; private set; }
    public static ConfigEntry<float> DazzleSpreadMultiplier { get; private set; }

    // Triangulation & Accuracy
    public static ConfigEntry<bool> TriangulationEnabled { get; private set; }
    public static ConfigEntry<float> DepthErrorScaleShort { get; private set; }
    public static ConfigEntry<float> DepthErrorScaleMedium { get; private set; }
    public static ConfigEntry<float> DepthErrorScaleLong { get; private set; }
    public static ConfigEntry<bool> MultiSensoryBoostEnabled { get; private set; }
    
    // Performance & Debug
    public static ConfigEntry<int> MaxRaycastsPerFrame { get; private set; }
    public static ConfigEntry<bool> DebugLogging { get; private set; }

    public static void Init(ConfigFile config)
    {
        ModEnabled = config.Bind("1. General", "ModEnabled", true, "Enable or disable BOT-Light-Laser-Vision mod.");
        FlashlightReactionEnabled = config.Bind("1. General", "FlashlightReactionEnabled", true, "Enable bot reaction to weapon flashlights.");
        LaserReactionEnabled = config.Bind("1. General", "LaserReactionEnabled", true, "Enable bot reaction to visible and IR lasers.");
        DebugLogging = config.Bind("1. General", "DebugLogging", false, "Enable verbose debug logs in console.");

        RelativeBrightnessGatingEnabled = config.Bind("2. Ambient & Daytime", "RelativeBrightnessGatingEnabled", true, "Gate detection by contrast against ambient light (sunlight washes out beams).");
        ContrastThreshold = config.Bind("2. Ambient & Daytime", "ContrastThreshold", 1.25f, "Minimum relative contrast ratio required for bot alert.");

        ExtendedBeaconEnabled = config.Bind("3. Night Vision & Beacons", "ExtendedBeaconEnabled", true, "Active emitters stand out like lighthouses through NVGs up to max beacon range.");
        MaxBeaconRange = config.Bind("3. Night Vision & Beacons", "MaxBeaconRange", 150.0f, "Maximum distance at which NVG wearers spot active lights and lasers.");
        TransverseLaserEnabled = config.Bind("3. Night Vision & Beacons", "TransverseLaserEnabled", true, "Bots with NVGs see laser beams passing across their field of view.");
        MaxTransverseRangeTopTier = config.Bind("3. Night Vision & Beacons", "MaxTransverseRangeTopTier", 100.0f, "Max transverse beam detection range for Gen 3+ GPNVG-18.");
        MaxTransverseRangeBottomTier = config.Bind("3. Night Vision & Beacons", "MaxTransverseRangeBottomTier", 75.0f, "Max transverse beam detection range for Gen 1 Soviet NVGs.");

        NvgDistanceScalingEnabled = config.Bind("4. NVG Tiers & Weather", "NvgDistanceScalingEnabled", true, "Scale bot ambient night vision distance by NVG quality.");
        RainSnowNvgDistanceCap = config.Bind("4. NVG Tiers & Weather", "RainSnowNvgDistanceCap", 15.0f, "Severely cut ambient night vision range during active rain or snow storms.");

        DynamicBlindnessEnabled = config.Bind("5. Dazzle & NVG Blackout", "DynamicBlindnessEnabled", true, "Enable dynamic blindness and bright source protection tube cutoff.");
        NvgTubeBlackoutMinSeconds = config.Bind("5. Dazzle & NVG Blackout", "NvgTubeBlackoutMinSeconds", 0.75f, "Minimum duration of tube shutdown when struck directly by laser.");
        NvgTubeBlackoutMaxSeconds = config.Bind("5. Dazzle & NVG Blackout", "NvgTubeBlackoutMaxSeconds", 1.50f, "Maximum duration of tube shutdown for budget NVGs.");
        DirectFaceDazzleRange = config.Bind("5. Dazzle & NVG Blackout", "DirectFaceDazzleRange", 15.0f, "Maximum distance for direct flashlight ocular dazzle.");
        DazzleSpreadMultiplier = config.Bind("5. Dazzle & NVG Blackout", "DazzleSpreadMultiplier", 2.5f, "Aim dispersion multiplier applied to dazzled bots.");

        TriangulationEnabled = config.Bind("6. Triangulation & Search", "TriangulationEnabled", true, "Bots estimate shooter position via reverse triangulation.");
        DepthErrorScaleShort = config.Bind("6. Triangulation & Search", "DepthErrorScaleShort", 0.10f, "Depth error margin at short range (< 25m).");
        DepthErrorScaleMedium = config.Bind("6. Triangulation & Search", "DepthErrorScaleMedium", 0.25f, "Depth error margin at medium range (25-60m).");
        DepthErrorScaleLong = config.Bind("6. Triangulation & Search", "DepthErrorScaleLong", 0.40f, "Depth error margin at long range (> 60m).");
        MultiSensoryBoostEnabled = config.Bind("6. Triangulation & Search", "MultiSensoryBoostEnabled", true, "Gunfire, sprinting, and repeated cues improve triangulation accuracy.");

        MaxRaycastsPerFrame = config.Bind("7. Performance", "MaxRaycastsPerFrame", 2, "Maximum linecasts per frame to maintain high framerates.");
    }
}
