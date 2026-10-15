using HarmonyLib;
using UnityEngine;
using CllcExtended.Inspection;

namespace CllcExtended.Ui;

/// <summary>
/// On the inspection key, writes the full explanation of the aimed creature in the local chat.
/// Nothing is sent to other players.
/// </summary>
internal static class ChatInspection
{
    // The chat window hides itself once this timer exceeds its hide delay; resetting it shows the chat.
    private static readonly AccessTools.FieldRef<Chat, float> HideTimer =
        AccessTools.FieldRefAccess<Chat, float>("m_hideTimer");

    internal static void Update(KeyCode key, CreatureInsightReader reader)
    {
        var player = Player.m_localPlayer;
        if (player == null || Chat.instance == null || IsTyping() || !ZInput.GetKeyDown(key, logWarning: false))
        {
            return;
        }

        var creature = player.GetHoverCreature();
        if (creature == null || creature.IsPlayer() || creature.IsDead())
        {
            MessageHud.instance?.ShowMessage(MessageHud.MessageType.TopLeft, Loc.T("Vise une créature pour l'inspecter.", "Aim at a creature to inspect it."));
            return;
        }

        foreach (var line in InsightFormatter.Detailed(reader.Read(creature)))
        {
            Chat.instance.AddString(line);
        }

        HideTimer(Chat.instance) = 0f;
    }

    // Same guards the game uses before reacting to gameplay keys.
    private static bool IsTyping() =>
        Chat.instance.HasFocus()
        || Console.IsVisible()
        || TextInput.IsVisible()
        || Minimap.InTextInput()
        || Menu.IsVisible()
        || InventoryGui.IsVisible();
}
