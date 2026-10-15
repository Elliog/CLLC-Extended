using HarmonyLib;
using UnityEngine;

namespace CllcExtended.Modifiers;

/// <summary>
/// Per-creature RPCs of this plugin, registered on every client when a character loads:
/// visual effects played for everyone, and Reaping gains forwarded to the creature's owner.
/// Effect prefabs are the vanilla ones CreatureManager uses.
/// </summary>
[HarmonyPatch(typeof(Character), "Awake")]
internal static class Effects
{
    internal const string VortexHit = "fx_StaffShield_Hit";
    internal const string DeathwardTrigger = "fx_StaffShield_Break";
    internal const string ReflectionTrigger = "fx_ShieldCharge_5";
    internal const string ReapingGain = "fx_tentaroot_death";
    internal const string BlinkStart = "fx_Adrenaline1";

    private const string EffectRpc = "cllcx_fx";
    internal const string ReapingRpc = "cllcx_reap";

    /// <summary>Plays <paramref name="prefabName"/> at the creature on every client.</summary>
    internal static void Play(Character character, string prefabName)
    {
        var nview = ModifierStore.NView(character);
        if (nview != null && nview.IsValid())
        {
            nview.InvokeRPC(ZNetView.Everybody, EffectRpc, prefabName);
        }
    }

    [HarmonyPostfix]
    private static void Postfix(Character __instance)
    {
        var nview = ModifierStore.NView(__instance);
        if (nview == null || !nview.IsValid() || __instance.IsPlayer())
        {
            return;
        }

        nview.Register<string>(EffectRpc, (_, prefabName) => PlayLocal(__instance, prefabName));
        nview.Register(ReapingRpc, _ => Reaping.Gain(__instance));
    }

    private static void PlayLocal(Character character, string prefabName)
    {
        var prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefabName) : null;
        // Networked prefabs would spawn one synced object per client: only plain effects are played.
        if (character != null && prefab != null && prefab.GetComponent<ZNetView>() == null)
        {
            Object.Instantiate(prefab, character.GetCenterPoint(), Quaternion.identity);
        }
    }
}
