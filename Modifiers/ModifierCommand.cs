using System;
using System.Linq;

namespace CllcExtended.Modifiers;

/// <summary>
/// Test command: <c>cllcx_modifier &lt;Modifier...|None&gt;</c> sets the modifiers of the aimed creature.
/// Registered as a cheat, so it needs <c>devcommands</c>.
/// </summary>
internal static class ModifierCommand
{
    private const string Name = "cllcx_modifier";

    internal static void Register()
    {
        var names = string.Join("|", Enum.GetNames(typeof(CreatureModifiers)));
        _ = new Terminal.ConsoleCommand(
            Name,
            $"[{names}] - set the modifiers of the aimed creature",
            Run,
            isCheat: true,
            optionsFetcher: () => Enum.GetNames(typeof(CreatureModifiers)).ToList());
    }

    private static void Run(Terminal.ConsoleEventArgs args)
    {
        var creature = Player.m_localPlayer?.GetHoverCreature();
        var nview = creature != null ? ModifierStore.NView(creature) : null;
        if (creature == null || creature.IsPlayer() || nview?.GetZDO() == null)
        {
            args.Context.AddString("Aim at a creature.");
            return;
        }

        var modifiers = CreatureModifiers.None;
        foreach (var arg in args.Args.Skip(1))
        {
            if (!Enum.TryParse(arg, ignoreCase: true, out CreatureModifiers parsed))
            {
                args.Context.AddString($"Unknown modifier: {arg}");
                return;
            }

            modifiers |= parsed;
        }

        nview.ClaimOwnership();
        ModifierStore.Set(nview.GetZDO(), modifiers);
        args.Context.AddString($"{creature.GetHoverName()} : {modifiers}");
    }
}
