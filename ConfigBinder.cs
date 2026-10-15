using BepInEx.Configuration;
using ServerSync;

namespace CllcExtended;

/// <summary>
/// Binds config entries and registers them with ServerSync, like CLLC: synced entries take the
/// server's value while "Lock Configuration" is on; local ones (display, language, keys) stay per
/// player. ServerSync also checks versions: a player without the mod, or with another version, is
/// refused by the server with a message saying why.
/// </summary>
internal sealed class ConfigBinder
{
    private readonly ConfigFile _file;

    internal ConfigBinder(ConfigFile file)
    {
        _file = file;
        Sync = new ConfigSync(PluginInfo.Guid)
        {
            DisplayName = PluginInfo.Name,
            CurrentVersion = PluginInfo.Version,
            MinimumRequiredVersion = PluginInfo.Version,
            // Modifiers are rolled and applied by whichever game owns a creature: everyone needs the mod.
            ModRequired = true,
        };
    }

    internal ConfigSync Sync { get; }

    internal ConfigEntry<T> Bind<T>(string section, string key, T value, ConfigDescription description, bool synced)
    {
        var entry = _file.Bind(section, key, value, description);
        Sync.AddConfigEntry(entry).SynchronizedConfig = synced;
        return entry;
    }
}
