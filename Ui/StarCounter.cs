using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CllcExtended.Ui;

/// <summary>
/// Above the configured number of stars, replaces CLLC's star rows with one star and the star
/// count. The star is a copy of the vanilla one-star row ("level_2") and takes the colour CLLC
/// gives the creature's stars (its effect colour). Must run after CLLC, which re-activates its
/// row every frame.
/// </summary>
internal static class StarCounter
{
    private const string CounterName = "CllcxStarCounter";
    private const string CountTextName = "count";
    private const float CountTextOffsetX = 10f;
    private const float CountTextScale = 0.75f;

    /// <summary>Stars are 16 px wide and tall, centred on their position; CLLC puts 5 per row.</summary>
    internal const float StarSize = 16f;
    private const float StarHalfWidth = StarSize / 2f;

    /// <summary>
    /// Shows the counter when needed and returns where the displayed stars (or counter) end: right
    /// edge in hud coordinates, for the modifier icons, and number of rows, for the inspection line.
    /// </summary>
    internal static StarLayout Apply(GameObject gui, int level, TextMeshProUGUI nameText, int maxStarsShown)
    {
        var stars = level - 1;
        var counter = gui.transform.Find(CounterName);
        var row = stars > maxStarsShown ? gui.transform.Find("level_" + level) : null;
        if (row == null)
        {
            if (counter != null)
            {
                counter.gameObject.SetActive(false);
            }

            return VisibleRow(gui, level);
        }

        // CLLC decides every frame whether the row is visible (its "Visual indicators for level"
        // setting); the counter follows that decision and the row itself is never shown.
        var visible = row.gameObject.activeSelf;
        row.gameObject.SetActive(false);
        if (counter == null)
        {
            if (!visible)
            {
                return StarLayout.Empty(gui);
            }

            counter = Create(gui, nameText);
            if (counter == null)
            {
                return StarLayout.Empty(gui);
            }
        }

        counter.gameObject.SetActive(visible);
        var count = counter.Find(CountTextName).GetComponent<TextMeshProUGUI>();
        count.text = stars.ToString();
        var rowStar = StarImage(row);
        var counterStar = StarImage(counter);
        if (rowStar != null && counterStar != null)
        {
            counterStar.color = rowStar.color;
        }

        return visible
            ? new StarLayout(counter.localPosition.x + CountTextOffsetX + count.preferredWidth, 1)
            : StarLayout.Empty(gui);
    }

    // The visible star row ("level_<level>", vanilla or CLLC): its right edge and how many rows
    // its stars span, or an empty layout when no star is shown.
    private static StarLayout VisibleRow(GameObject gui, int level)
    {
        var row = gui.transform.Find("level_" + level);
        if (row == null || !row.gameObject.activeSelf || row.childCount == 0)
        {
            return StarLayout.Empty(gui);
        }

        float right = float.MinValue, top = float.MinValue, bottom = float.MaxValue;
        foreach (Transform star in row)
        {
            right = Mathf.Max(right, star.localPosition.x);
            top = Mathf.Max(top, star.localPosition.y);
            bottom = Mathf.Min(bottom, star.localPosition.y);
        }

        var rows = 1 + Mathf.RoundToInt((top - bottom) / StarSize);
        return new StarLayout(row.localPosition.x + right + StarHalfWidth, rows);
    }

    internal static float LeftEdge(GameObject gui)
    {
        var oneStarRow = gui.transform.Find("level_2");
        return oneStarRow != null ? oneStarRow.localPosition.x - StarHalfWidth : 0f;
    }

    private static Transform? Create(GameObject gui, TextMeshProUGUI nameText)
    {
        var oneStarRow = gui.transform.Find("level_2");
        if (oneStarRow == null)
        {
            return null;
        }

        var counter = Object.Instantiate(oneStarRow.gameObject, gui.transform).transform;
        counter.name = CounterName;
        counter.localPosition = oneStarRow.localPosition;

        var label = new GameObject(CountTextName, typeof(RectTransform));
        var rect = (RectTransform)label.transform;
        rect.SetParent(counter, false);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(60f, 20f);
        rect.localPosition = new Vector3(CountTextOffsetX, 0f, 0f);

        var text = label.AddComponent<TextMeshProUGUI>();
        text.font = nameText.font;
        text.fontSharedMaterial = nameText.fontSharedMaterial;
        text.fontSize = nameText.fontSize * CountTextScale;
        text.color = Color.white;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.alignment = TextAlignmentOptions.Left;
        return counter;
    }

    // Each star is a container whose first child holds the coloured image (as CLLC colours it).
    private static Image? StarImage(Transform row)
    {
        var star = row.Find("star");
        return star != null && star.childCount > 0 ? star.GetChild(0).GetComponent<Image>() : null;
    }
}

/// <summary>Where the stars shown on a nameplate end.</summary>
internal readonly struct StarLayout
{
    internal StarLayout(float rightEdge, int rows)
    {
        RightEdge = rightEdge;
        Rows = rows;
    }

    internal float RightEdge { get; }

    internal int Rows { get; }

    internal static StarLayout Empty(GameObject gui) => new(StarCounter.LeftEdge(gui), 0);
}
