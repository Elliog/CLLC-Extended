using HarmonyLib;
using UnityEngine;

namespace CllcExtended.Modifiers;

/// <summary>
/// Changes a hit when it reaches the target's owner, before dodge, block, armor and resistances:
/// the target's own modifiers (Vortex, Juggernaut, Chameleon, Adaptive) and, for a player target,
/// Exposed and the attacker's Armor Piercing.
/// </summary>
[HarmonyPatch(typeof(Character), "RPC_Damage")]
internal static class IncomingHitPatch
{
    [HarmonyPrefix]
    private static bool Prefix(Character __instance, HitData hit, ZNetView ___m_nview)
    {
        if (___m_nview == null || !___m_nview.IsOwner())
        {
            return true;
        }

        if (__instance is Player player)
        {
            if (Afflictions.Has(player, CreatureModifiers.Exposed))
            {
                hit.m_damage.Modify(1f + Afflictions.ExposedDamageTaken);
            }

            var attacker = hit.GetAttacker();
            OffenseModifiers.ArmorPiercingHit = attacker != null && ModifierStore.Has(attacker, CreatureModifiers.ArmorPiercing);
            return true;
        }

        var modifiers = ModifierStore.Get(__instance);
        if (modifiers == CreatureModifiers.None)
        {
            return true;
        }

        // Vortex skips the whole hit: no damage, push, stagger or status effect.
        if ((modifiers & CreatureModifiers.Vortex) != 0 && hit.m_ranged && Random.value < DefenseModifiers.VortexChance)
        {
            Effects.Play(__instance, Effects.VortexHit);
            return false;
        }

        if ((modifiers & CreatureModifiers.Juggernaut) != 0)
        {
            hit.m_pushForce = 0f;
        }

        if ((modifiers & CreatureModifiers.Chameleon) != 0)
        {
            DefenseModifiers.ApplyChameleon(__instance, hit);
        }

        if ((modifiers & CreatureModifiers.Adaptive) != 0)
        {
            DefenseModifiers.ApplyAdaptive(__instance, hit);
        }

        return true;
    }

    // Runs even if the method throws, so Armor Piercing never leaks to another hit.
    [HarmonyFinalizer]
    private static void Finalizer() => OffenseModifiers.ArmorPiercingHit = false;
}
