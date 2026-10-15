using BepInEx.Configuration;
using HarmonyLib;

namespace CllcExtended.Modifiers;

/// <summary>Values of the offensive modifiers (CreatureManager defaults).</summary>
internal static class OffenseModifiers
{
    private static ConfigEntry<float> _furiousBonus = null!;
    private static ConfigEntry<float> _armorIgnored = null!;
    private static ConfigEntry<float> _staggerBonus = null!;
    private static ConfigEntry<float> _undodgeableDamageReduction = null!;

    internal static void Bind(ConfigBinder file)
    {
        _furiousBonus = ModifierValues.Percent(file, "Furious damage bonus (%)", 15f, 200f, "Extra damage dealt by Furious creatures.");
        _armorIgnored = ModifierValues.Percent(file, "Armor Piercing armor ignored (%)", 30f, 100f, "Share of the player's armor ignored by Armor Piercing creatures.");
        _staggerBonus = ModifierValues.Percent(file, "Staggering stagger bonus (%)", 60f, 500f, "Extra stagger caused by Staggering creatures.");
        _undodgeableDamageReduction = ModifierValues.Percent(file, "Undodgeable damage reduction (%)", 25f, 100f, "Damage Undodgeable attacks lose, the price of ignoring dodge.");
    }

    internal static float FuriousBonus => _furiousBonus.Value / 100f;
    internal static float ArmorIgnored => _armorIgnored.Value / 100f;
    internal static float StaggerBonus => _staggerBonus.Value / 100f;
    internal static float UndodgeableDamageReduction => _undodgeableDamageReduction.Value / 100f;

    // Set while a player's own client processes a hit from an Armor Piercing creature.
    internal static bool ArmorPiercingHit;
}

/// <summary>
/// Changes a creature's hit before it is sent: <see cref="Character.Damage"/> runs on the
/// attacker's client (the creature owner), then the hit goes to the target's owner.
/// </summary>
[HarmonyPatch(typeof(Character), nameof(Character.Damage))]
internal static class OutgoingHitPatch
{
    [HarmonyPrefix]
    private static void Prefix(Character __instance, HitData hit)
    {
        var attacker = hit.GetAttacker();
        if (attacker == null || attacker.IsPlayer())
        {
            return;
        }

        var modifiers = ModifierStore.Get(attacker);
        if (modifiers == CreatureModifiers.None)
        {
            return;
        }

        if ((modifiers & CreatureModifiers.Furious) != 0)
        {
            hit.m_damage.Modify(1f + OffenseModifiers.FuriousBonus);
        }

        if ((modifiers & CreatureModifiers.Staggering) != 0)
        {
            hit.m_staggerMultiplier *= 1f + OffenseModifiers.StaggerBonus;
        }

        if ((modifiers & CreatureModifiers.Reaping) != 0)
        {
            hit.m_damage.Modify(1f + Reaping.DamageBonus(attacker));
        }

        if (__instance is not Player)
        {
            return;
        }

        if ((modifiers & CreatureModifiers.Undodgeable) != 0)
        {
            hit.m_dodgeable = false;
            hit.m_damage.Modify(1f - OffenseModifiers.UndodgeableDamageReduction);
        }

        if ((modifiers & CreatureModifiers.Juggernaut) != 0)
        {
            Juggernaut.ApplyPush(attacker, hit);
        }
    }
}

/// <summary>Armor Piercing: the player's armor counts for less against the current hit.</summary>
[HarmonyPatch(typeof(Player), nameof(Player.GetBodyArmor))]
internal static class ArmorPiercingPatch
{
    [HarmonyPostfix]
    private static void Postfix(ref float __result)
    {
        if (OffenseModifiers.ArmorPiercingHit)
        {
            __result *= 1f - OffenseModifiers.ArmorIgnored;
        }
    }
}
