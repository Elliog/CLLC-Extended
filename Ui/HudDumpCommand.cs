using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CllcExtended.Ui;

/// <summary>
/// Diagnostic command: <c>cllcx_hud</c> writes the object tree of the aimed creature's hud
/// (names, active state, positions, scales, texts) to the BepInEx log, to see what other mods add.
/// </summary>
internal static class HudDumpCommand
{
    private const int MaxDepth = 4;

    internal static void Register()
    {
        _ = new Terminal.ConsoleCommand("cllcx_hud", "- write the aimed creature's hud tree to the BepInEx log", Run);
    }

    private static void Run(Terminal.ConsoleEventArgs args)
    {
        var creature = Player.m_localPlayer?.GetHoverCreature();
        var gui = creature != null ? NameplateInspection.FindHud(creature) : null;
        if (creature == null || gui == null)
        {
            args.Context.AddString("Aim at a creature whose hud is shown.");
            return;
        }

        var dump = new StringBuilder();
        dump.AppendLine($"HUD of {creature.name} (level {creature.GetLevel()}):");
        Append(dump, gui.transform, 0);
        CllcExtendedPlugin.Instance?.Log.LogInfo(dump.ToString());
        args.Context.AddString("Hud tree written to BepInEx/LogOutput.log (cllcx_hud).");
    }

    private static void Append(StringBuilder dump, Transform node, int depth)
    {
        var position = node.localPosition;
        dump.Append(' ', depth * 2)
            .Append(node.name)
            .Append(node.gameObject.activeSelf ? "" : " [inactive]")
            .Append($" pos=({position.x:0.#},{position.y:0.#})");

        if (node.localScale != Vector3.one)
        {
            dump.Append($" scale=({node.localScale.x:0.##},{node.localScale.y:0.##})");
        }

        var text = node.GetComponent<TMP_Text>()?.text ?? node.GetComponent<Text>()?.text;
        if (!string.IsNullOrEmpty(text))
        {
            dump.Append(" text=\"").Append(text!.Replace("\n", "\\n")).Append('"');
        }

        dump.AppendLine();
        if (depth >= MaxDepth)
        {
            return;
        }

        foreach (Transform child in node)
        {
            Append(dump, child, depth + 1);
        }
    }
}
