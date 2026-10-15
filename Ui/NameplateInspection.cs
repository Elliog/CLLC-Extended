using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using CllcExtended.Inspection;

namespace CllcExtended.Ui;

/// <summary>
/// Adds modifier adjectives to the name, a star counter above 5 stars and modifier icons after the
/// stars, and hides EpicMMO's level label on every creature hud, and
/// while the local player crouches, shows the compact inspection line under the nameplate of the
/// creature they aim at, below its star rows. The label is a new text object styled like the
/// nameplate; cloning the nameplate would also clone what other mods attach to it (EpicMMO's level).
/// </summary>
[HarmonyPatch(typeof(EnemyHud), "UpdateHuds")]
internal static class NameplateInspection
{
    private const string LabelName = "CllcxInspection";
    private const float LabelScale = 0.7f;
    private const float LabelWidth = 600f;
    private const float RefreshSeconds = 0.5f;

    private const float GapBelowStars = 4f;
    private const float FallbackOffsetBelowHealth = 24f;

    // EnemyHud keeps its huds in a private Dictionary<Character, HudData>, HudData being a private class.
    private static readonly FieldInfo HudsField = AccessTools.Field(typeof(EnemyHud), "m_huds");
    private static readonly Type HudDataType = AccessTools.Inner(typeof(EnemyHud), "HudData");
    private static readonly FieldInfo GuiField = AccessTools.Field(HudDataType, "m_gui");
    private static readonly FieldInfo NameField = AccessTools.Field(HudDataType, "m_name");

    private static Character? _cachedTarget;
    private static string _cachedText = string.Empty;
    private static float _cachedAt;

    // After EpicMMO and CLLC, which re-activate their level label and star row every frame.
    [HarmonyPostfix]
    [HarmonyAfter(EpicMmoLevelLabel.PluginGuid, Integration.CllcBridge.PluginGuid)]
    private static void Postfix(EnemyHud __instance)
    {
        var plugin = CllcExtendedPlugin.Instance;
        if (plugin?.Reader == null || HudsField.GetValue(__instance) is not IDictionary huds)
        {
            return;
        }

        var settings = plugin.Settings;
        var target = settings.CrouchInspection.Value ? CrouchTarget() : null;
        foreach (DictionaryEntry entry in huds)
        {
            if (GuiField.GetValue(entry.Value) is not GameObject gui)
            {
                continue;
            }

            var stars = default(StarLayout);
            if (gui.activeSelf && entry.Key is Character character)
            {
                var nameText = (TextMeshProUGUI)NameField.GetValue(entry.Value);
                if (settings.NameQualifiers.Value)
                {
                    NameQualifiers.Apply(character, nameText);
                }

                stars = StarCounter.Apply(gui, character.GetLevel(), nameText, settings.MaxStarsShown.Value);
                ModifierIconRow.Apply(gui, character, stars.RightEdge, settings.IconStyle);
                if (settings.HideEpicMmoLevel.Value)
                {
                    EpicMmoLevelLabel.Hide(gui);
                }
            }

            var show = target != null && ReferenceEquals(entry.Key, target) && gui.activeSelf;
            var label = gui.transform.Find(LabelName);
            if (show)
            {
                // Unity overloads == for destroyed objects, so no ??= here.
                if (label == null)
                {
                    label = CreateLabel(gui, (TextMeshProUGUI)NameField.GetValue(entry.Value));
                }

                label.GetComponent<TextMeshProUGUI>().text = TextFor(target!, plugin.Reader);
                PlaceBelowStars(label, gui, stars.Rows);
                label.gameObject.SetActive(true);
            }
            else if (label != null)
            {
                label.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>The hud object EnemyHud currently shows for <paramref name="character"/>, if any.</summary>
    internal static GameObject? FindHud(Character character)
    {
        if (EnemyHud.instance == null || HudsField.GetValue(EnemyHud.instance) is not IDictionary huds || !huds.Contains(character))
        {
            return null;
        }

        return GuiField.GetValue(huds[character]) as GameObject;
    }

    private static Character? CrouchTarget()
    {
        var player = Player.m_localPlayer;
        if (player == null || !player.IsCrouching())
        {
            return null;
        }

        var creature = player.GetHoverCreature();
        return creature != null && !creature.IsPlayer() && !creature.IsDead() ? creature : null;
    }

    private static string TextFor(Character target, CreatureInsightReader reader)
    {
        if (!ReferenceEquals(target, _cachedTarget) || Time.time - _cachedAt > RefreshSeconds)
        {
            _cachedTarget = target;
            _cachedText = InsightFormatter.Compact(reader.Read(target));
            _cachedAt = Time.time;
        }

        return _cachedText;
    }

    private static Transform CreateLabel(GameObject gui, TextMeshProUGUI nameText)
    {
        var label = new GameObject(LabelName, typeof(RectTransform));
        var rect = (RectTransform)label.transform;
        rect.SetParent(gui.transform, false);
        rect.sizeDelta = new Vector2(LabelWidth, 0f);
        // Top-centre pivot: the first line sits at the computed position, extra lines grow downward.
        rect.pivot = new Vector2(0.5f, 1f);

        var text = label.AddComponent<TextMeshProUGUI>();
        text.font = nameText.font;
        text.fontSharedMaterial = nameText.fontSharedMaterial;
        text.fontSize = nameText.fontSize * LabelScale;
        text.color = Color.white;
        text.richText = true;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.alignment = TextAlignmentOptions.Top;
        return rect;
    }

    private static void PlaceBelowStars(Transform label, GameObject gui, int starRows)
    {
        var firstStarRow = gui.transform.Find("level_3");
        if (firstStarRow == null)
        {
            // Boss and other huds have no regular star row: fall back to the health bar.
            var health = gui.transform.Find("Health");
            var healthY = health != null ? health.localPosition.y : 0f;
            label.localPosition = new Vector3(0f, healthY - FallbackOffsetBelowHealth, 0f);
            return;
        }

        var rowTop = firstStarRow.localPosition.y + StarCounter.StarSize / 2f;
        label.localPosition = new Vector3(0f, rowTop - starRows * StarCounter.StarSize - GapBelowStars, 0f);
    }
}
