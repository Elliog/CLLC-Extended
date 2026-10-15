using HarmonyLib;
using System;

namespace CllcExtended.Modifiers;

/// <summary>Combat modifiers this plugin can give a creature, stored as a bit mask on its ZDO.</summary>
[Flags]
internal enum CreatureModifiers
{
    None = 0,

    // Offense
    Furious = 1 << 0,
    ArmorPiercing = 1 << 1,
    Staggering = 1 << 2,
    Undodgeable = 1 << 3,

    // Defense
    Deathward = 1 << 4,
    Reflection = 1 << 5,
    Vortex = 1 << 6,
    Adaptive = 1 << 7,
    Unflinching = 1 << 8,
    Chameleon = 1 << 9,

    // Affliction
    Exposed = 1 << 10,
    Weakening = 1 << 11,
    Withered = 1 << 12,
    Crippling = 1 << 13,
    Disruptive = 1 << 14,
    AdrenalineDrain = 1 << 15,
    Corrosive = 1 << 16,
    ToxicDeath = 1 << 17,

    // Special
    Vampiric = 1 << 18,
    Reaping = 1 << 19,
    Blink = 1 << 20,
    Omen = 1 << 21,
    Juggernaut = 1 << 22,
    Blamer = 1 << 23,
}

/// <summary>Reads and writes a creature's modifiers on its ZDO, so every client sees the same ones.</summary>
internal static class ModifierStore
{
    // -1 (absent) means "not rolled yet"; 0 means "rolled, no modifier".
    private const string ZdoKey = "cllcx.modifiers";
    private const int NotRolled = -1;

    // The character's own ZNetView field: read on every hit and hud update, cheaper than GetComponent.
    private static readonly AccessTools.FieldRef<Character, ZNetView> NViewField =
        AccessTools.FieldRefAccess<Character, ZNetView>("m_nview");

    internal static ZNetView NView(Character creature) => NViewField(creature);

    internal static bool IsRolled(ZDO zdo) => zdo.GetInt(ZdoKey, NotRolled) != NotRolled;

    internal static void Set(ZDO zdo, CreatureModifiers modifiers) => zdo.Set(ZdoKey, (int)modifiers);

    internal static CreatureModifiers Get(Character creature)
    {
        // Turning modifiers off in the config disables existing ones too.
        var enabled = CllcExtendedPlugin.Instance?.Settings.ModifiersEnabled.Value ?? false;
        var zdo = Zdo(creature);
        return enabled && zdo != null ? Stored(zdo) : CreatureModifiers.None;
    }

    internal static bool Has(Character creature, CreatureModifiers modifier) => (Get(creature) & modifier) != 0;

    /// <summary>Removes one modifier (e.g. Blamer once its calls are spent). Owner only.</summary>
    internal static void Remove(Character creature, CreatureModifiers modifier)
    {
        var zdo = Zdo(creature);
        if (zdo != null && IsOwner(creature))
        {
            Set(zdo, Stored(zdo) & ~modifier);
        }
    }

    internal static ZDO? Zdo(Character creature)
    {
        var nview = NView(creature);
        return nview != null && nview.IsValid() ? nview.GetZDO() : null;
    }

    internal static bool IsOwner(Character creature)
    {
        var nview = NView(creature);
        return nview != null && nview.IsValid() && nview.IsOwner();
    }

    // What is stored, whatever the config says.
    private static CreatureModifiers Stored(ZDO zdo)
    {
        var value = zdo.GetInt(ZdoKey, NotRolled);
        return value == NotRolled ? CreatureModifiers.None : (CreatureModifiers)value;
    }
}
