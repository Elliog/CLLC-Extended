using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CllcExtended.Inspection;

/// <summary>TextMeshPro rich text (French or English) for the nameplate lines and the chat explanation.</summary>
internal static class InsightFormatter
{
    private const string ResistColor = "#8fd18f";
    private const string WeakColor = "#ff8a65";
    private const string ImmuneColor = "#b0bec5";
    private const string TraitColor = "#ffd54f";
    private const string Separator = " · ";

    /// <summary>Short lines shown under the nameplate while crouching: one line per damage group, then modifier names.</summary>
    internal static string Compact(CreatureInsight insight)
    {
        // The star count is on the nameplate itself (see Ui.StarCounter).
        var lines = new List<string>(DamageParts(insight.Damage));
        if (insight.Traits.Count > 0)
        {
            lines.Add(Colored(TraitColor, string.Join(Separator, insight.Traits.Select(trait => trait.Name))));
        }

        return string.Join("\n", lines);
    }

    /// <summary>One chat line per item: header, each damage group, then each trait explained.</summary>
    internal static IEnumerable<string> Detailed(CreatureInsight insight)
    {
        var stars = insight.Stars > 0 ? " " + StarCount(insight.Stars) : string.Empty;
        yield return Colored(TraitColor, $"<b>{insight.Name}{stars}</b>");
        foreach (var part in DamageParts(insight.Damage))
        {
            yield return part;
        }

        if (insight.Traits.Count == 0)
        {
            yield return Loc.T("Aucun modificateur.", "No modifier.");
            yield break;
        }

        foreach (var trait in insight.Traits)
        {
            yield return $"• {Colored(TraitColor, trait.Name)}{Loc.T(" : ", ": ")}{trait.Description}";
        }
    }

    private static IEnumerable<string> DamageParts(DamageProfile damage)
    {
        var parts = new List<string>();
        if (!IsNeutral(damage.Overall))
        {
            parts.Add(Colored(damage.Overall < 1f ? ResistColor : WeakColor, $"{Loc.T("Dégâts subis", "Damage taken")} {Delta(damage.Overall)}"));
        }

        AddGroup(parts, Loc.T("Immunisé", "Immune"), ImmuneColor, damage.Where(factor => factor <= 0f), damage, withDelta: false);
        AddGroup(parts, Loc.T("Résiste", "Resists"), ResistColor, damage.Where(factor => factor > 0f && factor < 1f && !IsNeutral(factor)), damage, withDelta: true);
        AddGroup(parts, Loc.T("Faible", "Weak"), WeakColor, damage.Where(factor => factor > 1f && !IsNeutral(factor)), damage, withDelta: true);

        if (parts.Count == 0)
        {
            parts.Add(Loc.T("Aucune résistance ni faiblesse", "No resistance or weakness"));
        }

        return parts;
    }

    private static void AddGroup(
        List<string> parts,
        string label,
        string color,
        IEnumerable<DamageKind> kinds,
        DamageProfile damage,
        bool withDelta)
    {
        var items = kinds.Select(kind => withDelta ? $"{Name(kind)} {Delta(damage[kind])}" : Name(kind)).ToList();
        if (items.Count > 0)
        {
            parts.Add(Colored(color, $"{label}{Loc.T(" : ", ": ")}{string.Join(", ", items)}"));
        }
    }

    private static bool IsNeutral(float factor) => Math.Abs(factor - 1f) < 0.005f;

    // 0.5 -> "-50 %", 2 -> "+100 %".
    private static string Delta(float factor)
    {
        var percent = (factor - 1f) * 100f;
        var sign = percent > 0f ? "+" : "-";
        return sign + Math.Abs(percent).ToString("0", CultureInfo.InvariantCulture) + Loc.T(" %", "%");
    }

    private static string StarCount(int stars) => $"★{stars}";

    private static string Colored(string color, string text) => $"<color={color}>{text}</color>";

    // Names used by the French and English versions of the game.
    internal static string Name(DamageKind kind) =>
        kind switch
        {
            DamageKind.Blunt => Loc.T("Contondant", "Blunt"),
            DamageKind.Slash => Loc.T("Tranchant", "Slash"),
            DamageKind.Pierce => Loc.T("Perforant", "Pierce"),
            DamageKind.Fire => Loc.T("Feu", "Fire"),
            DamageKind.Frost => Loc.T("Givre", "Frost"),
            DamageKind.Lightning => Loc.T("Foudre", "Lightning"),
            DamageKind.Poison => "Poison",
            DamageKind.Spirit => Loc.T("Esprit", "Spirit"),
            _ => kind.ToString(),
        };
}
