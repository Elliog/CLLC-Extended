using HarmonyLib;
using UnityEngine;

namespace CllcExtended.Modifiers;

/// <summary>
/// Registers the afflictions' status effects, so SEMan can add them by hash. ObjectDB is created
/// in the main menu and in game (where it copies the menu's list), hence both patches.
/// </summary>
[HarmonyPatch]
internal static class ModifierStatusEffects
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ObjectDB), "Awake")]
    private static void AfterAwake(ObjectDB __instance) => Register(__instance);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
    private static void AfterCopy(ObjectDB __instance) => Register(__instance);

    private static void Register(ObjectDB db)
    {
        foreach (var status in Afflictions.Statuses)
        {
            if (db.GetStatusEffect(status.Hash) != null)
            {
                continue;
            }

            var effect = ScriptableObject.CreateInstance<SE_Stats>();
            effect.name = status.InternalName;
            effect.m_name = status.Name;
            effect.m_tooltip = status.Tooltip;
            effect.m_ttl = Afflictions.Duration;
            effect.m_icon = ModifierIcons.For(status.Modifier);
            status.Configure(effect);
            db.m_StatusEffects.Add(effect);
        }
    }
}
