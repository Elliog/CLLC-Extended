using UnityEngine;
using UnityEngine.UI;
using CllcExtended.Modifiers;

namespace CllcExtended.Ui;

/// <summary>
/// Shows one icon per modifier of this plugin, on the star line right after the stars (or the star
/// counter), like CreatureManager does under its nameplates.
/// </summary>
internal static class ModifierIconRow
{
    private const string RowName = "CllcxModifierIcons";
    private const float GapAfterStars = 4f;
    private const float GapBetweenIcons = 2f;
    private const string BackgroundName = "background";
    private const string IconName = "icon";

    internal static void Apply(GameObject gui, Character creature, float starsRight, IconStyle style)
    {
        var size = style.Size;
        var row = gui.transform.Find(RowName);
        var modifiers = size > 0f ? ModifierStore.Get(creature) : CreatureModifiers.None;
        var starLine = gui.transform.Find("level_2");
        if (modifiers == CreatureModifiers.None || starLine == null)
        {
            if (row != null)
            {
                row.gameObject.SetActive(false);
            }

            return;
        }

        if (row == null)
        {
            row = new GameObject(RowName, typeof(RectTransform)).transform;
            row.SetParent(gui.transform, false);
        }

        row.gameObject.SetActive(true);
        row.localPosition = new Vector3(starsRight + GapAfterStars, starLine.localPosition.y, 0f);

        var x = size / 2f;
        foreach (var definition in ModifierCatalog.All)
        {
            var modifier = definition.Modifier;
            var icon = row.Find(modifier.ToString());
            if ((modifiers & modifier) == 0)
            {
                if (icon != null)
                {
                    icon.gameObject.SetActive(false);
                }

                continue;
            }

            if (icon == null)
            {
                icon = CreateSlot(row, modifier);
            }

            icon.gameObject.SetActive(true);
            icon.localPosition = new Vector3(x, 0f, 0f);
            Style(icon, style);
            x += size + GapBetweenIcons;
        }
    }

    // Applied every frame, so config changes show up live.
    private static void Style(Transform slot, IconStyle style)
    {
        var background = slot.Find(BackgroundName).GetComponent<Image>();
        var hasBackground = style.Background != IconBackgroundShape.None;
        background.gameObject.SetActive(hasBackground);
        if (hasBackground)
        {
            background.sprite = IconBackgrounds.For(style.Background);
            background.color = style.BackgroundColor;
            ((RectTransform)background.transform).sizeDelta = new Vector2(style.Size, style.Size);
        }

        var iconSize = style.Size * IconBackgrounds.IconScale(style.Background);
        ((RectTransform)slot.Find(IconName)).sizeDelta = new Vector2(iconSize, iconSize);
    }

    // A slot holds the background then the icon, so the icon is drawn on top.
    private static Transform CreateSlot(Transform row, CreatureModifiers modifier)
    {
        var slot = new GameObject(modifier.ToString(), typeof(RectTransform)).transform;
        slot.SetParent(row, false);
        AddImage(slot, BackgroundName, null);
        AddImage(slot, IconName, ModifierIcons.For(modifier));
        return slot;
    }

    private static void AddImage(Transform parent, string name, Sprite? sprite)
    {
        var child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        var image = child.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }
}

/// <summary>Display settings of the modifier icons.</summary>
internal readonly struct IconStyle
{
    internal IconStyle(float size, IconBackgroundShape background, Color backgroundColor)
    {
        Size = size;
        Background = background;
        BackgroundColor = backgroundColor;
    }

    internal float Size { get; }

    internal IconBackgroundShape Background { get; }

    internal Color BackgroundColor { get; }
}
