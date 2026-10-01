using System.Reflection;
using BOT_Light_Laser_Vision.Services;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace BOT_Light_Laser_Vision.Patches;

public class BotDeathPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BotOwner), nameof(BotOwner.Dispose));
    }

    [PatchPostfix]
    private static void PatchPostfix(BotOwner __instance)
    {
        if (__instance == null)
        {
            return;
        }

        int botId = __instance.Id;
        BotRegistry.Unregister(__instance);
        LightDetectionService.ClearBot(botId);
        TriangulationService.ClearBot(botId);
    }
}
