using BepInEx.Configuration;

namespace CllcExtended.Modifiers;

/// <summary>
/// Binds the tunable values of the modifiers in "6 - Modifier values". Each modifier class declares
/// its own values next to the behaviour using them; defaults are CreatureManager's. Percentages are
/// stored as percent in the config (like CLLC) and read as ratios in code. All are synced from the
/// server.
/// </summary>
internal static class ModifierValues
{
    private const string Section = "6 - Modifier values";

    internal static ConfigEntry<float> Percent(ConfigBinder file, string key, float defaultPercent, float maxPercent, string description, bool relog = false) =>
        file.Bind(Section, key, defaultPercent,
            new ConfigDescription(description, new AcceptableValueRange<float>(0f, maxPercent), Tag(relog)), synced: true);

    internal static ConfigEntry<float> Number(ConfigBinder file, string key, float defaultValue, float min, float max, string description, bool relog = false) =>
        file.Bind(Section, key, defaultValue,
            new ConfigDescription(description, new AcceptableValueRange<float>(min, max), Tag(relog)), synced: true);

    internal static ConfigEntry<int> Count(ConfigBinder file, string key, int defaultValue, int min, int max, string description) =>
        file.Bind(Section, key, defaultValue,
            new ConfigDescription(description, new AcceptableValueRange<int>(min, max), Tag(relog: false)), synced: true);

    // Values only read when status effects are registered need a relog (Ultimate SCM tags).
    private static string Tag(bool relog) => relog ? "scm:relog" : "scm:live";
}
