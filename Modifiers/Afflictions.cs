using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace CllcExtended.Modifiers;

/// <summary>
/// Afflictions: a creature's direct hit that removes a player's health has a chance to put a
/// timed status effect on that player (CreatureManager defaults). Most effects use the game's own
/// SE_Stats fields; Exposed, Withered and Corrosive are applied by the patches below.
/// </summary>
internal static class Afflictions
{
    private static ConfigEntry<float> _procChance = null!;
    private static ConfigEntry<float> _duration = null!;
    private static ConfigEntry<float> _exposedDamageTaken = null!;
    private static ConfigEntry<float> _weakenedDamageDealt = null!;
    private static ConfigEntry<float> _witheredHealing = null!;
    private static ConfigEntry<float> _cripplingMovement = null!;
    private static ConfigEntry<float> _disruptiveRegen = null!;
    private static ConfigEntry<float> _adrenalineRemoved = null!;
    private static ConfigEntry<float> _adrenalineGain = null!;
    private static ConfigEntry<float> _corrosiveWear = null!;

    internal static void Bind(ConfigBinder file)
    {
        _procChance = ModifierValues.Percent(file, "Affliction proc chance (%)", 50f, 100f, "Chance for each affliction to apply on a direct hit that removes health.");
        _duration = ModifierValues.Number(file, "Affliction duration (s)", 5f, 1f, 60f, "Duration of the afflictions' status effects.", relog: true);
        _exposedDamageTaken = ModifierValues.Percent(file, "Exposing extra damage taken (%)", 20f, 200f, "Extra damage taken while Exposed.");
        _weakenedDamageDealt = ModifierValues.Percent(file, "Weakening damage dealt reduction (%)", 20f, 100f, "Damage reduction while Weakened.", relog: true);
        _witheredHealing = ModifierValues.Percent(file, "Withering healing reduction (%)", 50f, 100f, "Healing reduction while Withered.");
        _cripplingMovement = ModifierValues.Percent(file, "Crippling speed and jump reduction (%)", 50f, 100f, "Speed and jump reduction while Crippled.", relog: true);
        _disruptiveRegen = ModifierValues.Percent(file, "Disruptive regeneration reduction (%)", 50f, 100f, "Stamina and eitr regeneration reduction while Disrupted.", relog: true);
        _adrenalineRemoved = ModifierValues.Percent(file, "Adrenaline Drain adrenaline removed (%)", 50f, 100f, "Share of the current adrenaline removed when Adrenaline Drain applies.");
        _adrenalineGain = ModifierValues.Percent(file, "Adrenaline Drain gain reduction (%)", 50f, 100f, "Adrenaline gain reduction while Drained.", relog: true);
        _corrosiveWear = ModifierValues.Percent(file, "Corrosive extra wear (%)", 50f, 500f, "Extra equipment wear while Corroded.");
    }

    internal static float ProcChance => _procChance.Value / 100f;
    internal static float Duration => _duration.Value;
    internal static float ExposedDamageTaken => _exposedDamageTaken.Value / 100f;
    internal static float WeakenedDamageDealt => _weakenedDamageDealt.Value / 100f;
    internal static float WitheredHealing => _witheredHealing.Value / 100f;
    internal static float CripplingMovement => _cripplingMovement.Value / 100f;
    internal static float DisruptiveRegen => _disruptiveRegen.Value / 100f;
    internal static float AdrenalineRemoved => _adrenalineRemoved.Value / 100f;
    internal static float AdrenalineGain => _adrenalineGain.Value / 100f;
    internal static float CorrosiveWear => _corrosiveWear.Value / 100f;

    /// <summary>Status effect put on the player, named in French.</summary>
    internal sealed class Status
    {
        private readonly string _frenchName;
        private readonly string _englishName;
        private readonly Func<string> _frenchTooltip;
        private readonly Func<string> _englishTooltip;

        internal Status(
            CreatureModifiers modifier,
            (string Name, Func<string> Tooltip) french,
            (string Name, Func<string> Tooltip) english,
            Action<SE_Stats> configure)
        {
            Modifier = modifier;
            (_frenchName, _frenchTooltip) = french;
            (_englishName, _englishTooltip) = english;
            Configure = configure;
            InternalName = "cllcx_" + modifier;
            Hash = InternalName.GetStableHashCode();
        }

        internal CreatureModifiers Modifier { get; }
        internal string Name => Loc.T(_frenchName, _englishName);
        internal string Tooltip => Loc.French ? _frenchTooltip() : _englishTooltip();
        internal Action<SE_Stats> Configure { get; }
        internal string InternalName { get; }
        internal int Hash { get; }
    }

    internal static readonly Status[] Statuses =
    {
        new(CreatureModifiers.Exposed,
            ("Exposé", () => $"Tu subis {ModifierCatalog.P(ExposedDamageTaken)} de dégâts en plus."),
            ("Exposed", () => $"You take {ModifierCatalog.P(ExposedDamageTaken)} more damage."),
            _ => { }),
        new(CreatureModifiers.Weakening,
            ("Affaibli", () => $"Tu infliges {ModifierCatalog.P(WeakenedDamageDealt)} de dégâts en moins."),
            ("Weakened", () => $"You deal {ModifierCatalog.P(WeakenedDamageDealt)} less damage."),
            se =>
        {
            se.m_modifyAttackSkill = Skills.SkillType.All;
            se.m_damageModifier = 1f - WeakenedDamageDealt;
        }),
        new(CreatureModifiers.Withered,
            ("Flétri", () => $"Tes soins sont réduits de {ModifierCatalog.P(WitheredHealing)}."),
            ("Withered", () => $"Healing you receive is reduced by {ModifierCatalog.P(WitheredHealing)}."),
            _ => { }),
        new(CreatureModifiers.Crippling,
            ("Entravé", () => $"Ta vitesse et ton saut sont réduits de {ModifierCatalog.P(CripplingMovement)}."),
            ("Crippled", () => $"Your speed and jump are reduced by {ModifierCatalog.P(CripplingMovement)}."),
            se =>
        {
            se.m_speedModifier = -CripplingMovement;
            se.m_jumpModifier = new Vector3(0f, -CripplingMovement, 0f);
        }),
        new(CreatureModifiers.Disruptive,
            ("Épuisé", () => $"Ta régénération d'endurance et d'eitr est réduite de {ModifierCatalog.P(DisruptiveRegen)}."),
            ("Disrupted", () => $"Your stamina and eitr regeneration is reduced by {ModifierCatalog.P(DisruptiveRegen)}."),
            se =>
        {
            se.m_staminaRegenMultiplier = 1f - DisruptiveRegen;
            se.m_eitrRegenMultiplier = 1f - DisruptiveRegen;
        }),
        new(CreatureModifiers.AdrenalineDrain,
            ("Drainé", () => $"Tes gains d'adrénaline sont réduits de {ModifierCatalog.P(AdrenalineGain)}."),
            ("Drained", () => $"Your adrenaline gains are reduced by {ModifierCatalog.P(AdrenalineGain)}."),
            se =>
        {
            se.m_adrenalineModifier = -AdrenalineGain;
        }),
        new(CreatureModifiers.Corrosive,
            ("Corrodé", () => $"Ton équipement s'use {ModifierCatalog.P(CorrosiveWear)} plus vite."),
            ("Corroded", () => $"Your equipment wears {ModifierCatalog.P(CorrosiveWear)} faster."),
            _ => { }),
    };

    private static readonly Dictionary<CreatureModifiers, Status> ByModifier = Index();

    internal static string DescribeFr(string effect) =>
        $"Chaque coup direct a {ModifierCatalog.P(ProcChance)} de chances {effect} pendant {Duration:0} s.";

    internal static string DescribeEn(string effect) =>
        $"Each direct hit has a {ModifierCatalog.P(ProcChance)} chance to {effect} for {Duration:0} s.";

    internal static bool Has(Player player, CreatureModifiers modifier) =>
        ByModifier.TryGetValue(modifier, out var status) && player.GetSEMan().HaveStatusEffect(status.Hash);

    /// <summary>Rolls each affliction of the attacker on the hit player (player's own client).</summary>
    internal static void OnDirectHit(Player player, CreatureModifiers attackerModifiers)
    {
        foreach (var status in Statuses)
        {
            if ((attackerModifiers & status.Modifier) == 0 || UnityEngine.Random.value >= ProcChance)
            {
                continue;
            }

            player.GetSEMan().AddStatusEffect(status.Hash, resetTime: true);
            if (status.Modifier == CreatureModifiers.AdrenalineDrain)
            {
                player.AddAdrenaline(-player.GetAdrenaline() * AdrenalineRemoved);
            }
        }
    }

    private static Dictionary<CreatureModifiers, Status> Index()
    {
        var index = new Dictionary<CreatureModifiers, Status>();
        foreach (var status in Statuses)
        {
            index.Add(status.Modifier, status);
        }

        return index;
    }
}

/// <summary>Withered: heals received by the player (food regeneration included) are reduced.</summary>
[HarmonyPatch(typeof(Character), "RPC_Heal")]
internal static class WitheredHealPatch
{
    [HarmonyPrefix]
    private static void Prefix(Character __instance, ref float hp)
    {
        if (__instance is Player player && Afflictions.Has(player, CreatureModifiers.Withered))
        {
            hp *= 1f - Afflictions.WitheredHealing;
        }
    }
}

/// <summary>
/// Corrosive: while it lasts, every durability loss of an equipped item is increased. Durability
/// is watched each frame on the local player, which covers weapons, tools and armor alike.
/// </summary>
[HarmonyPatch(typeof(Player), "Update")]
internal static class CorrosiveDurabilityPatch
{
    private static readonly Dictionary<ItemDrop.ItemData, float> LastDurability = new();

    [HarmonyPostfix]
    private static void Postfix(Player __instance)
    {
        if (__instance != Player.m_localPlayer)
        {
            return;
        }

        if (!Afflictions.Has(__instance, CreatureModifiers.Corrosive))
        {
            LastDurability.Clear();
            return;
        }

        foreach (var item in __instance.GetInventory().GetEquippedItems())
        {
            if (LastDurability.TryGetValue(item, out var last) && item.m_durability < last)
            {
                item.m_durability = Mathf.Max(0f, item.m_durability - (last - item.m_durability) * Afflictions.CorrosiveWear);
            }

            LastDurability[item] = item.m_durability;
        }
    }
}
