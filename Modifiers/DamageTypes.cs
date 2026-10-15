using CllcExtended.Inspection;

namespace CllcExtended.Modifiers;

/// <summary>Per-type access to <see cref="HitData.DamageTypes"/> using the inspection's damage kinds.</summary>
internal static class DamageTypes
{
    internal static float Get(HitData.DamageTypes damage, DamageKind kind) =>
        kind switch
        {
            DamageKind.Blunt => damage.m_blunt,
            DamageKind.Slash => damage.m_slash,
            DamageKind.Pierce => damage.m_pierce,
            DamageKind.Fire => damage.m_fire,
            DamageKind.Frost => damage.m_frost,
            DamageKind.Lightning => damage.m_lightning,
            DamageKind.Poison => damage.m_poison,
            _ => damage.m_spirit,
        };

    internal static void Scale(ref HitData.DamageTypes damage, DamageKind kind, float factor)
    {
        switch (kind)
        {
            case DamageKind.Blunt: damage.m_blunt *= factor; break;
            case DamageKind.Slash: damage.m_slash *= factor; break;
            case DamageKind.Pierce: damage.m_pierce *= factor; break;
            case DamageKind.Fire: damage.m_fire *= factor; break;
            case DamageKind.Frost: damage.m_frost *= factor; break;
            case DamageKind.Lightning: damage.m_lightning *= factor; break;
            case DamageKind.Poison: damage.m_poison *= factor; break;
            default: damage.m_spirit *= factor; break;
        }
    }

    /// <summary>The kind dealing the most damage in a hit, or null if the hit deals none.</summary>
    internal static DamageKind? Dominant(HitData.DamageTypes damage)
    {
        DamageKind? dominant = null;
        var best = 0f;
        foreach (var kind in DamageProfile.AllKinds)
        {
            var value = Get(damage, kind);
            if (value > best)
            {
                best = value;
                dominant = kind;
            }
        }

        return dominant;
    }
}
