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

        float dist = Vector3.Distance(from.Position, to.Position);

        // 2. Weather & NVG Distance Clamping for Bots with NVG
        if (ModConfig.NvgDistanceScalingEnabled.Value && nvgProfile.Tier != NVGQualityTier.None)
        {
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
        // 3. Naked Eye, Mounted Rogue, and Stationary Weapon Night Vision Clamping
        else if (nvgProfile.Tier == NVGQualityTier.None && AmbientLightService.IsNighttime())
        {
            bool isRogue = bot.Profile?.Info?.Settings?.Role == WildSpawnType.exUsec;
            bool isMounted = IsMountedOnStationaryWeapon(bot, from);

            // A. Hard ceiling for mounted Rogues at night (100m cap)
            if (isRogue && isMounted)
            {
                float rogueCap = ModConfig.MountedRogueVisionCap.Value;
                if (dist > rogueCap)
                {
                    __result = false;
                    return;
                }
            }
            // B. General stationary weapon night distance clamp for standard bots
            else if (isMounted && ModConfig.StationaryWeaponNightClampingEnabled.Value)
            {
                float stationaryCap = ModConfig.StationaryWeaponNightDistanceCap.Value;
                if (dist > stationaryCap)
                {
                    __result = false;
                    return;
                }
            }

            // C. Comparative ambient light check for naked-eye vision (scales with moonlight & glare)
            if (ModConfig.EnableComparativeLightCheck.Value && !LightDetectionService.IsAnyDeviceActive())
            {
                float baseCap = ModConfig.NakedEyeNightDistanceCap.Value;
                float hardCeiling = (isRogue && isMounted) ? ModConfig.MountedRogueVisionCap.Value : 85.0f;

                float maxAllowedSight = AmbientLightService.CalculateEffectiveSightDistance(
                    from.Position,
                    to.Position,
                    baseCap,
                    hardCeiling
                );

                if (dist > maxAllowedSight)
                {
                    __result = false;
                    return;
                }
            }
        }

        // 4. Process Light and Laser Reaction (Lighthouse beacon, transverse beams, cone alerts)
        LightDetectionService.EvaluateBotLightReaction(bot, to, nvgProfile);
    }

    private static bool IsMountedOnStationaryWeapon(BotOwner bot, Player player)
    {
        if (player == null)
        {
            return false;
        }

        try
        {
            if (player.OnHisWayToOperateStationaryWeapon)
            {
                return true;
            }

            if (player.HandsController != null)
            {
                string controllerName = player.HandsController.GetType().Name;
                if (controllerName.IndexOf("Stationary", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                if (player.HandsController is Player.ItemHandsController itemHands && itemHands.Item != null)
                {
                    string itemName = itemHands.Item.ShortName?.ToLower() ?? string.Empty;
                    string templateId = itemHands.Item.TemplateId.ToString();
                    if (templateId == "5d52cc5a86f77405c45388c4" || templateId == "5d52cc6b86f77405c45388c7" ||
                        itemName.Contains("nsv") || itemName.Contains("ags") || itemName.Contains("utes"))
                    {
                        return true;
                    }
                }
            }
        }
        catch
        {
            // Fall back
        }

        return false;
    }
}
