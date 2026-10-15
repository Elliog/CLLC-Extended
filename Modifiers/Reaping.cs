using BepInEx.Configuration;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace CllcExtended.Modifiers;

/// <summary>
/// Reaping creatures absorb every death around them (CreatureManager defaults): each one heals them
/// (limited number of times) and permanently raises max health, damage and size up to caps. Gains
/// are applied by the reaper's owner and stored on its ZDO; size is applied on every client.
/// </summary>
internal static class Reaping
{
    private static ConfigEntry<float> _radius = null!;
    private static ConfigEntry<float> _healPerKill = null!;
    private static ConfigEntry<int> _healMaxActivations = null!;
    private static ConfigEntry<float> _maxHealthPerKill = null!;
    private static ConfigEntry<float> _maxHealthCap = null!;
    private static ConfigEntry<float> _damagePerKill = null!;
    private static ConfigEntry<float> _damageCap = null!;
    private static ConfigEntry<float> _scalePerKill = null!;
    private static ConfigEntry<float> _scaleCap = null!;

    internal static void Bind(ConfigBinder file)
    {
        _radius = ModifierValues.Number(file, "Reaping radius (m)", 24f, 1f, 100f, "Deaths within this distance feed Reaping creatures.");
        _healPerKill = ModifierValues.Percent(file, "Reaping heal per death (% of max health)", 5f, 100f, "Heal per absorbed death.");
        _healMaxActivations = ModifierValues.Count(file, "Reaping max heals", 20, 0, 1000, "How many absorbed deaths can heal the creature.");
        _maxHealthPerKill = ModifierValues.Percent(file, "Reaping max health per death (%)", 10f, 100f, "Max health gained per absorbed death, in percent of the starting max health.");
        _maxHealthCap = ModifierValues.Number(file, "Reaping max health cap (x)", 2f, 1f, 10f, "Max health never exceeds this multiple of the starting max health.");
        _damagePerKill = ModifierValues.Percent(file, "Reaping damage per death (%)", 1f, 100f, "Damage gained per absorbed death.");
        _damageCap = ModifierValues.Percent(file, "Reaping damage cap (%)", 20f, 500f, "Most damage a Reaping creature can gain.");
        _scalePerKill = ModifierValues.Percent(file, "Reaping size per death (%)", 5f, 100f, "Size gained per absorbed death.");
        _scaleCap = ModifierValues.Percent(file, "Reaping size cap (%)", 50f, 300f, "Most size a Reaping creature can gain.");
    }

    internal static float Radius => _radius.Value;
    internal static float HealPerKill => _healPerKill.Value / 100f;
    internal static int HealMaxActivations => _healMaxActivations.Value;
    internal static float MaxHealthPerKill => _maxHealthPerKill.Value / 100f;
    internal static float MaxHealthCap => _maxHealthCap.Value;
    internal static float DamagePerKill => _damagePerKill.Value / 100f;
    internal static float DamageCap => _damageCap.Value / 100f;
    internal static float ScalePerKill => _scalePerKill.Value / 100f;
    internal static float ScaleCap => _scaleCap.Value / 100f;

    private const string KillsKey = "cllcx.reaping.kills";
    private const string HealsKey = "cllcx.reaping.heals";

    private static readonly List<Character> Nearby = new();
    private static readonly ConditionalWeakTable<Character, StrongBox<Vector3>> BaseScales = new();

    internal static int Kills(Character creature) => ModifierStore.Zdo(creature)?.GetInt(KillsKey) ?? 0;

    internal static float DamageBonus(Character creature) => Mathf.Min(Kills(creature) * DamagePerKill, DamageCap);

    /// <summary>Max health multiplier for a number of absorbed deaths.</summary>
    internal static float MaxHealthFactor(int kills) => Mathf.Min(1f + kills * MaxHealthPerKill, MaxHealthCap);

    /// <summary>Tells every living reaper in range about a death (called by the dead's owner).</summary>
    internal static void NotifyDeath(Character dead, Vector3 position)
    {
        Nearby.Clear();
        Character.GetCharactersInRange(position, Radius, Nearby);
        foreach (var reaper in Nearby)
        {
            if (reaper == dead || reaper.IsDead() || !ModifierStore.Has(reaper, CreatureModifiers.Reaping))
            {
                continue;
            }

            if (ModifierStore.IsOwner(reaper))
            {
                Gain(reaper);
            }
            else
            {
                ModifierStore.NView(reaper).InvokeRPC(Effects.ReapingRpc);
            }
        }
    }

    /// <summary>Applies one absorbed death (reaper's owner only).</summary>
    internal static void Gain(Character reaper)
    {
        var zdo = ModifierStore.Zdo(reaper);
        if (zdo == null || !ModifierStore.IsOwner(reaper) || reaper.IsDead())
        {
            return;
        }

        // Max health without Reaping, as the game (and CLLC) last computed it.
        var kills = zdo.GetInt(KillsKey);
        var baseMaxHealth = reaper.GetMaxHealth() / MaxHealthFactor(kills);
        zdo.Set(KillsKey, ++kills);
        reaper.SetMaxHealth(baseMaxHealth * MaxHealthFactor(kills));

        var heals = zdo.GetInt(HealsKey);
        if (heals < HealMaxActivations)
        {
            zdo.Set(HealsKey, heals + 1);
            reaper.Heal(baseMaxHealth * HealPerKill);
        }

        Effects.Play(reaper, Effects.ReapingGain);
    }

    /// <summary>Keeps the reaper's size in line with its kills, on every client.</summary>
    internal static void ApplyScale(Character creature)
    {
        var kills = Kills(creature);
        if (kills <= 0)
        {
            return;
        }

        var baseScale = BaseScales.GetValue(creature, c => new StrongBox<Vector3>(c.transform.localScale)).Value;
        var target = baseScale * (1f + Mathf.Min(kills * ScalePerKill, ScaleCap));
        if (creature.transform.localScale != target)
        {
            creature.transform.localScale = target;
        }
    }
}

[HarmonyPatch(typeof(Character), nameof(Character.CustomFixedUpdate))]
internal static class ReapingScalePatch
{
    [HarmonyPostfix]
    private static void Postfix(Character __instance)
    {
        if (!__instance.IsPlayer() && ModifierStore.Has(__instance, CreatureModifiers.Reaping))
        {
            Reaping.ApplyScale(__instance);
        }
    }
}

/// <summary>
/// The game recomputes max health when a full-health creature loads and when its level changes
/// (CLLC adds its own factor there): Reaping's gains are applied again on top, by the owner.
/// </summary>
[HarmonyPatch(typeof(Character), "SetupMaxHealth")]
internal static class ReapingMaxHealthPatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(Character __instance)
    {
        var kills = Reaping.Kills(__instance);
        if (kills > 0 && ModifierStore.IsOwner(__instance) && ModifierStore.Has(__instance, CreatureModifiers.Reaping))
        {
            __instance.SetMaxHealth(__instance.GetMaxHealth() * Reaping.MaxHealthFactor(kills));
        }
    }
}
