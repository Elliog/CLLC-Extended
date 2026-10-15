using BepInEx.Configuration;
using System;
using HarmonyLib;
using UnityEngine;
using CllcExtended.Inspection;

namespace CllcExtended.Modifiers;

/// <summary>
/// Defensive modifiers (CreatureManager defaults). State that other clients must see in the
/// inspection (Chameleon, Adaptive, Deathward uses) lives on the creature's ZDO.
/// </summary>
internal static class DefenseModifiers
{
    private static ConfigEntry<float> _deathwardHealth = null!;
    private static ConfigEntry<float> _deathwardCooldown = null!;
    private static ConfigEntry<int> _deathwardMaxActivations = null!;
    private static ConfigEntry<float> _reflectionChance = null!;
    private static ConfigEntry<float> _reflectionRatio = null!;
    private static ConfigEntry<float> _reflectionCap = null!;
    private static ConfigEntry<float> _vortexChance = null!;
    private static ConfigEntry<float> _adaptiveReduction = null!;
    private static ConfigEntry<float> _adaptiveDuration = null!;
    private static ConfigEntry<float> _chameleonInterval = null!;

    internal static void Bind(ConfigBinder file)
    {
        _deathwardHealth = ModifierValues.Percent(file, "Deathward restored health (%)", 20f, 100f, "Health a Deathward creature comes back with, in percent of its max health.");
        _deathwardCooldown = ModifierValues.Number(file, "Deathward cooldown (s)", 10f, 0f, 600f, "Seconds before Deathward can trigger again.");
        _deathwardMaxActivations = ModifierValues.Count(file, "Deathward max activations", 3, 1, 50, "How many lethal hits Deathward can cancel.");
        _reflectionChance = ModifierValues.Percent(file, "Reflection chance (%)", 50f, 100f, "Chance to reflect a melee hit.");
        _reflectionRatio = ModifierValues.Percent(file, "Reflection damage reflected (%)", 10f, 100f, "Share of the health lost sent back to the attacker.");
        _reflectionCap = ModifierValues.Number(file, "Reflection max damage", 25f, 0f, 1000f, "Most damage one reflection can deal.");
        _vortexChance = ModifierValues.Percent(file, "Vortex projectile ignore chance (%)", 50f, 100f, "Chance to ignore a projectile entirely.");
        _adaptiveReduction = ModifierValues.Percent(file, "Adaptive damage reduction (%)", 50f, 100f, "Reduction of the remembered damage type.");
        _adaptiveDuration = ModifierValues.Number(file, "Adaptive memory (s)", 5f, 1f, 60f, "How long a damage type is remembered.");
        _chameleonInterval = ModifierValues.Number(file, "Chameleon switch interval (s)", 10f, 1f, 120f, "Seconds between two immunity changes.");
    }

    internal static float DeathwardHealth => _deathwardHealth.Value / 100f;
    internal static float DeathwardCooldown => _deathwardCooldown.Value;
    internal static int DeathwardMaxActivations => _deathwardMaxActivations.Value;
    internal static float ReflectionChance => _reflectionChance.Value / 100f;
    internal static float ReflectionRatio => _reflectionRatio.Value / 100f;
    internal static float ReflectionCap => _reflectionCap.Value;
    internal static float VortexChance => _vortexChance.Value / 100f;
    internal static float AdaptiveReduction => _adaptiveReduction.Value / 100f;
    internal static float AdaptiveDuration => _adaptiveDuration.Value;
    internal static float ChameleonInterval => _chameleonInterval.Value;

    private const string DeathwardUsesKey = "cllcx.deathward.uses";
    private const string DeathwardReadyKey = "cllcx.deathward.ready";
    private const string AdaptiveTypeKey = "cllcx.adaptive.type";
    private const string AdaptiveUntilKey = "cllcx.adaptive.until";
    private const string ChameleonTypeKey = "cllcx.chameleon.type";
    private const string ChameleonNextKey = "cllcx.chameleon.next";
    private const int NoType = -1;

    private static float Now => ZNet.instance != null ? (float)ZNet.instance.GetTimeSeconds() : Time.time;

    // Deathward: called by the owner after damage that brought health to 0 or below.
    internal static void TryDeathward(Character creature)
    {
        var zdo = ModifierStore.Zdo(creature);
        if (zdo == null || zdo.GetInt(DeathwardUsesKey) >= DeathwardMaxActivations || Now < zdo.GetFloat(DeathwardReadyKey))
        {
            return;
        }

        zdo.Set(DeathwardUsesKey, zdo.GetInt(DeathwardUsesKey) + 1);
        zdo.Set(DeathwardReadyKey, Now + DeathwardCooldown);
        var restored = creature.GetMaxHealth() * DeathwardHealth;
        creature.SetHealth(restored);
        DamageText.instance?.ShowText(DamageText.TextType.Heal, creature.GetTopPoint(), restored);
        Effects.Play(creature, Effects.DeathwardTrigger);
    }

    internal static int DeathwardRemaining(Character creature) =>
        DeathwardMaxActivations - (ModifierStore.Zdo(creature)?.GetInt(DeathwardUsesKey) ?? 0);

    // Reflection: the creature's owner sends part of a player's melee damage back to them.
    internal static void TryReflect(Character creature, Player attacker, float healthLost)
    {
        if (UnityEngine.Random.value >= ReflectionChance)
        {
            return;
        }

        var reflected = new HitData
        {
            m_point = attacker.GetCenterPoint(),
            m_dir = (attacker.transform.position - creature.transform.position).normalized,
            m_blockable = false,
            m_dodgeable = false,
        };
        reflected.m_damage.m_damage = Mathf.Min(healthLost * ReflectionRatio, ReflectionCap);
        reflected.SetAttacker(creature);
        attacker.Damage(reflected);
        Effects.Play(creature, Effects.ReflectionTrigger);
    }

    // Adaptive: remembers the dominant type of one hit for a while (without reducing that hit),
    // then reduces matching damage until the memory expires.
    internal static void ApplyAdaptive(Character creature, HitData hit)
    {
        var zdo = ModifierStore.Zdo(creature);
        var dominant = DamageTypes.Dominant(hit.m_damage);
        if (zdo == null || dominant == null)
        {
            return;
        }

        if (Now < zdo.GetFloat(AdaptiveUntilKey))
        {
            if (zdo.GetInt(AdaptiveTypeKey, NoType) == (int)dominant.Value)
            {
                DamageTypes.Scale(ref hit.m_damage, dominant.Value, 1f - AdaptiveReduction);
            }

            return;
        }

        zdo.Set(AdaptiveTypeKey, (int)dominant.Value);
        zdo.Set(AdaptiveUntilKey, Now + AdaptiveDuration);
    }

    internal static DamageKind? CurrentAdaptiveType(Character creature)
    {
        var zdo = ModifierStore.Zdo(creature);
        return zdo != null && Now < zdo.GetFloat(AdaptiveUntilKey) ? TypeOf(zdo, AdaptiveTypeKey) : null;
    }

    // Chameleon: while alerted, immune to one damage type that changes every interval (owner).
    internal static void UpdateChameleon(Character creature, MonsterAI ai)
    {
        var zdo = ModifierStore.Zdo(creature);
        if (zdo == null)
        {
            return;
        }

        if (!ai.IsAlerted())
        {
            if (zdo.GetInt(ChameleonTypeKey, NoType) != NoType)
            {
                zdo.Set(ChameleonTypeKey, NoType);
            }

            return;
        }

        if (Now < zdo.GetFloat(ChameleonNextKey) && zdo.GetInt(ChameleonTypeKey, NoType) != NoType)
        {
            return;
        }

        var current = zdo.GetInt(ChameleonTypeKey, NoType);
        var kinds = DamageProfile.AllKinds;
        var next = (int)kinds[UnityEngine.Random.Range(0, kinds.Length)];
        if (next == current)
        {
            next = (int)kinds[(Array.IndexOf(kinds, (DamageKind)next) + 1) % kinds.Length];
        }

        zdo.Set(ChameleonTypeKey, next);
        zdo.Set(ChameleonNextKey, Now + ChameleonInterval);
    }

    internal static void ApplyChameleon(Character creature, HitData hit)
    {
        if (CurrentChameleonType(creature) is { } immune)
        {
            DamageTypes.Scale(ref hit.m_damage, immune, 0f);
        }
    }

    internal static DamageKind? CurrentChameleonType(Character creature)
    {
        var zdo = ModifierStore.Zdo(creature);
        return zdo != null ? TypeOf(zdo, ChameleonTypeKey) : null;
    }

    private static DamageKind? TypeOf(ZDO zdo, string key)
    {
        var value = zdo.GetInt(key, NoType);
        return value == NoType ? null : (DamageKind)value;
    }
}

/// <summary>Unflinching creatures ignore stagger, from normal hits and from perfect parries.</summary>
[HarmonyPatch]
internal static class UnflinchingPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Character), "AddStaggerDamage")]
    private static bool BeforeStaggerDamage(Character __instance, ref bool __result)
    {
        if (!ModifierStore.Has(__instance, CreatureModifiers.Unflinching))
        {
            return true;
        }

        __result = false;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Character), nameof(Character.Stagger))]
    private static bool BeforeStagger(Character __instance) => !ModifierStore.Has(__instance, CreatureModifiers.Unflinching);
}
