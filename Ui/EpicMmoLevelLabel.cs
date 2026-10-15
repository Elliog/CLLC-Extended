using UnityEngine;

namespace CllcExtended.Ui;

/// <summary>
/// Hides the creature level label WackyEpicMMOSystem attaches under the nameplate. EpicMMO creates
/// it from two places: one honours its "Mob Level UI String" setting, the other (its
/// EnemyHud.UpdateHuds postfix) hard-codes "[level]" and re-activates it every frame, so the
/// setting alone cannot hide it. Display only: EpicMMO levels and XP are untouched.
/// </summary>
internal static class EpicMmoLevelLabel
{
    internal const string PluginGuid = "WackyMole.EpicMMOSystem";

    // EpicMMO instantiates the "Name" text under itself, hence the default clone name.
    private const string LabelName = "Name(Clone)";

    internal static void Hide(GameObject gui)
    {
        // Other mods that copy the nameplate text (BetrValheim's "BV_text") copy the label with it,
        // so look under every direct child of the hud, not only under "Name".
        foreach (Transform child in gui.transform)
        {
            var label = child.Find(LabelName);
            if (label != null && label.gameObject.activeSelf)
            {
                label.gameObject.SetActive(false);
            }
        }
    }
}
