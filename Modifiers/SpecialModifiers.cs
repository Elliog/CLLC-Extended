using BepInEx.Configuration;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace CllcExtended.Modifiers;

/// <summary>Blink: teleports next to its player target (CreatureManager defaults; not indoors).</summary>
internal static class Blink
{
    private static ConfigEntry<float> _cooldown = null!;
    private static ConfigEntry<float> _maxRange = null!;
    private static ConfigEntry<float> _alertGrace = null!;

    internal static void Bind(ConfigBinder file)
    {
        _cooldown = ModifierValues.Number(file, "Blink cooldown (s)", 6f, 1f, 120f, "Seconds between two teleports.");
        _maxRange = ModifierValues.Number(file, "Blink max range (m)", 16f, 2f, 60f, "Blink only targets players within this distance.");
        _alertGrace = ModifierValues.Number(file, "Blink alert grace (s)", 3f, 0f, 30f, "Seconds after spotting a player before the first teleport.");
    }

    internal static float Cooldown => _cooldown.Value;
    internal static float MaxRange => _maxRange.Value;
    internal static float AlertGrace => _alertGrace.Value;
    private const float MinDistance = 4f;
    private const float LandingDistance = 2f;

    private sealed class State
    {
        internal float AlertedSince = -1f;
        internal float NextReady;
    }

    private static readonly ConditionalWeakTable<Character, State> States = new();

    internal static void Update(Character creature, MonsterAI ai)
    {
        var state = States.GetOrCreateValue(creature);
        if (!ai.IsAlerted())
        {
            state.AlertedSince = -1f;
            return;
        }

        var now = Time.time;
        if (state.AlertedSince < 0f)
        {
            state.AlertedSince = now;
        }

        if (now - state.AlertedSince < AlertGrace || now < state.NextReady || ai.GetTargetCreature() is not Player target)
        {
            return;
        }

        // Dungeons have no terrain to land on and walls to go through: Blink stays outdoors.
        var distance = Vector3.Distance(creature.transform.position, target.transform.position);
        if (creature.InInterior() || target.InInterior() || distance > MaxRange || distance < MinDistance)
        {
            return;
        }

        var away = creature.transform.position - target.transform.position;
        away.y = 0f;
        var destination = target.transform.position + away.normalized * LandingDistance;
        if (ZoneSystem.instance != null && ZoneSystem.instance.GetSolidHeight(destination, out var height))
        {
            destination.y = height;
        }

        Effects.Play(creature, Effects.BlinkStart);
        creature.transform.position = destination;
        var body = creature.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.position = destination;
        }

        creature.transform.rotation = Quaternion.LookRotation(-away.normalized);
        state.NextReady = now + Cooldown;
    }
}

/// <summary>Juggernaut: strong knockback on players with a cooldown; immune to push (see IncomingHitPatch).</summary>
internal static class Juggernaut
{
    private static ConfigEntry<float> _minimumPush = null!;
    private static ConfigEntry<float> _pushCooldown = null!;

    internal static void Bind(ConfigBinder file)
    {
        _minimumPush = ModifierValues.Number(file, "Juggernaut minimum push", 150f, 0f, 1000f, "Push force of Juggernaut hits on players.");
        _pushCooldown = ModifierValues.Number(file, "Juggernaut cooldown (s)", 5f, 0f, 120f, "Seconds between two strong pushes.");
    }

    internal static float MinimumPush => _minimumPush.Value;
    internal static float PushCooldown => _pushCooldown.Value;

    private static readonly ConditionalWeakTable<Character, StrongBox<float>> NextReady = new();

    // Runs on the attacker's client (OutgoingHitPatch).
    internal static void ApplyPush(Character attacker, HitData hit)
    {
        var next = NextReady.GetValue(attacker, _ => new StrongBox<float>(0f));
        if (Time.time < next.Value)
        {
            return;
        }

        hit.m_pushForce = Mathf.Max(hit.m_pushForce, MinimumPush);
        next.Value = Time.time + PushCooldown;
    }
}

/// <summary>
/// Blamer (adapted, no Karma): below a health threshold the creature flees and keeps alerting the
/// creatures around it, until its calls are spent; then it loses the modifier and fights again.
/// </summary>
internal static class Blamer
{
    private static ConfigEntry<float> _fleeHealthRatio = null!;
    private static ConfigEntry<float> _callRadius = null!;
    private static ConfigEntry<int> _maxCalls = null!;

    internal static void Bind(ConfigBinder file)
    {
        _fleeHealthRatio = ModifierValues.Percent(file, "Blamer flee below health (%)", 75f, 100f, "The creature flees below this health.");
        _callRadius = ModifierValues.Number(file, "Blamer call radius (m)", 30f, 1f, 100f, "Creatures within this distance are alerted.");
        _maxCalls = ModifierValues.Count(file, "Blamer max calls", 15, 1, 200, "Calls before the creature stops fleeing and loses the modifier.");
    }

    internal static float FleeHealthRatio => _fleeHealthRatio.Value / 100f;
    internal static float CallRadius => _callRadius.Value;
    internal static int MaxCalls => _maxCalls.Value;
    private const float CallInterval = 1f;
    private const string CallsKey = "cllcx.blamer.calls";

    private sealed class State
    {
        internal float OriginalFleeThreshold = -1f;
        internal float NextCall;
    }

    private static readonly ConditionalWeakTable<Character, State> States = new();
    private static readonly List<Character> Nearby = new();

    internal static int CallsRemaining(Character creature) => MaxCalls - (ModifierStore.Zdo(creature)?.GetInt(CallsKey) ?? 0);

    internal static void Update(Character creature, MonsterAI ai)
    {
        var state = States.GetOrCreateValue(creature);
        if (state.OriginalFleeThreshold < 0f)
        {
            state.OriginalFleeThreshold = ai.m_fleeIfLowHealth;
        }

        if (creature.GetHealthPercentage() >= FleeHealthRatio)
        {
            return;
        }

        ai.m_fleeIfLowHealth = FleeHealthRatio;
        if (Time.time < state.NextCall)
        {
            return;
        }

        state.NextCall = Time.time + CallInterval;
        Nearby.Clear();
        Character.GetCharactersInRange(creature.transform.position, CallRadius, Nearby);
        foreach (var other in Nearby)
        {
            if (other != creature && !other.IsPlayer() && !other.IsTamed() && !other.IsDead())
            {
                other.GetComponent<BaseAI>()?.Alert();
            }
        }

        var zdo = ModifierStore.Zdo(creature);
        var calls = (zdo?.GetInt(CallsKey) ?? 0) + 1;
        zdo?.Set(CallsKey, calls);
        if (calls >= MaxCalls)
        {
            ai.m_fleeIfLowHealth = state.OriginalFleeThreshold;
            ModifierStore.Remove(creature, CreatureModifiers.Blamer);
        }
    }
}

/// <summary>Runs the modifiers driven by the AI, on the client that owns the creature.</summary>
[HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
internal static class ModifierAiPatch
{
    [HarmonyPostfix]
    private static void Postfix(MonsterAI __instance)
    {
        var creature = __instance.GetComponent<Character>();
        var modifiers = creature != null ? ModifierStore.Get(creature) : CreatureModifiers.None;
        if (modifiers == CreatureModifiers.None || creature!.IsDead())
        {
            return;
        }

        if ((modifiers & CreatureModifiers.Chameleon) != 0)
        {
            DefenseModifiers.UpdateChameleon(creature, __instance);
        }

        if ((modifiers & CreatureModifiers.Blink) != 0)
        {
            Blink.Update(creature, __instance);
        }

        if ((modifiers & CreatureModifiers.Blamer) != 0)
        {
            Blamer.Update(creature, __instance);
        }
    }
}
