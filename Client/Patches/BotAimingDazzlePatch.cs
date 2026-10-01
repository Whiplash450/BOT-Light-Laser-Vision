using System.Reflection;
using BOT_Light_Laser_Vision.Configuration;
using BOT_Light_Laser_Vision.Services;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace BOT_Light_Laser_Vision.Patches;

public class BotAimingDazzlePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.PropertyGetter(typeof(BotAimingClass), nameof(BotAimingClass.EndTargetPoint));
    }

    [PatchPostfix]
    private static void PatchPostfix(BotAimingClass __instance, ref Vector3 __result)
    {
        if (!ModConfig.ModEnabled.Value || !ModConfig.DynamicBlindnessEnabled.Value || __instance?.BotOwner_0 == null)
        {
            return;
        }

        BotOwner bot = __instance.BotOwner_0;
        if (LightDetectionService.IsBotDazzled(bot.Id, out float durationRemaining))
        {
            float baseMultiplier = ModConfig.DazzleSpreadMultiplier.Value;
            // Decay spread smoothly over the duration
            float currentSpread = (baseMultiplier - 1.0f) * Mathf.Clamp01(durationRemaining / 2.0f);
            
            // Apply 3D aim dispersion offset
            Vector3 scatterOffset = Random.insideUnitSphere * currentSpread;
            __result += scatterOffset;
        }
    }
}
