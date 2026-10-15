using System.Collections.Generic;
using BepInEx.Configuration;
using CllcExtended.Modifiers;
using CllcExtended.Ui;
using UnityEngine;

namespace CllcExtended;

/// <summary>
/// Every setting, in numbered sections like CLLC's. Gameplay settings are synced from the server
/// through ServerSync (see <see cref="ConfigBinder"/>); display settings stay per player. Admins can
/// edit them in game with Ultimate Server Configuration Manager; tags tell it when a change applies
/// (scm:live / scm:relog).
/// </summary>
internal sealed class PluginConfig
{
    private const string General = "1 - General";
    private const string Inspection = "2 - Inspection";
    private const string Display = "3 - Display";
    private const string Modifiers = "4 - Modifiers";
    private const string Chances = "5 - Modifier chances";
    private const string Live = "scm:live";
    private const string Relog = "scm:relog";

    private readonly Dictionary<CreatureModifiers, ConfigEntry<float>> _chances = new();

    internal PluginConfig(ConfigFile configFile)
    {
        var file = new ConfigBinder(configFile);
        LockConfiguration = file.Bind(General, "Lock Configuration", true,
            Describe("On: players use the server's gameplay settings and only admins change them (display settings stay personal).", null, Live),
            synced: true);
        file.Sync.AddLockingConfigEntry(LockConfiguration);

        Language = file.Bind(General, "Language", DisplayLanguage.Auto,
            Describe("Language of names and texts: Auto follows the game (French or English), or force one.", null, Relog), synced: false);

        CrouchInspection = file.Bind(Inspection, "Crouch inspection", true,
            Describe("While crouching, show resistances, weaknesses and modifiers under the aimed creature's nameplate.", null, Live), synced: false);
        InspectKey = file.Bind(Inspection, "Inspect key", KeyCode.I,
            Describe("Writes the full explanation of the aimed creature in your own chat.", null, Live), synced: false);

        StarColors = file.Bind(Display, "Star colors", true,
            Describe("Give each star count from 3 stars its own body colour (the game only has 1 and 2-star variants).", null, Relog), synced: false);
        MaxStarsShown = file.Bind(Display, "Stars shown before counter", 5,
            Describe("Above this many stars, the nameplate shows one star and the number instead of the star rows.",
                new AcceptableValueRange<int>(2, 10), Live), synced: false);
        NameQualifiers = file.Bind(Display, "Modifier names in creature name", true,
            Describe("Add this mod's modifiers to the creature name, like CLLC does for its effects.", null, Live), synced: false);
        IconSize = file.Bind(Display, "Modifier icon size", 16f,
            Describe("Size in pixels of the modifier icons shown after the stars (a star is 16). 0 hides them.",
                new AcceptableValueRange<float>(0f, 64f), Live), synced: false);
        IconBackground = file.Bind(Display, "Modifier icon background", IconBackgroundShape.Circle,
            Describe("Shape drawn behind each modifier icon to keep it readable.", null, Live), synced: false);
        IconBackgroundColor = file.Bind(Display, "Modifier icon background color", new Color(0f, 0f, 0f, 0.6f),
            Describe("Background colour as RRGGBBAA hex; the last pair is the opacity (00 transparent, FF opaque).", null, Live), synced: false);
        HideEpicMmoLevel = file.Bind(Display, "Hide EpicMMO creature level", true,
            Describe("Hide the [level] label WackyEpicMMOSystem draws under creature nameplates (display only).", null, Live), synced: false);

        ModifiersEnabled = file.Bind(Modifiers, "Enabled", true,
            Describe("Off: no new creature gets modifiers and existing ones have no effect.", null, Live), synced: true);
        MaxModifiers = file.Bind(Modifiers, "Max modifiers per creature", 4,
            Describe("At most one per group (Offense, Defense, Affliction, Special), and never more than this.",
                new AcceptableValueRange<int>(0, 4), Live), synced: true);
        MinimumStars = file.Bind(Modifiers, "Minimum stars", 0,
            Describe("Creatures with fewer stars never get modifiers.", new AcceptableValueRange<int>(0, 10), Live), synced: true);

        // Values before chances: chance descriptions mention the values.
        OffenseModifiers.Bind(file);
        DefenseModifiers.Bind(file);
        Afflictions.Bind(file);
        DeathModifiers.Bind(file);
        VampiricModifier.Bind(file);
        Reaping.Bind(file);
        Blink.Bind(file);
        Juggernaut.Bind(file);
        Blamer.Bind(file);

        foreach (var definition in ModifierCatalog.All)
        {
            _chances[definition.Modifier] = file.Bind(Chances, $"{definition.Modifier} chance (%)", definition.DefaultChance,
                Describe($"{definition.EnglishName} ({definition.Group}). A creature gets at most one modifier per group; "
                    + "the chances of a group add up (above 100 they are weights). 0 disables it. Values: 6 - Modifier values.",
                    new AcceptableValueRange<float>(0f, 100f), Live),
                synced: true);
        }
    }

    internal ConfigEntry<bool> LockConfiguration { get; }
    internal ConfigEntry<DisplayLanguage> Language { get; }
    internal ConfigEntry<bool> CrouchInspection { get; }
    internal ConfigEntry<KeyCode> InspectKey { get; }
    internal ConfigEntry<bool> StarColors { get; }
    internal ConfigEntry<int> MaxStarsShown { get; }
    internal ConfigEntry<bool> NameQualifiers { get; }
    internal ConfigEntry<float> IconSize { get; }
    internal ConfigEntry<IconBackgroundShape> IconBackground { get; }
    internal ConfigEntry<Color> IconBackgroundColor { get; }
    internal ConfigEntry<bool> HideEpicMmoLevel { get; }
    internal ConfigEntry<bool> ModifiersEnabled { get; }
    internal ConfigEntry<int> MaxModifiers { get; }
    internal ConfigEntry<int> MinimumStars { get; }

    internal IconStyle IconStyle => new(IconSize.Value, IconBackground.Value, IconBackgroundColor.Value);

    /// <summary>Spawn chance (%) of a modifier within its group (see ModifierRoller).</summary>
    internal float ChanceOf(CreatureModifiers modifier) => _chances[modifier].Value;

    private static ConfigDescription Describe(string text, AcceptableValueBase? range, string tag) =>
        new(text, range, tag);
}
