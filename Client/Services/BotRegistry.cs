using System.Collections.Generic;
using EFT;

namespace BOT_Light_Laser_Vision.Services;

public static class BotRegistry
{
    private static readonly HashSet<BotOwner> _activeBots = new(64);
    private static readonly object _lock = new();

    public static IReadOnlyCollection<BotOwner> ActiveBots
    {
        get
        {
            lock (_lock)
            {
                return _activeBots;
            }
        }
    }

    public static void Register(BotOwner bot)
    {
        if (bot == null) return;

        lock (_lock)
        {
            _activeBots.Add(bot);
        }
    }

    public static void Unregister(BotOwner bot)
    {
        if (bot == null) return;

        lock (_lock)
        {
            _activeBots.Remove(bot);
        }
    }

    public static void Clear()
    {
        lock (_lock)
        {
            _activeBots.Clear();
        }
    }
}
