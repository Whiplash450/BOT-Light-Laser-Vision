using System;
using System.Collections.Generic;
using BOT_Light_Laser_Vision.Configuration;
using BOT_Light_Laser_Vision.Models;
using EFT;
using UnityEngine;

namespace BOT_Light_Laser_Vision.Services;

public static class LightDetectionService
{
    public struct BotLightState
    {
        public float BlindnessUntil;
        public float DazzleUntil;
        public float NextCheckTime;
        public Vector3 LastInvestigatedPosition;
    }

    private static readonly Dictionary<int, BotLightState> BotStates = new(64);
    private static readonly RaycastHit[] HitBuffer = new RaycastHit[16];

    // Frame raycast budget
    private static int _lastFrame;
    private static int _frameRaycasts;

    // Cached player weapon device state for current frame
    private static int _lastPlayerStateFrame = -1;
    private static bool _isFlashlightOn;
    private static bool _isLaserOn;
    private static bool _isInfrared;
    private static Vector3 _emitterPos;
    private static Vector3 _emitterForward;
    private static Vector3 _laserEndPoint;
    private static Vector3 _laserHitNormal;
    private static bool _hasLaserHit;

    public static bool CanCastRayThisFrame()
    {
        if (Time.frameCount != _lastFrame)
        {
            _lastFrame = Time.frameCount;
            _frameRaycasts = 0;
        }

        return _frameRaycasts < ModConfig.MaxRaycastsPerFrame.Value;
    }

    public static void IncrementRaycastCount()
    {
        _frameRaycasts++;
    }

    public static void UpdatePlayerDeviceState(Player player)
    {
        if (player == null || Time.frameCount == _lastPlayerStateFrame)
        {
            return;
        }

        _lastPlayerStateFrame = Time.frameCount;
        _isFlashlightOn = false;
        _isLaserOn = false;
        _isInfrared = false;
        _hasLaserHit = false;

        _emitterPos = player.WeaponRoot != null ? player.WeaponRoot.position : player.Position + Vector3.up * 1.4f;
        _emitterForward = player.LookDirection;

        // Query active weapon lights
        var lights = player.gameObject.GetComponentsInChildren<Light>(false);
        if (lights != null)
        {
            foreach (var l in lights)
            {
                if (l != null && l.enabled && l.intensity > 0.05f)
                {
                    _isFlashlightOn = true;
                    _emitterPos = l.transform.position;
                    _emitterForward = l.transform.forward;
                    break;
                }
            }
        }

        // Query active weapon lasers via LineRenderer
        var lines = player.gameObject.GetComponentsInChildren<LineRenderer>(false);
        if (lines != null)
        {
            foreach (var line in lines)
            {
                if (line != null && line.enabled && line.gameObject.activeInHierarchy)
                {
                    _isLaserOn = true;
                    _emitterPos = line.transform.position;
                    _emitterForward = line.transform.forward;

                    string name = line.gameObject.name.ToLower();
                    if (name.Contains("ir") || name.Contains("infra"))
                    {
                        _isInfrared = true;
                    }
                    break;
                }
            }
        }

        // If laser is active, trace one single forward raycast for the whole frame
        if (_isLaserOn)
        {
            int mask = LayerMaskClass.HighPolyWithTerrainMask;
            if (Physics.Raycast(_emitterPos, _emitterForward, out RaycastHit hit, 150.0f, mask))
            {
                _laserEndPoint = hit.point;
                _laserHitNormal = hit.normal;
                _hasLaserHit = true;
            }
            else
            {
                _laserEndPoint = _emitterPos + _emitterForward * 150.0f;
                _laserHitNormal = -_emitterForward;
                _hasLaserHit = false;
            }
        }
    }

    public static bool IsAnyDeviceActive()
    {
        return _isFlashlightOn || _isLaserOn;
    }

    public static bool IsBotBlind(int botId)
    {
        if (BotStates.TryGetValue(botId, out var state))
        {
            return Time.time < state.BlindnessUntil;
        }
        return false;
    }

    public static bool IsBotDazzled(int botId, out float durationRemaining)
    {
        durationRemaining = 0f;
        if (BotStates.TryGetValue(botId, out var state))
        {
            if (Time.time < state.DazzleUntil)
            {
                durationRemaining = state.DazzleUntil - Time.time;
                return true;
            }
        }
        return false;
    }

    public static void SetBotBlindness(int botId, float duration)
    {
        if (!BotStates.TryGetValue(botId, out var state))
        {
            state = new BotLightState();
        }
        state.BlindnessUntil = Mathf.Max(state.BlindnessUntil, Time.time + duration);
        BotStates[botId] = state;
    }

    public static void SetBotDazzle(int botId, float duration)
    {
        if (!BotStates.TryGetValue(botId, out var state))
        {
            state = new BotLightState();
        }
        state.DazzleUntil = Mathf.Max(state.DazzleUntil, Time.time + duration);
        BotStates[botId] = state;
    }

    public static bool EvaluateBotLightReaction(BotOwner bot, Player player, NVGProfile nvgProfile)
    {
        if (bot == null || player == null || !ModConfig.ModEnabled.Value)
        {
            return false;
        }

        int botId = bot.Id;

        // 1. Blindness Cutoff Gate: If bot is currently blinded by tube shutdown, line of sight is cut
        if (IsBotBlind(botId))
        {
            return false;
        }

        UpdatePlayerDeviceState(player);

        // 2. State Gate: If devices are off, return immediately with zero cost
        if (!IsAnyDeviceActive())
        {
            return false;
        }

        Vector3 botEyePos = bot.MyHead != null ? bot.MyHead.position : bot.Position + Vector3.up * 1.6f;
        Vector3 botLookDir = bot.LookDirection;
        float distanceToPlayer = Vector3.Distance(botEyePos, _emitterPos);

        // 3. Staggered Check LOD
        if (!BotStates.TryGetValue(botId, out var state))
        {
            state = new BotLightState();
            BotStates[botId] = state;
        }

        if (Time.time < state.NextCheckTime)
        {
            return false;
        }

        // Stagger intervals by distance: 75ms (< 25m), 150ms (25-60m), 250ms (> 60m)
        float staggerInterval = distanceToPlayer < 25f ? 0.075f : (distanceToPlayer < 60f ? 0.15f : 0.25f);
        state.NextCheckTime = Time.time + staggerInterval;
        BotStates[botId] = state;

        // 4. IR Filter: If device is IR and bot has no NVG or is Thermal, bot cannot see it
        if (_isInfrared && (nvgProfile.Tier == NVGQualityTier.None || nvgProfile.IsThermal))
        {
            return false;
        }

        // 5. Flashlight Cone & Direct Illumination Check
        if (_isFlashlightOn && ModConfig.FlashlightReactionEnabled.Value && distanceToPlayer <= 50.0f)
        {
            Vector3 toBot = (bot.Position + Vector3.up * 1.0f - _emitterPos).normalized;
            float coneDot = Vector3.Dot(_emitterForward, toBot);

            // 50 degree total cone angle (25 degrees half angle = cos(25) ~ 0.906)
            if (coneDot > 0.88f)
            {
                // Relative Brightness Gating
                float deviceLux = 50.0f / (1.0f + 0.05f * distanceToPlayer * distanceToPlayer);
                if (!ModConfig.RelativeBrightnessGatingEnabled.Value ||
                    AmbientLightService.IsBeamVisibleAgainstAmbient(deviceLux, bot.Position, ModConfig.ContrastThreshold.Value))
                {
                    // Linecast verification
                    if (CanCastRayThisFrame())
                    {
                        IncrementRaycastCount();
                        int coverMask = LayerMaskClass.HighPolyWithTerrainMask;
                        if (!Physics.Linecast(_emitterPos, botEyePos, coverMask))
                        {
                            // Rule: Naked-eye bot illuminated by non-IR flashlight detects throw/shadow (FOV ignored!)
                            if (!_isInfrared && nvgProfile.Tier == NVGQualityTier.None)
                            {
                                TriangulationService.RegisterSensoryCue(bot, 2);
                                Vector3 estimated = TriangulationService.CalculateEstimatedPosition(bot, player, _emitterPos);
                                TriangulationService.InjectSearchPosition(bot, player, estimated);

                                // Check direct face dazzle if bot happens to be facing light within 15m
                                float facingDot = Vector3.Dot(botLookDir, -toBot);
                                if (facingDot > 0.5f && distanceToPlayer <= ModConfig.DirectFaceDazzleRange.Value)
                                {
                                    SetBotDazzle(botId, 2.0f);
                                }
                                return true;
                            }
                            else if (nvgProfile.Tier != NVGQualityTier.None)
                            {
                                // NVG wearer: Check narrow tube FOV
                                float facingDot = Vector3.Dot(botLookDir, -toBot);
                                float minDot = Mathf.Cos(nvgProfile.FieldOfView * 0.5f * Mathf.Deg2Rad);
                                if (facingDot >= minDot || nvgProfile.HasPeripheralSight)
                                {
                                    TriangulationService.RegisterSensoryCue(bot, 2);
                                    Vector3 estimated = TriangulationService.CalculateEstimatedPosition(bot, player, _emitterPos);
                                    TriangulationService.InjectSearchPosition(bot, player, estimated);

                                    // Intense visible light dazzles NVG users severely
                                    if (facingDot > 0.5f && distanceToPlayer <= ModConfig.DirectFaceDazzleRange.Value)
                                    {
                                        SetBotDazzle(botId, 3.5f);
                                    }
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
        }

        // 6. Direct Laser Strike on Bot (Bright Source Protection Cutoff)
        if (_isLaserOn && _hasLaserHit && ModConfig.LaserReactionEnabled.Value)
        {
            float hitToHeadDist = Vector3.Distance(_laserEndPoint, botEyePos);

            // Direct strike on head/eyes of NVG wearer
            if (hitToHeadDist < 0.35f && nvgProfile.Tier != NVGQualityTier.None && !nvgProfile.IsThermal)
            {
                // Trips BSP circuit: complete vision blackout for duration
                SetBotBlindness(botId, nvgProfile.BlackoutDuration);
                
                // Triggers anonymous alert to take cover
                bot.BotsGroup?.AddPointToSearch(bot.Position, 5.0f, bot, true);
                return false;
            }

            // Laser Dot on nearby surface within bot FOV
            if (hitToHeadDist < 3.0f)
            {
                Vector3 toDot = (_laserEndPoint - botEyePos).normalized;
                float dotFacing = Vector3.Dot(botLookDir, toDot);
                float minDot = Mathf.Cos(nvgProfile.FieldOfView * 0.5f * Mathf.Deg2Rad);

                if (dotFacing >= minDot && CanCastRayThisFrame())
                {
                    IncrementRaycastCount();
                    if (!Physics.Linecast(botEyePos, _laserEndPoint, LayerMaskClass.HighPolyWithTerrainMask))
                    {
                        TriangulationService.RegisterSensoryCue(bot, 1);
                        Vector3 estimated = TriangulationService.CalculateEstimatedPosition(bot, player, _laserEndPoint);
                        TriangulationService.InjectSearchPosition(bot, player, estimated);
                        return true;
                    }
                }
            }
        }

        // 7. Transverse Laser Beam Detection (Passing in Front of Bot up to 75m - 100m)
        if (_isLaserOn && ModConfig.TransverseLaserEnabled.Value && nvgProfile.Tier != NVGQualityTier.None && !nvgProfile.IsThermal)
        {
            float maxBeamRange = nvgProfile.MaxTransverseBeamRange;
            if (distanceToPlayer <= maxBeamRange)
            {
                Vector3 beamVec = _laserEndPoint - _emitterPos;
                float beamLength = beamVec.magnitude;
                if (beamLength > 0.01f)
                {
                    Vector3 beamDir = beamVec / beamLength;
                    float projection = Vector3.Dot(botEyePos - _emitterPos, beamDir);
                    projection = Mathf.Clamp(projection, 0f, beamLength);
                    Vector3 closestPointOnBeam = _emitterPos + beamDir * projection;

                    float distToBeam = Vector3.Distance(botEyePos, closestPointOnBeam);
                    if (distToBeam <= 12.0f)
                    {
                        Vector3 toBeam = (closestPointOnBeam - botEyePos).normalized;
                        float viewDot = Vector3.Dot(botLookDir, toBeam);
                        float minDot = Mathf.Cos(nvgProfile.FieldOfView * 0.5f * Mathf.Deg2Rad);

                        if (viewDot >= minDot)
                        {
                            TriangulationService.RegisterSensoryCue(bot, 1);
                            Vector3 estimated = TriangulationService.CalculateEstimatedPosition(bot, player, closestPointOnBeam);
                            TriangulationService.InjectSearchPosition(bot, player, estimated);
                            return true;
                        }
                    }
                }
            }
        }

        // 8. Lighthouse Beacon Override (Active emitter spotted across long distance 100m - 150m)
        if (ModConfig.ExtendedBeaconEnabled.Value && distanceToPlayer <= ModConfig.MaxBeaconRange.Value)
        {
            Vector3 toEmitter = (_emitterPos - botEyePos).normalized;
            float viewDot = Vector3.Dot(botLookDir, toEmitter);
            float minDot = Mathf.Cos(nvgProfile.FieldOfView * 0.5f * Mathf.Deg2Rad);

            if (viewDot >= minDot && CanCastRayThisFrame())
            {
                IncrementRaycastCount();
                if (!Physics.Linecast(botEyePos, _emitterPos, LayerMaskClass.HighPolyWithTerrainMask))
                {
                    TriangulationService.RegisterSensoryCue(bot, 2);
                    Vector3 estimated = TriangulationService.CalculateEstimatedPosition(bot, player, _emitterPos);
                    TriangulationService.InjectSearchPosition(bot, player, estimated);
                    return true;
                }
            }
        }

        return false;
    }

    public static void ClearBot(int botId)
    {
        BotStates.Remove(botId);
    }
}
