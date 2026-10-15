using BepInEx.Configuration;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace CllcExtended.Modifiers;

/// <summary>
/// Effects of a death, run once by the owner of the character that died (players included, for
/// Reaping): Toxic Death, Omen (adapted without Karma: an avenger appears) and Reaping gains.
/// A creature's network object is destroyed during its death, so what these effects need is
/// captured just before (see <see cref="DeathPatch"/>).
/// </summary>
internal static class DeathModifiers
{
    private static ConfigEntry<float> _toxicRadius = null!;
    private static ConfigEntry<float> _toxicDamage = null!;
    private static ConfigEntry<float> _omenChance = null!;
    private static ConfigEntry<int> _omenExtraStars = null!;

    internal static void Bind(ConfigBinder file)
    {
        _toxicRadius = ModifierValues.Number(file, "Toxic Death radius (m)", 4f, 1f, 30f, "Players within this distance are poisoned when the creature dies.");
        _toxicDamage = ModifierValues.Percent(file, "Toxic Death poison (% of max health)", 30f, 100f, "Poison damage, in percent of each player's max health.");
        _omenChance = ModifierValues.Percent(file, "Omen avenger chance (%)", 50f, 100f, "Chance that an avenger appears when an Omen creature is killed by a player.");
        _omenExtraStars = ModifierValues.Count(file, "Omen avenger extra stars", 2, 0, 10, "Stars the avenger has on top of the dead creature's.");
    }

    internal static float ToxicRadius => _toxicRadius.Value;
    internal static float ToxicDamage => _toxicDamage.Value / 100f;
    internal static float OmenChance => _omenChance.Value / 100f;
    internal static int OmenExtraStars => _omenExtraStars.Value;

    private static readonly List<Player> Players = new();

    /// <summary>State of a character about to die, read before its ZDO goes away.</summary>
    internal readonly struct Dying
    {
        internal Dying(Character character, HitData? lastHit)
        {
            Modifiers = character.IsPlayer() ? CreatureModifiers.None : ModifierStore.Get(character);
            Position = character.transform.position;
            Level = character.GetLevel();
            PrefabName = Utils.GetPrefabName(character.gameObject);
            Killer = lastHit?.GetAttacker() as Player;
        }

        internal CreatureModifiers Modifiers { get; }
        internal Vector3 Position { get; }
        internal int Level { get; }
        internal string PrefabName { get; }
        internal Player? Killer { get; }
    }

    internal static void OnDeath(Character dead, Dying dying)
    {
        Reaping.NotifyDeath(dead, dying.Position);
        if ((dying.Modifiers & CreatureModifiers.ToxicDeath) != 0)
        {
            ToxicDeath(dying.Position);
        }

        if ((dying.Modifiers & CreatureModifiers.Omen) != 0 && dying.Killer != null && Random.value < OmenChance)
        {
            SpawnAvenger(dying, dying.Killer);
        }
    }

    // Poison damage turns into the vanilla poison status on each player's own client.
    private static void ToxicDeath(Vector3 origin)
    {
        Players.Clear();
        Player.GetPlayersInRange(origin, ToxicRadius, Players);
        foreach (var player in Players)
        {
            if (player.IsDead())
            {
                continue;
            }

            var poison = new HitData
            {
                m_point = player.GetCenterPoint(),
                m_dir = (player.transform.position - origin).normalized,
            };
            poison.m_damage.m_poison = player.GetMaxHealth() * ToxicDamage;
            player.Damage(poison);
        }
    }

    // The same creature with more stars and one guaranteed affliction (no Omen, so no chain).
    private static void SpawnAvenger(Dying dead, Player killer)
    {
        var plugin = CllcExtendedPlugin.Instance;
        var prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(dead.PrefabName) : null;
        if (plugin == null || prefab == null)
        {
            return;
        }

        var offset = Random.insideUnitCircle.normalized * 3f;
        var position = dead.Position + new Vector3(offset.x, 0.5f, offset.y);
        var avenger = Object.Instantiate(prefab, position, Quaternion.identity).GetComponent<Character>();
        var zdo = avenger != null ? ModifierStore.Zdo(avenger) : null;
        if (zdo == null)
        {
            return;
        }

        avenger!.SetLevel(dead.Level + OmenExtraStars);
        ModifierStore.Set(zdo, ModifierRoller.RollGroup(ModifierGroup.Affliction, plugin.Settings, guaranteed: true));
        avenger.GetComponent<BaseAI>()?.Alert();
        killer.Message(MessageHud.MessageType.Center, Loc.T("Un vengeur surgit !", "An avenger rises!"));
    }
}

/// <summary>Detects deaths where the game does: CheckDeath, on the owner of the character.</summary>
[HarmonyPatch(typeof(Character), "CheckDeath")]
internal static class DeathPatch
{
    // CheckDeath runs every fixed frame: only a character about to die is captured.
    [HarmonyPrefix]
    private static void Prefix(Character __instance, HitData ___m_lastHit, out DeathModifiers.Dying? __state)
    {
        __state = !__instance.IsDead() && __instance.GetHealth() <= 0f ? new DeathModifiers.Dying(__instance, ___m_lastHit) : null;
    }

    [HarmonyPostfix]
    private static void Postfix(Character __instance, DeathModifiers.Dying? __state)
    {
        if (__state is { } dying && __instance.IsDead())
        {
            DeathModifiers.OnDeath(__instance, dying);
        }
    }
}
