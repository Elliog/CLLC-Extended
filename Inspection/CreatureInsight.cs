using System.Collections.Generic;

namespace CllcExtended.Inspection;

/// <summary>A named modifier carried by a creature, with a player-facing explanation.</summary>
internal sealed class CreatureTrait
{
    internal CreatureTrait(string name, string description)
    {
        Name = name;
        Description = description;
    }

    internal string Name { get; }

    internal string Description { get; }
}

/// <summary>Everything the inspection shows about one creature, independent of how it is displayed.</summary>
internal sealed class CreatureInsight
{
    internal CreatureInsight(string name, int stars, DamageProfile damage, IReadOnlyList<CreatureTrait> traits)
    {
        Name = name;
        Stars = stars;
        Damage = damage;
        Traits = traits;
    }

    internal string Name { get; }

    internal int Stars { get; }

    internal DamageProfile Damage { get; }

    internal IReadOnlyList<CreatureTrait> Traits { get; }
}
