using System.Reflection;
using BOT_Light_Laser_Vision.Configuration;
using BOT_Light_Laser_Vision.Services;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace BOT_Light_Laser_Vision.Patches;

public class BotSoundStimulusPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BotEventHandler), nameof(BotEventHandler.PlaySound));
    }

    [PatchPrefix]
    private static void PatchPrefix(IPlayer player, Vector3 position, ref float power, AISoundType soundType)
    {
        if (!ModConfig.ModEnabled.Value || !ModConfig.MultiSensoryBoostEnabled.Value || player == null || player.IsAI)
        {
            return;
        }

        // Auditory cues reduce triangulation error for bots within hearing range
        int cueWeight = 1;
        if (soundType == AISoundType.gun)
        {
            cueWeight = 3; // Loud unsuppressed gunfire provides maximum acoustic cue
        }
        else if (soundType == AISoundType.silencedGun)
        {
            cueWeight = 2;
        }
        else if (soundType == AISoundType.step)
        {
            cueWeight = 1;
        }

        // Iterate over fast active bot registry (zero scene hierarchy traversal)
        float rangeSqr = power * power;
        foreach (var bot in BotRegistry.ActiveBots)
        {
            if (bot != null && (bot.Position - position).sqrMagnitude <= rangeSqr)
            {
                TriangulationService.RegisterSensoryCue(bot, cueWeight);
            }
        }
    }
}
