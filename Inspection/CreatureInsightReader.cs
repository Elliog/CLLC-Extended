using System;
using System.Collections.Generic;
using CllcExtended.Integration;
using CllcExtended.Modifiers;

namespace CllcExtended.Inspection;

/// <summary>Builds a <see cref="CreatureInsight"/> from the game state of a creature.</summary>
internal sealed class CreatureInsightReader
{
    private readonly CllcBridge _cllc;

    internal CreatureInsightReader(CllcBridge cllc)
    {
        _cllc = cllc;
    }

    internal CreatureInsight Read(Character creature)
    {
        var damage = new DamageProfile();
        // Includes armor and status effect modifiers, like the game does when the creature is hit.
        var modifiers = creature.GetDamageModifiers();
        foreach (var kind in DamageProfile.AllKinds)
        {
            damage.Scale(kind, Factor(modifiers.GetModifier(ToHitType(kind))));
        }

        var traits = new List<CreatureTrait>();
        CllcTraits.Apply(_cllc, creature, damage, traits);
        ModifierTraits.Add(creature, damage, traits);

        var name = Localization.instance.Localize(creature.GetHoverName());
        return new CreatureInsight(name, Math.Max(0, creature.GetLevel() - 1), damage, traits);
    }

    // Same factors as HitData.ApplyModifier.
    private static float Factor(HitData.DamageModifier modifier) =>
        modifier switch
        {
            HitData.DamageModifier.SlightlyResistant => 0.75f,
            HitData.DamageModifier.Resistant => 0.5f,
            HitData.DamageModifier.VeryResistant => 0.25f,
            HitData.DamageModifier.SlightlyWeak => 1.25f,
            HitData.DamageModifier.Weak => 1.5f,
            HitData.DamageModifier.VeryWeak => 2f,
            HitData.DamageModifier.Immune => 0f,
            HitData.DamageModifier.Ignore => 0f,
            _ => 1f,
        };

    private static HitData.DamageType ToHitType(DamageKind kind) =>
        kind switch
        {
            DamageKind.Blunt => HitData.DamageType.Blunt,
            DamageKind.Slash => HitData.DamageType.Slash,
            DamageKind.Pierce => HitData.DamageType.Pierce,
            DamageKind.Fire => HitData.DamageType.Fire,
            DamageKind.Frost => HitData.DamageType.Frost,
            DamageKind.Lightning => HitData.DamageType.Lightning,
            DamageKind.Poison => HitData.DamageType.Poison,
            DamageKind.Spirit => HitData.DamageType.Spirit,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
}
