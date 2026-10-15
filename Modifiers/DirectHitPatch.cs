using HarmonyLib;
using UnityEngine;

namespace CllcExtended.Modifiers;

/// <summary>
/// Reacts to damage once it is applied, on the client that owns the target (a player's own
/// client, a creature's owner): the target's Deathward and Reflection, then the attacker's
/// Vampiric and afflictions for direct creature hits that removed health.
/// </summary>
[HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
internal static class DirectHitPatch
{
    [HarmonyPrefix]
    private static void Prefix(Character __instance, ZNetView ___m_nview, out float __state)
    {
        __state = ___m_nview != null && ___m_nview.IsOwner() ? __instance.GetHealth() : -1f;
    }

    [HarmonyPostfix]
    private static void Postfix(Character __instance, HitData hit, float __state)
    {
        if (__state <= 0f)
        {
            return;
        }

        var healthLost = Mathf.Clamp(__state - __instance.GetHealth(), 0f, __state);
        var attacker = hit.GetAttacker();
        var targetModifiers = __instance.IsPlayer() ? CreatureModifiers.None : ModifierStore.Get(__instance);

        if ((targetModifiers & CreatureModifiers.Reflection) != 0 && attacker is Player player && !hit.m_ranged && healthLost > 0f)
        {
            DefenseModifiers.TryReflect(__instance, player, healthLost);
        }

        // Before death is checked (next fixed update), so a lethal hit can still be cancelled.
        if ((targetModifiers & CreatureModifiers.Deathward) != 0 && __instance.GetHealth() <= 0f)
        {
            DefenseModifiers.TryDeathward(__instance);
        }

        // EnemyHit is a direct creature attack (melee, area or projectile): damage over time and
        // the environment are excluded.
        if (hit.m_hitType != HitData.HitType.EnemyHit || healthLost <= 0f || attacker == null || attacker.IsDead())
        {
            return;
        }

        var modifiers = ModifierStore.Get(attacker);
        if ((modifiers & CreatureModifiers.Vampiric) != 0)
        {
            VampiricModifier.OnDirectHit(attacker, healthLost);
        }

        if (__instance is Player target)
        {
            Afflictions.OnDirectHit(target, modifiers);
        }
    }
}
