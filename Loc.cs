namespace CllcExtended;

/// <summary>Language of the texts shown to players.</summary>
public enum DisplayLanguage
{
    Auto,
    English,
    French,
}

/// <summary>Picks the French or English text, following the config or the game's language.</summary>
internal static class Loc
{
    internal static bool French =>
        CllcExtendedPlugin.Instance?.Settings.Language.Value switch
        {
            DisplayLanguage.French => true,
            DisplayLanguage.English => false,
            _ => Localization.instance != null && Localization.instance.GetSelectedLanguage() == "French",
        };

    internal static string T(string french, string english) => French ? french : english;
}
