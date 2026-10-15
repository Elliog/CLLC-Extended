using System.Collections.Generic;
using System.Text;
using TMPro;
using CllcExtended.Modifiers;

namespace CllcExtended.Ui;

/// <summary>
/// Adds this add-on's modifiers to the nameplate as adjectives, placed like CLLC's own: after the
/// name in French ("Troll ardent curieux vampirique"), before it in English ("Vampiric Curious
/// Fire-Infused Troll"). CLLC (or the game without it) rewrites the name every frame before this
/// runs, so the adjectives are added again each frame instead of accumulating.
/// </summary>
internal static class NameQualifiers
{
    // Same feminine creatures as CLLC's French translation, so adjectives agree with its own.
    private static readonly HashSet<string> FeminineCreatures = new()
    {
        "Greydwarf_Elite",
        "Leech",
        "Bonemass",
        "Neck",
        "BlobTar",
    };

    private static readonly StringBuilder Words = new();

    internal static void Apply(Character creature, TextMeshProUGUI nameText)
    {
        var modifiers = ModifierStore.Get(creature);
        if (modifiers == CreatureModifiers.None)
        {
            return;
        }

        var feminine = FeminineCreatures.Contains(creature.name.Replace("(Clone)", string.Empty));
        Words.Clear();
        foreach (var definition in ModifierCatalog.All)
        {
            if ((modifiers & definition.Modifier) != 0)
            {
                Words.Append(Words.Length > 0 ? " " : string.Empty).Append(definition.Adjective(feminine));
            }
        }

        var words = Words.ToString();
        if (Loc.French)
        {
            if (!nameText.text.EndsWith(" " + words))
            {
                nameText.text += " " + words;
            }
        }
        else if (!nameText.text.StartsWith(words + " "))
        {
            nameText.text = words + " " + nameText.text;
        }
    }
}
