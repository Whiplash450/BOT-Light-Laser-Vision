using System.Collections.Generic;
using BOT_Light_Laser_Vision.Configuration;
using EFT;
using UnityEngine;

namespace BOT_Light_Laser_Vision.Services;

public static class TriangulationService
{
    private class BotCueTracking
    {
        public int CueCount;
        public float LastCueTime;
        public Vector3 LastEstimatedPosition;
    }

    private static readonly Dictionary<int, BotCueTracking> TrackingCache = new(64);

    public static void RegisterSensoryCue(BotOwner bot, int cueWeight = 1)
    {
        if (bot == null) return;

        int botId = bot.Id;
        if (!TrackingCache.TryGetValue(botId, out var state))
        {
            state = new BotCueTracking();
            TrackingCache[botId] = state;
        }

        // Decay old cues if more than 8 seconds have passed
        if (Time.time - state.LastCueTime > 8.0f)
        {
            state.CueCount = 0;
        }

        state.CueCount = Mathf.Min(state.CueCount + cueWeight, 5);
        state.LastCueTime = Time.time;
    }

    public static Vector3 CalculateEstimatedPosition(BotOwner bot, Player player, Vector3 referencePoint)
    {
        if (bot == null || player == null)
        {
            return player != null ? player.Position : Vector3.zero;
        }

        Vector3 botEyePos = bot.MyHead != null ? bot.MyHead.position : bot.Position + Vector3.up * 1.6f;
        Vector3 playerPos = player.Position;
        float trueDistance = Vector3.Distance(botEyePos, playerPos);

        // Determine bot tier (PMC / Raider / Boss vs Scav)
        bool isEliteBot = bot.Profile?.Info?.Settings?.Role == WildSpawnType.pmcBEAR
                          || bot.Profile?.Info?.Settings?.Role == WildSpawnType.pmcUSEC
                          || bot.Profile?.Info?.Settings?.Role == WildSpawnType.pmcBot
                          || bot.Profile?.Info?.Settings?.Role == WildSpawnType.exUsec
                          || bot.Profile?.Info?.Settings?.Role == WildSpawnType.bossBully
                          || bot.Profile?.Info?.Settings?.Role == WildSpawnType.bossKnight;

        // 1. Base Angular Error (Degrees)
        float baseAngularError = isEliteBot ? 10.0f : 30.0f;

        // 2. Base Depth Error Percentage
        float baseDepthErrorPercent;
        if (trueDistance < 25.0f)
        {
            baseDepthErrorPercent = ModConfig.DepthErrorScaleShort.Value;
        }
        else if (trueDistance < 60.0f)
        {
            baseDepthErrorPercent = ModConfig.DepthErrorScaleMedium.Value;
        }
        else
        {
            baseDepthErrorPercent = isEliteBot ? ModConfig.DepthErrorScaleMedium.Value : ModConfig.DepthErrorScaleLong.Value;
        }

        // 3. Multi-Sensory Cue Accumulation Discount
        int botId = bot.Id;
        int activeCues = 0;
        if (TrackingCache.TryGetValue(botId, out var cueState))
        {
            if (Time.time - cueState.LastCueTime <= 8.0f)
            {
                activeCues = cueState.CueCount;
            }
        }

        if (ModConfig.MultiSensoryBoostEnabled.Value && activeCues > 0)
        {
            float discountFactor = 1.0f / (1.0f + 0.35f * activeCues);
            baseAngularError *= discountFactor;
            baseDepthErrorPercent *= discountFactor;
        }

        // 4. Random angular offset around Y axis
        float randomAngle = Random.Range(-baseAngularError, baseAngularError);
        Vector3 dirToPlayer = (playerPos - referencePoint).normalized;
        Quaternion rotation = Quaternion.Euler(0f, randomAngle, 0f);
        Vector3 offsetDir = rotation * dirToPlayer;

        // 5. Random depth offset along distance vector
        float randomDepthOffset = Random.Range(-baseDepthErrorPercent, baseDepthErrorPercent);
        float estimatedDistance = trueDistance * Mathf.Max(0.2f, 1.0f + randomDepthOffset);

        // Calculate estimated world position
        Vector3 estimatedPos = referencePoint + offsetDir * estimatedDistance;
        estimatedPos.y = playerPos.y; // Keep roughly aligned with ground height

        if (cueState != null)
        {
            cueState.LastEstimatedPosition = estimatedPos;
        }

        return estimatedPos;
    }

    public static void InjectSearchPosition(BotOwner bot, Player player, Vector3 estimatedPosition)
    {
        if (bot?.BotsGroup == null) return;

        // Add suspicious investigation point to the squad's native search system
        bot.BotsGroup.AddPointToSearch(estimatedPosition, 10.0f, bot, true);
    }

    public static void ClearBot(int botId)
    {
        TrackingCache.Remove(botId);
    }
}
