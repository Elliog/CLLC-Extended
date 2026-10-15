using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CllcExtended.Modifiers;

/// <summary>
/// Rolls the modifiers of a hostile creature once, on the client that owns it, and stores them on
/// the ZDO so later loads and other clients reuse them. Rolled in Start, one frame after the
/// creature is created, so CLLC has already set its level (needed for "Minimum stars").
/// As in CreatureManager, each group gives at most one modifier: chances of a group add up, a
/// total under 100 % leaves a chance of none, above 100 % they act as weights.
/// </summary>
[HarmonyPatch]
internal static class ModifierRoller
{
    private static readonly ModifierGroup[] Groups = (ModifierGroup[])Enum.GetValues(typeof(ModifierGroup));

    // Humanoid.Start does not call Character.Start, so both are patched.
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(Character), "Start");
        yield return AccessTools.Method(typeof(Humanoid), "Start");
    }

    [HarmonyPostfix]
    private static void Postfix(Character __instance)
    {
        var settings = CllcExtendedPlugin.Instance?.Settings;
        var nview = ModifierStore.NView(__instance);
        var zdo = nview != null ? nview.GetZDO() : null;
        if (settings == null || zdo == null || !nview!.IsOwner() || ModifierStore.IsRolled(zdo) || !settings.ModifiersEnabled.Value)
        {
            return;
        }

        // Players, bosses and passive animals (no MonsterAI) never get modifiers.
        if (__instance.IsPlayer() || __instance.IsBoss() || __instance.GetComponent<MonsterAI>() == null
            || __instance.GetLevel() - 1 < settings.MinimumStars.Value)
        {
            ModifierStore.Set(zdo, CreatureModifiers.None);
            return;
        }

        var rolled = new List<CreatureModifiers>();
        foreach (var group in Groups)
        {
            var modifier = RollGroup(group, settings);
            if (modifier != CreatureModifiers.None)
            {
                rolled.Add(modifier);
            }
        }

        // Over the cap, keep a random subset.
        while (rolled.Count > settings.MaxModifiers.Value)
        {
            rolled.RemoveAt(UnityEngine.Random.Range(0, rolled.Count));
        }

        var modifiers = CreatureModifiers.None;
        foreach (var modifier in rolled)
        {
            modifiers |= modifier;
        }

        ModifierStore.Set(zdo, modifiers);
    }

    /// <summary>One random modifier of <paramref name="group"/> (used by Omen's avenger too), or None.</summary>
    internal static CreatureModifiers RollGroup(ModifierGroup group, PluginConfig settings, bool guaranteed = false)
    {
        var total = 0f;
        foreach (var definition in ModifierCatalog.All)
        {
            if (definition.Group == group)
            {
                total += settings.ChanceOf(definition.Modifier);
            }
        }

        if (total <= 0f)
        {
            return CreatureModifiers.None;
        }

        var roll = UnityEngine.Random.Range(0f, guaranteed ? total : Mathf.Max(100f, total));
        foreach (var definition in ModifierCatalog.All)
        {
            if (definition.Group != group)
            {
                continue;
            }

            roll -= settings.ChanceOf(definition.Modifier);
            if (roll < 0f)
            {
                return definition.Modifier;
            }
        }

        return CreatureModifiers.None;
    }
}
