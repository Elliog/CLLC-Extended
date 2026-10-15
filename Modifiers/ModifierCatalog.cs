using System;
using System.Collections.Generic;
using System.Globalization;

namespace CllcExtended.Modifiers;

/// <summary>A creature gets at most one modifier per group, as in CreatureManager.</summary>
internal enum ModifierGroup
{
    Offense,
    Defense,
    Affliction,
    Special,
}

/// <summary>French texts of a modifier: name, adjectives for the creature name, description.</summary>
internal readonly struct FrenchText
{
    internal FrenchText(string name, string masculine, string feminine, Func<string> description)
    {
        Name = name;
        Masculine = masculine;
        Feminine = feminine;
        Description = description;
    }

    internal string Name { get; }
    internal string Masculine { get; }
    internal string Feminine { get; }

    // Built when shown, so it follows the configured values.
    internal Func<string> Description { get; }
}

/// <summary>English texts of a modifier; the adjective goes before the creature name.</summary>
internal readonly struct EnglishText
{
    internal EnglishText(string name, string adjective, Func<string> description)
    {
        Name = name;
        Adjective = adjective;
        Description = description;
    }

    internal string Name { get; }
    internal string Adjective { get; }

    // Built when shown, so it follows the configured values.
    internal Func<string> Description { get; }
}

/// <summary>Everything the rest of the plugin needs to know about one modifier.</summary>
internal sealed class ModifierDefinition
{
    private readonly FrenchText _french;
    private readonly EnglishText _english;

    internal ModifierDefinition(CreatureModifiers modifier, ModifierGroup group, float defaultChance, FrenchText french, EnglishText english)
    {
        Modifier = modifier;
        Group = group;
        DefaultChance = defaultChance;
        _french = french;
        _english = english;
    }

    internal CreatureModifiers Modifier { get; }

    internal ModifierGroup Group { get; }

    /// <summary>Default spawn chance in percent (CreatureManager's generated defaults).</summary>
    internal float DefaultChance { get; }

    /// <summary>Name shown by the inspection, in the display language.</summary>
    internal string Name => Loc.French ? _french.Name : _english.Name;

    internal string Description => Loc.French ? _french.Description() : _english.Description();

    internal string EnglishName => _english.Name;

    /// <summary>Adjective for the creature name (French agrees with the creature's gender).</summary>
    internal string Adjective(bool feminine) => Loc.French ? (feminine ? _french.Feminine : _french.Masculine) : _english.Adjective;
}

/// <summary>
/// The modifiers of this add-on: CreatureManager's list (sighsorry, GPL-3.0) with its default
/// values, minus the ones CLLC already provides (elemental damage, Armored, Regenerating, Swift,
/// Attack Speed). Omen and Blamer are adapted, as there is no Karma or Enforcer system here.
/// The order is the display order of the icons and name adjectives.
/// </summary>
internal static class ModifierCatalog
{
    private const float Chance = 5f;

    internal static readonly ModifierDefinition[] All =
    {
        // Offense
        new(CreatureModifiers.Furious, ModifierGroup.Offense, Chance,
            new("Furieux", "furieux", "furieuse", () => $"Inflige {P(OffenseModifiers.FuriousBonus)} de dégâts en plus."),
            new("Furious", "Furious", () => $"Deals {P(OffenseModifiers.FuriousBonus)} more damage.")),
        new(CreatureModifiers.ArmorPiercing, ModifierGroup.Offense, Chance,
            new("Transperçant", "transperçant", "transperçante", () => $"Ignore {P(OffenseModifiers.ArmorIgnored)} de ton armure."),
            new("Armor Piercing", "Piercing", () => $"Ignores {P(OffenseModifiers.ArmorIgnored)} of your armor.")),
        new(CreatureModifiers.Staggering, ModifierGroup.Offense, Chance,
            new("Déstabilisant", "déstabilisant", "déstabilisante", () => $"Ses coups font {P(OffenseModifiers.StaggerBonus)} de déséquilibre en plus."),
            new("Staggering", "Staggering", () => $"Its hits cause {P(OffenseModifiers.StaggerBonus)} more stagger.")),
        new(CreatureModifiers.Undodgeable, ModifierGroup.Offense, Chance,
            new("Inesquivable", "inesquivable", "inesquivable",
                () => $"Ses attaques traversent l'invulnérabilité de l'esquive mais infligent {P(OffenseModifiers.UndodgeableDamageReduction)} "
                + "de dégâts en moins. Bloquer et parer fonctionnent."),
            new("Undodgeable", "Undodgeable",
                () => $"Its attacks ignore dodge invulnerability but deal {P(OffenseModifiers.UndodgeableDamageReduction)} less damage. "
                + "Blocking and parrying still work.")),

        // Defense
        new(CreatureModifiers.Deathward, ModifierGroup.Defense, Chance,
            new("Increvable", "increvable", "increvable",
                () => $"Annule un coup mortel et revient à {P(DefenseModifiers.DeathwardHealth)} de sa vie "
                + $"({DefenseModifiers.DeathwardCooldown:0} s de recharge, {DefenseModifiers.DeathwardMaxActivations} fois au plus)."),
            new("Deathward", "Deathwarded",
                () => $"Cancels a lethal hit and comes back at {P(DefenseModifiers.DeathwardHealth)} health "
                + $"({DefenseModifiers.DeathwardCooldown:0} s cooldown, {DefenseModifiers.DeathwardMaxActivations} times at most).")),
        new(CreatureModifiers.Reflection, ModifierGroup.Defense, Chance,
            new("Épineux", "épineux", "épineuse",
                () => $"{P(DefenseModifiers.ReflectionChance)} de chances de te renvoyer {P(DefenseModifiers.ReflectionRatio)} des dégâts "
                + $"de mêlée que tu lui infliges ({DefenseModifiers.ReflectionCap:0} au plus)."),
            new("Reflection", "Thorny",
                () => $"{P(DefenseModifiers.ReflectionChance)} chance to send back {P(DefenseModifiers.ReflectionRatio)} of the melee damage "
                + $"you deal to it ({DefenseModifiers.ReflectionCap:0} at most).")),
        new(CreatureModifiers.Vortex, ModifierGroup.Defense, Chance,
            new("Déflecteur", "déflecteur", "déflectrice",
                () => $"{P(DefenseModifiers.VortexChance)} de chances d'ignorer complètement un projectile (dégâts, recul et effets)."),
            new("Vortex", "Deflecting",
                () => $"{P(DefenseModifiers.VortexChance)} chance to ignore a projectile entirely (damage, push and effects).")),
        new(CreatureModifiers.Adaptive, ModifierGroup.Defense, Chance,
            new("Adaptatif", "adaptatif", "adaptative",
                () => $"Retient le type de dégâts dominant d'un coup pendant {DefenseModifiers.AdaptiveDuration:0} s "
                + $"et en subit {P(DefenseModifiers.AdaptiveReduction)} de moins."),
            new("Adaptive", "Adaptive",
                () => $"Remembers the dominant damage type of a hit for {DefenseModifiers.AdaptiveDuration:0} s "
                + $"and takes {P(DefenseModifiers.AdaptiveReduction)} less of it.")),
        new(CreatureModifiers.Unflinching, ModifierGroup.Defense, Chance,
            new("Inébranlable", "inébranlable", "inébranlable", () => "Impossible à déséquilibrer, même par une parade parfaite."),
            new("Unflinching", "Unflinching", () => "Cannot be staggered, not even by a perfect parry.")),
        new(CreatureModifiers.Chameleon, ModifierGroup.Defense, Chance,
            new("Caméléon", "caméléon", "caméléon",
                () => $"En combat, devient immunisé à un type de dégâts qui change toutes les {DefenseModifiers.ChameleonInterval:0} s."),
            new("Chameleon", "Chameleon",
                () => $"While fighting, becomes immune to one damage type that changes every {DefenseModifiers.ChameleonInterval:0} s.")),

        // Affliction (procs on direct hits that remove health, see Afflictions)
        new(CreatureModifiers.Exposed, ModifierGroup.Affliction, Chance,
            new("Fragilisant", "fragilisant", "fragilisante", () => Afflictions.DescribeFr($"de te faire subir {P(Afflictions.ExposedDamageTaken)} de dégâts en plus")),
            new("Exposing", "Exposing", () => Afflictions.DescribeEn($"make you take {P(Afflictions.ExposedDamageTaken)} more damage"))),
        new(CreatureModifiers.Weakening, ModifierGroup.Affliction, Chance,
            new("Affaiblissant", "affaiblissant", "affaiblissante", () => Afflictions.DescribeFr($"de te faire infliger {P(Afflictions.WeakenedDamageDealt)} de dégâts en moins")),
            new("Weakening", "Weakening", () => Afflictions.DescribeEn($"make you deal {P(Afflictions.WeakenedDamageDealt)} less damage"))),
        new(CreatureModifiers.Withered, ModifierGroup.Affliction, Chance,
            new("Flétrissant", "flétrissant", "flétrissante", () => Afflictions.DescribeFr($"de réduire de {P(Afflictions.WitheredHealing)} les soins que tu reçois")),
            new("Withering", "Withering", () => Afflictions.DescribeEn($"reduce the healing you receive by {P(Afflictions.WitheredHealing)}"))),
        new(CreatureModifiers.Crippling, ModifierGroup.Affliction, Chance,
            new("Entravant", "entravant", "entravante", () => Afflictions.DescribeFr($"de réduire de {P(Afflictions.CripplingMovement)} ta vitesse et ton saut")),
            new("Crippling", "Crippling", () => Afflictions.DescribeEn($"reduce your speed and jump by {P(Afflictions.CripplingMovement)}"))),
        new(CreatureModifiers.Disruptive, ModifierGroup.Affliction, Chance,
            new("Épuisant", "épuisant", "épuisante", () => Afflictions.DescribeFr($"de réduire de {P(Afflictions.DisruptiveRegen)} ta régénération d'endurance et d'eitr")),
            new("Disruptive", "Disruptive", () => Afflictions.DescribeEn($"reduce your stamina and eitr regeneration by {P(Afflictions.DisruptiveRegen)}"))),
        new(CreatureModifiers.AdrenalineDrain, ModifierGroup.Affliction, Chance,
            new("Drainant", "drainant", "drainante",
                () => Afflictions.DescribeFr($"de te retirer {P(Afflictions.AdrenalineRemoved)} de ton adrénaline et de réduire tes gains de {P(Afflictions.AdrenalineGain)}")),
            new("Adrenaline Drain", "Draining",
                () => Afflictions.DescribeEn($"remove {P(Afflictions.AdrenalineRemoved)} of your adrenaline and reduce your gains by {P(Afflictions.AdrenalineGain)}"))),
        new(CreatureModifiers.Corrosive, ModifierGroup.Affliction, Chance,
            new("Corrosif", "corrosif", "corrosive", () => Afflictions.DescribeFr($"d'user ton équipement {P(Afflictions.CorrosiveWear)} plus vite")),
            new("Corrosive", "Corrosive", () => Afflictions.DescribeEn($"make your equipment wear {P(Afflictions.CorrosiveWear)} faster"))),
        new(CreatureModifiers.ToxicDeath, ModifierGroup.Affliction, 10f,
            new("Toxique", "toxique", "toxique",
                () => $"À sa mort, empoisonne les joueurs à {DeathModifiers.ToxicRadius:0} m pour {P(DeathModifiers.ToxicDamage)} de leur vie max."),
            new("Toxic Death", "Toxic",
                () => $"On death, poisons players within {DeathModifiers.ToxicRadius:0} m for {P(DeathModifiers.ToxicDamage)} of their max health.")),

        // Special
        new(CreatureModifiers.Vampiric, ModifierGroup.Special, Chance,
            new("Vampirique", "vampirique", "vampirique", () => $"Se soigne de {P(VampiricModifier.HealRatio)} des dégâts directs qu'il inflige."),
            new("Vampiric", "Vampiric", () => $"Heals for {P(VampiricModifier.HealRatio)} of the direct damage it deals.")),
        new(CreatureModifiers.Reaping, ModifierGroup.Special, Chance,
            new("Moissonneur", "moissonneur", "moissonneuse",
                () => $"Chaque mort à {Reaping.Radius:0} m le soigne ({P(Reaping.HealPerKill)} de sa vie, {Reaping.HealMaxActivations} fois au plus) "
                + $"et le renforce : +{P(Reaping.MaxHealthPerKill)} de vie max (x{Reaping.MaxHealthCap:0} au plus), "
                + $"+{P(Reaping.DamagePerKill)} de dégâts (+{P(Reaping.DamageCap)} au plus), "
                + $"+{P(Reaping.ScalePerKill)} de taille (+{P(Reaping.ScaleCap)} au plus)."),
            new("Reaping", "Reaping",
                () => $"Every death within {Reaping.Radius:0} m heals it ({P(Reaping.HealPerKill)} of its health, {Reaping.HealMaxActivations} times at most) "
                + $"and grows it: +{P(Reaping.MaxHealthPerKill)} max health (x{Reaping.MaxHealthCap:0} at most), "
                + $"+{P(Reaping.DamagePerKill)} damage (+{P(Reaping.DamageCap)} at most), +{P(Reaping.ScalePerKill)} size (+{P(Reaping.ScaleCap)} at most).")),
        new(CreatureModifiers.Blink, ModifierGroup.Special, Chance,
            new("Téléporteur", "téléporteur", "téléporteuse",
                () => $"Se téléporte près de sa cible à {Blink.MaxRange:0} m toutes les {Blink.Cooldown:0} s, "
                + $"{Blink.AlertGrace:0} s après t'avoir repéré. Pas en donjon."),
            new("Blink", "Blinking",
                () => $"Teleports next to its target within {Blink.MaxRange:0} m every {Blink.Cooldown:0} s, "
                + $"{Blink.AlertGrace:0} s after spotting you. Not in dungeons.")),
        new(CreatureModifiers.Omen, ModifierGroup.Special, Chance,
            new("Funeste", "funeste", "funeste",
                () => $"S'il est tué par un joueur, {P(DeathModifiers.OmenChance)} de chances qu'un vengeur apparaisse : "
                + $"la même créature avec {DeathModifiers.OmenExtraStars} étoiles de plus et une affliction."),
            new("Omen", "Ominous",
                () => $"When killed by a player, {P(DeathModifiers.OmenChance)} chance that an avenger appears: "
                + $"the same creature with {DeathModifiers.OmenExtraStars} more stars and an affliction.")),
        new(CreatureModifiers.Juggernaut, ModifierGroup.Special, Chance,
            new("Renversant", "renversant", "renversante", () => $"Ses coups te repoussent violemment ({Juggernaut.PushCooldown:0} s de recharge). Insensible au recul."),
            new("Juggernaut", "Juggernaut", () => $"Its hits knock you back hard ({Juggernaut.PushCooldown:0} s cooldown). Immune to push.")),
        new(CreatureModifiers.Blamer, ModifierGroup.Special, Chance,
            new("Lâche", "lâche", "lâche",
                () => $"Sous {P(Blamer.FleeHealthRatio)} de vie, fuit et alerte les créatures à {Blamer.CallRadius:0} m "
                + $"({Blamer.MaxCalls} appels au plus), puis reprend le combat."),
            new("Blamer", "Cowardly",
                () => $"Below {P(Blamer.FleeHealthRatio)} health, flees and alerts creatures within {Blamer.CallRadius:0} m "
                + $"({Blamer.MaxCalls} calls at most), then fights again.")),
    };

    private static readonly Dictionary<CreatureModifiers, ModifierDefinition> ByModifier = Index();

    internal static ModifierDefinition For(CreatureModifiers modifier) => ByModifier[modifier];

    /// <summary>0.2 as "20 %" in French, "20%" in English.</summary>
    internal static string P(float ratio) => (ratio * 100f).ToString("0.#", CultureInfo.InvariantCulture) + Loc.T(" %", "%");

    private static Dictionary<CreatureModifiers, ModifierDefinition> Index()
    {
        var index = new Dictionary<CreatureModifiers, ModifierDefinition>();
        foreach (var definition in All)
        {
            index.Add(definition.Modifier, definition);
        }

        return index;
    }
}
