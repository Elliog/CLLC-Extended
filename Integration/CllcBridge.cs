using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Configuration;

namespace CllcExtended.Integration;

/// <summary>
/// Read-only access to Creature Level &amp; Loot Control (CLLC) through its public <c>API</c> class.
/// Resolved by reflection so the plugin builds and runs without CLLC: every getter then reports "None".
/// </summary>
internal sealed class CllcBridge
{
    internal const string PluginGuid = "org.bepinex.plugins.creaturelevelcontrol";

    private const string None = "None";

    private readonly MethodInfo? _getExtraEffect;
    private readonly MethodInfo? _getInfusion;
    private readonly MethodInfo? _getBossAffix;
    private readonly ConfigFile? _config;

    private CllcBridge(System.Type? api, ConfigFile? config)
    {
        _getExtraEffect = api?.GetMethod("GetExtraEffectCreature", new[] { typeof(Character) });
        _getInfusion = api?.GetMethod("GetInfusionCreature", new[] { typeof(Character) });
        _getBossAffix = api?.GetMethod("GetAffixBoss", new[] { typeof(Character) });
        _config = config;
    }

    internal bool IsLoaded => _getExtraEffect != null;

    /// <summary>Call once all plugins are loaded (from Start or later), not from Awake.</summary>
    internal static CllcBridge Resolve()
    {
        if (!Chainloader.PluginInfos.TryGetValue(PluginGuid, out var info) || info.Instance == null)
        {
            return new CllcBridge(null, null);
        }

        var api = info.Instance.GetType().Assembly.GetType("CreatureLevelControl.API");
        var bridge = new CllcBridge(api, info.Instance.Config);
        if (!bridge.IsLoaded || bridge._getInfusion == null || bridge._getBossAffix == null)
        {
            UnityEngine.Debug.LogWarning(
                $"[{PluginInfo.Name}] CLLC {info.Metadata.Version} is loaded but its API differs from 5.0: its effects are not shown.");
        }

        return bridge;
    }

    /// <summary>CLLC <c>CreatureExtraEffect</c> name, e.g. "Armored".</summary>
    internal string ExtraEffect(Character creature) => InvokeEnum(_getExtraEffect, creature);

    /// <summary>CLLC <c>CreatureInfusion</c> name, e.g. "Fire".</summary>
    internal string Infusion(Character creature) => InvokeEnum(_getInfusion, creature);

    /// <summary>CLLC <c>BossAffix</c> name, e.g. "Shielded".</summary>
    internal string BossAffix(Character creature) => InvokeEnum(_getBossAffix, creature);

    /// <summary>A numeric CLLC setting, or <paramref name="fallback"/> (the CLLC default) when unavailable.</summary>
    internal float Setting(string section, string key, float fallback)
    {
        if (_config != null && _config.TryGetEntry<float>(section, key, out var entry))
        {
            return entry.Value;
        }

        return fallback;
    }

    private static string InvokeEnum(MethodInfo? method, Character creature)
    {
        if (method == null)
        {
            return None;
        }

        return method.Invoke(null, new object[] { creature })?.ToString() ?? None;
    }
}
