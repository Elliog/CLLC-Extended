using System;
using System.Collections.Generic;

namespace CllcExtended.Inspection;

/// <summary>Damage types a player can deal to a creature (chop and pickaxe only hit objects).</summary>
internal enum DamageKind
{
    Blunt,
    Slash,
    Pierce,
    Fire,
    Frost,
    Lightning,
    Poison,
    Spirit,
}

/// <summary>
/// Share of the incoming damage a creature actually takes: 1 is normal, 0.5 resistant, 0 immune.
/// <see cref="Overall"/> applies to every type on top of the per-type factors.
/// </summary>
internal sealed class DamageProfile
{
    internal static readonly DamageKind[] AllKinds = (DamageKind[])Enum.GetValues(typeof(DamageKind));

    private readonly float[] _factors;

    internal DamageProfile()
    {
        _factors = new float[AllKinds.Length];
        for (var i = 0; i < _factors.Length; i++)
        {
            _factors[i] = 1f;
        }
    }

    internal float Overall { get; private set; } = 1f;

    internal float this[DamageKind kind] => _factors[(int)kind];

    internal void Scale(DamageKind kind, float factor) => _factors[(int)kind] *= factor;

    internal void ScaleOverall(float factor) => Overall *= factor;

    internal IEnumerable<DamageKind> Where(Func<float, bool> predicate)
    {
        foreach (var kind in AllKinds)
        {
            if (predicate(this[kind]))
            {
                yield return kind;
            }
        }
    }
}
