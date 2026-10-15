using System.Collections.Generic;
using CllcExtended.Inspection;

namespace CllcExtended.Modifiers;

/// <summary>Traits and live damage changes shown by the inspection for this plugin's modifiers.</summary>
internal static class ModifierTraits
{
    internal static void Add(Character creature, DamageProfile damage, List<CreatureTrait> traits)
    {
        var modifiers = ModifierStore.Get(creature);
        if (modifiers == CreatureModifiers.None)
        {
            return;
        }

        foreach (var definition in ModifierCatalog.All)
        {
            if ((modifiers & definition.Modifier) != 0)
            {
                traits.Add(new CreatureTrait(definition.Name, definition.Description + LiveState(creature, definition.Modifier, damage)));
            }
        }
    }

    // Current state of modifiers that change over time, also applied to the damage profile.
    private static string LiveState(Character creature, CreatureModifiers modifier, DamageProfile damage)
    {
        switch (modifier)
        {
            case CreatureModifiers.Chameleon when DefenseModifiers.CurrentChameleonType(creature) is { } immune:
                damage.Scale(immune, 0f);
                return Loc.T($" Actuellement immunisé : {InsightFormatter.Name(immune)}.", $" Currently immune: {InsightFormatter.Name(immune)}.");
            case CreatureModifiers.Adaptive when DefenseModifiers.CurrentAdaptiveType(creature) is { } adapted:
                damage.Scale(adapted, 1f - DefenseModifiers.AdaptiveReduction);
                return Loc.T($" Adapté en ce moment : {InsightFormatter.Name(adapted)}.", $" Currently adapted to: {InsightFormatter.Name(adapted)}.");
            case CreatureModifiers.Reaping when Reaping.Kills(creature) > 0:
                return Loc.T($" Âmes moissonnées : {Reaping.Kills(creature)}.", $" Souls reaped: {Reaping.Kills(creature)}.");
            case CreatureModifiers.Deathward:
                return Loc.T($" Utilisations restantes : {DefenseModifiers.DeathwardRemaining(creature)}.", $" Uses left: {DefenseModifiers.DeathwardRemaining(creature)}.");
            case CreatureModifiers.Blamer:
                return Loc.T($" Appels restants : {Blamer.CallsRemaining(creature)}.", $" Calls left: {Blamer.CallsRemaining(creature)}.");
            default:
                return string.Empty;
        }
    }
}
