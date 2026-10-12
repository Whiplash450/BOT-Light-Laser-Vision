using EFT.Weather;
using UnityEngine;
using UnityEngine.Rendering;

namespace BOT_Light_Laser_Vision.Services;

public static class AmbientLightService
{
    private static float _lastSunCheckTime;
    private static float _cachedSunIntensity = 0.1f;
    private static bool _cachedSevereWeather = false;
    private static float _lastWeatherCheckTime;

    public static float GetAmbientLuminanceAt(Vector3 position)
    {
        // Sample Unity Light Probes at position
        LightProbes.GetInterpolatedProbe(position, null, out SphericalHarmonicsL2 probe);
        
        // Primary L0 coefficient represents omnidirectional ambient irradiance
        float r = Mathf.Max(0f, probe[0, 0]);
        float g = Mathf.Max(0f, probe[1, 0]);
        float b = Mathf.Max(0f, probe[2, 0]);
        float probeLuminance = (r * 0.299f + g * 0.587f + b * 0.114f);

        // Sample TOD Sky intensity for outdoor areas
        float sunIntensity = GetCurrentSunIntensity();

        return Mathf.Max(probeLuminance, sunIntensity * 0.8f);
    }

    public static float GetCurrentSunIntensity()
    {
        if (Time.time < _lastSunCheckTime + 1.0f)
        {
            return _cachedSunIntensity;
        }

        _lastSunCheckTime = Time.time;
        if (TOD_Sky.Instance != null)
        {
            _cachedSunIntensity = Mathf.Clamp01(TOD_Sky.Instance.LightIntensity);
        }
        else
        {
            _cachedSunIntensity = 0.1f;
        }

        return _cachedSunIntensity;
    }

    public static bool IsSevereWeatherActive()
    {
        if (Time.time < _lastWeatherCheckTime + 2.0f)
        {
            return _cachedSevereWeather;
        }

        _lastWeatherCheckTime = Time.time;
        _cachedSevereWeather = false;

        try
        {
            WeatherController weather = WeatherController.Instance;
            if (weather?.WeatherCurve != null)
            {
                if (weather.WeatherCurve.Rain > 0.3f || weather.WeatherCurve.Fog > 0.4f)
                {
                    _cachedSevereWeather = true;
                }
            }
        }
        catch
        {
            _cachedSevereWeather = false;
        }

        return _cachedSevereWeather;
    }

    public static bool IsBeamVisibleAgainstAmbient(float deviceLux, Vector3 targetPos, float threshold)
    {
        float ambientLux = GetAmbientLuminanceAt(targetPos);
        float contrastRatio = deviceLux / (ambientLux + 0.05f);
        return contrastRatio >= threshold;
    }

    public static bool IsNighttime()
    {
        if (TOD_Sky.Instance != null)
        {
            return TOD_Sky.Instance.IsNight;
        }
        return GetCurrentSunIntensity() < 0.20f;
    }

    public static float GetMoonlightIntensity()
    {
        if (TOD_Sky.Instance != null && TOD_Sky.Instance.IsNight)
        {
            return Mathf.Clamp01(TOD_Sky.Instance.LightIntensity);
        }
        return 0f;
    }

    public static float CalculateEffectiveSightDistance(Vector3 botPos, Vector3 targetPos, float baseCap, float maxHardCap)
    {
        if (!IsNighttime())
        {
            return maxHardCap;
        }

        // Sample ambient irradiance (includes moonlight and local artificial light probes)
        float targetLux = GetAmbientLuminanceAt(targetPos);
        float botLux = GetAmbientLuminanceAt(botPos);

        // Target Illumination scaling:
        // Deep darkness (~0.05 lux) stays near baseCap.
        // Ambient moonlight or floodlights (0.15 - 0.40+ lux) scale sight up towards maxHardCap.
        float illuminationFactor = Mathf.Clamp01((targetLux - 0.05f) / 0.35f);
        float sightDist = Mathf.Lerp(baseCap, maxHardCap, illuminationFactor);

        // Observer Glare / Pupil Adaptation:
        // When a bot stands under bright lights (rooftop floodlights) and targets darkness,
        // constricted pupils reduce the bot's ability to resolve dark silhouettes.
        if (botLux > 0.25f && targetLux < botLux)
        {
            float glareRatio = Mathf.Clamp01((botLux - targetLux) / 0.50f);
            sightDist *= (1.0f - glareRatio * 0.40f);
        }

        return Mathf.Clamp(sightDist, baseCap * 0.70f, maxHardCap);
    }
}
