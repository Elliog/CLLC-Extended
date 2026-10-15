using BepInEx.Configuration;
namespace CllcExtended.Modifiers;

/// <summary>
/// Vampiric creatures heal for part of the health they remove with a direct hit (same rule and
/// default power as CreatureManager's Vampiric). <see cref="Character.Heal"/> forwards the heal to
/// whichever client owns the creature.
/// </summary>
internal static class VampiricModifier
{
    private static ConfigEntry<float> _healRatio = null!;

    internal static void Bind(ConfigBinder file)
    {
        _healRatio = ModifierValues.Percent(file, "Vampiric heal (% of damage)", 30f, 200f, "Share of the health removed by a direct hit that Vampiric creatures heal.");
    }

    internal static float HealRatio => _healRatio.Value / 100f;

    internal static void OnDirectHit(Character attacker, float healthLost) => attacker.Heal(healthLost * HealRatio);
}
