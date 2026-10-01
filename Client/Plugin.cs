using BepInEx;
using BepInEx.Logging;
using BOT_Light_Laser_Vision.Configuration;
using BOT_Light_Laser_Vision.Patches;

namespace BOT_Light_Laser_Vision;

[BepInPlugin("com.ethical.botlightlaservision", "BOT-Light-Laser-Vision", "1.0.0")]
[BepInDependency("xyz.drakia.bigbrain", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("xyz.drakia.waypoints", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("me.sol.sain", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("com.ethical.botbushblocker", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("com.fika.headless", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin
{
    public static ManualLogSource LogSource;
    public static Plugin Instance;

    private void Awake()
    {
        Instance = this;
        LogSource = Logger;

        // Initialize F12 BepInEx configuration
        ModConfig.Init(Config);

        // Enable Harmony patches
        new LookSensorLightPatch().Enable();
        new BotAimingDazzlePatch().Enable();
        new BotSoundStimulusPatch().Enable();
        new BotDeathPatch().Enable();

        LogSource.LogInfo("BOT-Light-Laser-Vision 1.0.0 initialized successfully.");
    }
}
