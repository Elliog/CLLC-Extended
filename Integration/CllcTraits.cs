using System.Collections.Generic;
using System.Globalization;
using CllcExtended.Inspection;

namespace CllcExtended.Integration;

/// <summary>
/// Translates CLLC effects, infusions and boss affixes into player-facing traits, and applies the
/// damage CLLC changes at hit time (it never touches the vanilla damage modifiers).
/// Numbers mirror CLLC 5.0 behaviour; creature effect powers use CLLC defaults (they are only
/// overridable through its creature YAML, which this add-on does not read).
/// Trait names are CLLC's own French and English names (shown in the creature name), so both
/// mods agree; Sturdy and Godlike have no French name in CLLC.
/// </summary>
internal static class CllcTraits
{
    private const string BossPowerSection = "8 - Boss Affix power";

    // CLLC multiplies hit damage by 0.33 for Armored creatures.
    private const float ArmoredDamageTaken = 0.33f;

    internal static void Apply(CllcBridge cllc, Character creature, DamageProfile damage, List<CreatureTrait> traits)
    {
        if (!cllc.IsLoaded)
        {
            return;
        }

        if (creature.IsBoss())
        {
            ApplyBossAffix(cllc, cllc.BossAffix(creature), damage, traits);
            return;
        }

        ApplyExtraEffect(cllc.ExtraEffect(creature), damage, traits);
        ApplyInfusion(cllc.Infusion(creature), damage, traits);
    }

    private static void ApplyExtraEffect(string effect, DamageProfile damage, List<CreatureTrait> traits)
    {
        switch (effect)
        {
            case "Aggressive":
                Add(traits, ("Agressif", "Attaque 25 % plus vite et revient à la charge plus souvent."),
                    ("Aggressive", "Attacks 25% faster and comes back at you more often."));
                break;
            case "Quick":
                Add(traits, ("Rapide", "Se déplace 40 % plus vite."), ("Quick", "Moves 40% faster."));
                break;
            case "Regenerating":
                Add(traits, ("Régénératif", "Régénère sa vie en continu."), ("Regenerating", "Regenerates health over time."));
                break;
            case "Curious":
                Add(traits, ("Curieux", "Te voit et t'entend deux fois plus loin."), ("Curious", "Sees and hears you from twice as far."));
                break;
            case "Splitting":
                Add(traits, ("Scindable", "Se divise en deux créatures plus faibles à sa mort."),
                    ("Splitting", "Splits into two weaker creatures on death."));
                break;
            case "Armored":
                damage.ScaleOverall(ArmoredDamageTaken);
                Add(traits,
                    ("Renforcé", $"Subit {Percent(1f - ArmoredDamageTaken)} de dégâts en moins, mais se déplace 50 % moins vite."),
                    ("Armored", $"Takes {Percent(1f - ArmoredDamageTaken)} less damage, but moves 50% slower."));
                break;
        }
    }

    private static void ApplyInfusion(string infusion, DamageProfile damage, List<CreatureTrait> traits)
    {
        switch (infusion)
        {
            case "Fire":
                Swap(damage, DamageKind.Fire, DamageKind.Frost);
                Add(traits, ("Ardent", "Infusion de feu : résiste au feu, vulnérable au givre. Ses coups t'enflamment."),
                    ("Fire-Infused", "Resists fire, weak to frost. Its hits set you on fire."));
                break;
            case "Frost":
                Swap(damage, DamageKind.Frost, DamageKind.Fire);
                Add(traits, ("Gelé", "Infusion de givre : résiste au givre, vulnérable au feu. Ses coups te gèlent."),
                    ("Frost-Infused", "Resists frost, weak to fire. Its hits freeze you."));
                break;
            case "Lightning":
                Swap(damage, DamageKind.Lightning, DamageKind.Spirit);
                Add(traits, ("Foudroyant", "Infusion de foudre : résiste à la foudre, vulnérable à l'esprit. Ses coups ajoutent des dégâts de foudre."),
                    ("Lightning-Infused", "Resists lightning, weak to spirit. Its hits add lightning damage."));
                break;
            case "Spirit":
                Swap(damage, DamageKind.Spirit, DamageKind.Lightning);
                Add(traits, ("Spirituel", "Infusion d'esprit : résiste à l'esprit, vulnérable à la foudre. Se soigne en te frappant."),
                    ("Spirit-Infused", "Resists spirit, weak to lightning. Heals itself when hitting you."));
                break;
            case "Poison":
                damage.Scale(DamageKind.Poison, 0f);
                Add(traits, ("Empoisonné", "Infusion de poison : immunisé au poison. Explose en nuage de poison à sa mort."),
                    ("Poison-Infused", "Immune to poison. Explodes into a poison cloud on death."));
                break;
            case "Chaos":
                // A random infusion is rolled on every hit, so no fixed resistance can be shown.
                Add(traits, ("Chaotique", "Infusion du chaos : tire une infusion au hasard à chaque coup donné ou reçu : ses résistances changent sans cesse."),
                    ("Chaos-Infused", "Rolls a random infusion on every hit dealt or taken: its resistances keep changing."));
                break;
        }
    }

    private static void ApplyBossAffix(CllcBridge cllc, string affix, DamageProfile damage, List<CreatureTrait> traits)
    {
        var shielded = cllc.Setting(BossPowerSection, "Shielded damage reduction (percentage)", 50f) / 100f;
        var sturdy = cllc.Setting(BossPowerSection, "Sturdy damage taken reduction (percentage)", 25f) / 100f;
        var elementalist = cllc.Setting(BossPowerSection, "Elementalist damage increase (percentage)", 20f) / 100f;
        var enraged = cllc.Setting(BossPowerSection, "Enraged damage increase (percentage)", 20f) / 100f;
        var twin = cllc.Setting(BossPowerSection, "Twin damage and health (percentage)", 75f) / 100f;

        switch (affix)
        {
            case "Reflective":
                Add(traits, ("Réfléchissant", "Te renvoie une partie des dégâts directs que tu lui infliges."),
                    ("Reflective", "Sends back part of the direct damage you deal to it."));
                break;
            case "Shielded":
                Add(traits, ("Blindé", $"Subit {Percent(shielded)} de dégâts en moins des arcs et arbalètes."),
                    ("Shielded", $"Takes {Percent(shielded)} less damage from bows and crossbows."));
                break;
            case "Mending":
                Add(traits, ("Régénératif", "Regagne sa vie bien plus vite."), ("Mending", "Regains health much faster."));
                break;
            case "Summoner":
                Add(traits, ("Invocateur", "Appelle des renforts à chaque tiers de sa vie perdu."),
                    ("Summoner", "Calls reinforcements every third of its health lost."));
                break;
            case "Elementalist":
                Add(traits, ("Élémentaliste", $"Inflige {Percent(elementalist)} de dégâts élémentaires en plus."),
                    ("Elementalist", $"Deals {Percent(elementalist)} more elemental damage."));
                break;
            case "Enraged":
                Add(traits, ("Enragé", $"Inflige {Percent(enraged)} de dégâts physiques en plus."),
                    ("Enraged", $"Deals {Percent(enraged)} more physical damage."));
                break;
            case "Twin":
                Add(traits,
                    ("Dédoublé", $"Un second boss l'accompagne. Ils partagent leur vie, chacun avec {Percent(twin)} de sa vie et de ses dégâts."),
                    ("Twin", $"A second boss comes along. They share their health, each with {Percent(twin)} of its health and damage."));
                break;
            case "Sturdy":
                damage.ScaleOverall(1f - sturdy);
                Add(traits, ("Robuste", $"Subit {Percent(sturdy)} de dégâts en moins."), ("Sturdy", $"Takes {Percent(sturdy)} less damage."));
                break;
            case "Godlike":
                damage.ScaleOverall(1f - sturdy);
                Add(traits,
                    ("Divin", "Cumule tous les affixes : réfléchissant, blindé, régénératif, invocateur, élémentaliste, enragé, dédoublé et robuste."),
                    ("Godlike", "Has every affix: reflective, shielded, mending, summoner, elementalist, enraged, twin and sturdy."));
                break;
        }
    }

    private static void Add(List<CreatureTrait> traits, (string Name, string Description) french, (string Name, string Description) english)
    {
        var text = Loc.French ? french : english;
        traits.Add(new CreatureTrait(text.Name, text.Description));
    }

    // CLLC halves the resisted type and doubles the opposite one.
    private static void Swap(DamageProfile damage, DamageKind resisted, DamageKind vulnerable)
    {
        damage.Scale(resisted, 0.5f);
        damage.Scale(vulnerable, 2f);
    }

    private static string Percent(float ratio) => Modifiers.ModifierCatalog.P(ratio);
}
