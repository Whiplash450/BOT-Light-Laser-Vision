using System.Reflection;
using BOT_Light_Laser_Vision.Configuration;
using BOT_Light_Laser_Vision.Models;
using BOT_Light_Laser_Vision.Services;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace BOT_Light_Laser_Vision.Patches;

public class LookSensorLightPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(LookSensor), nameof(LookSensor.CheckLookSimple));
    }

    [PatchPrefix]
    private static bool PatchPrefix(LookSensor __instance, Player from, Player to, ref bool __result)
    {
        if (!ModConfig.ModEnabled.Value || from == null || to == null)
        {
            return true;
        }

        BotOwner bot = from.AIData?.BotOwner;
        if (bot == null)
        {
            return true;
        }

        // 1. If bot is currently suffering from NVG tube shutdown blackout, line of sight fails
        if (LightDetectionService.IsBotBlind(bot.Id))
        {
            __result = false;
            return false; // Skip vanilla vision check while blinded
        }

        return true;
    }

    [PatchPostfix]
    private static void PatchPostfix(LookSensor __instance, Player from, Player to, ref bool __result)
    {
        if (!ModConfig.ModEnabled.Value || from == null || to == null)
        {
            return;
        }

        // Only evaluate interactions between AI bots and human player
        if (to.IsAI)
        {
            return;
        }

        BotOwner bot = from.AIData?.BotOwner;
        if (bot == null)
        {
            return;
        }

        // Evaluate NVG Profile
        NVGProfile nvgProfile = NVGProfile.EvaluateBotNVG(bot);

        // 2. Weather & NVG Distance Clamping for Ambient Sight
        if (ModConfig.NvgDistanceScalingEnabled.Value && nvgProfile.Tier != NVGQualityTier.None)
        {
            float dist = Vector3.Distance(from.Position, to.Position);

            // Severe rain / snow storm clamps ambient sight down to 15m
            if (AmbientLightService.IsSevereWeatherActive())
            {
                if (dist > ModConfig.RainSnowNvgDistanceCap.Value && !LightDetectionService.IsAnyDeviceActive())
                {
                    __result = false;
                    return;
                }
            }
            else if (dist > nvgProfile.AmbientVisionDistance && !LightDetectionService.IsAnyDeviceActive())
            {
                __result = false;
                return;
            }
        }

        // 3. Process Light and Laser Reaction (Lighthouse beacon, transverse beams, cone alerts)
        LightDetectionService.EvaluateBotLightReaction(bot, to, nvgProfile);
    }
}
