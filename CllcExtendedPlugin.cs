using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using CllcExtended.Inspection;
using CllcExtended.Integration;
using CllcExtended.Modifiers;
using CllcExtended.Ui;

namespace CllcExtended;

/// <summary>
/// CLLC Extended: an add-on for Creature Level &amp; Loot Control with creature inspection, star
/// visuals and extra combat modifiers, built on ideas and code from CreatureManager and StarLevelSystem.
/// CLLC is required: BepInEx does not load this plugin without it.
/// </summary>
[BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
[BepInDependency(CllcBridge.PluginGuid)]
public sealed class CllcExtendedPlugin : BaseUnityPlugin
{
    private Harmony? _harmony;

    internal static CllcExtendedPlugin? Instance { get; private set; }

    internal PluginConfig Settings { get; private set; } = null!;

    internal CreatureInsightReader Reader { get; private set; } = null!;

    internal BepInEx.Logging.ManualLogSource Log => Logger;

    private void Awake()
    {
        Settings = new PluginConfig(Config);
        Instance = this;
        _harmony = new Harmony(PluginInfo.Guid);
        _harmony.PatchAll(typeof(CllcExtendedPlugin).Assembly);
        ModifierCommand.Register();
        HudDumpCommand.Register();
    }

    // Start runs once every plugin is loaded, so CLLC can be resolved.
    private void Start()
    {
        var cllc = CllcBridge.Resolve();
        Reader = new CreatureInsightReader(cllc);
        Logger.LogInfo(cllc.IsLoaded
            ? "Creature Level & Loot Control found: its effects are included in inspections."
            : "Creature Level & Loot Control not found: inspections show vanilla resistances only.");
    }

    private void Update()
    {
        if (Reader != null)
        {
            ChatInspection.Update(Settings.InspectKey.Value, Reader);
        }
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
        Instance = null;
    }
}
