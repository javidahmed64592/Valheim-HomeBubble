using System.Collections.Generic;

using UnityEngine;

namespace HomeBubble.Bubble;

/// <summary>
/// <para>
/// A monster that was fighting something outside a bubble, which then ran inside, attacks the bubble for a while
/// instead of fleeing: it walks up to the edge on the side nearest it and strikes the wall, or shoots at it from
/// range. Melee hits use up bones at the same rate per point of damage as projectiles hitting the shield, and
/// projectiles hit the shield as they always do.
/// </para>
/// <para>
/// It only starts when the target crosses into the bubble during a fight, so monsters that come across a base with
/// players already inside keep away as before. The siege ends when its time runs out, the bubble goes down, or the
/// target leaves the bubble (the monster then goes back to chasing it). At the end the monster loses interest and
/// wanders off rather than starting again, until it sees its target outside once more.
/// </para>
/// </summary>
internal static class BubbleSiege
{
    /// <summary>How recently the target must have been outside for its running in to count as escaping a fight.</summary>
    private const float ChaseMemory = 5f;

    /// <summary>A monster further than this beyond the edge when its target ran in wasn't close enough to follow.</summary>
    private const float MaxStartDistance = 30f;

    /// <summary>Seconds from the start of a melee swing to when it lands on the wall.</summary>
    private const float MeleeHitDelay = 0.5f;

    private const float SweepInterval = 10f;

    private sealed class State
    {
        public Character Target;
        public float OutsideAt = float.NegativeInfinity;
        public HomeBubbleZone Bubble;
        public float Until;
        public float HitAt = -1f;
        public float HitDamage;
        public Vector3 HitPoint;
    }

    private static readonly Dictionary<MonsterAI, State> States = [];
    private static readonly List<MonsterAI> Dead = [];
    private static float _nextSweep;

    /// <summary>Runs after the monster's own AI. Returns true if the monster is besieging a bubble this frame.</summary>
    public static bool Update(MonsterAI ai, Character character, float dt)
    {
        Sweep();

        Character target = ai.m_targetCreature;
        if (!BubbleConfig.AttackBubble.Value || target == null || character.IsFlying() || character is not Humanoid humanoid)
        {
            _ = States.Remove(ai);
            return false;
        }

        if (!States.TryGetValue(ai, out State state))
        {
            state = new State();
            States[ai] = state;
        }

        if (state.Target != target)
        {
            state.Target = target;
            state.OutsideAt = float.NegativeInfinity;
            state.Bubble = null;
        }

        HomeBubbleZone bubble = HomeBubbleZone.FindBubbleInside(target.transform.position);
        if (bubble == null)
        {
            // Out in the open: the ordinary chase, which remembers that the target was outside.
            state.OutsideAt = Time.time;
            state.Bubble = null;
            return false;
        }

        Vector3 position = character.transform.position;
        if (state.Bubble != bubble)
        {
            float beyondEdge = Utils.DistanceXZ(position, bubble.Centre) - bubble.Radius;
            if (Time.time - state.OutsideAt > ChaseMemory || !ai.IsAlerted() || beyondEdge > MaxStartDistance)
            {
                return false;
            }

            state.Bubble = bubble;
            state.Until = Time.time + BubbleConfig.SiegeSeconds.Value;
            state.HitAt = -1f;
        }

        if (Time.time >= state.Until || !bubble.IsUp)
        {
            GiveUp(ai, state);
            return false;
        }

        LandPendingHit(character, state);
        Besiege(ai, character, humanoid, state, dt);
        return true;
    }

    private static void Besiege(MonsterAI ai, Character character, Humanoid humanoid, State state, float dt)
    {
        HomeBubbleZone bubble = state.Bubble;
        ItemDrop.ItemData weapon = ai.SelectBestAttack(humanoid, dt);
        if (weapon == null)
        {
            return;
        }

        Vector3 position = character.transform.position;
        Vector3 centre = bubble.Centre;
        float radius = bubble.Radius;
        Vector3 outward = position - centre;
        outward.y = 0f;
        outward = outward.sqrMagnitude < 0.01f ? Vector3.forward : outward.normalized;

        bool ranged = weapon.m_shared.m_attack.m_attackType == Attack.AttackType.Projectile;
        float range = weapon.m_shared.m_aiAttackRange;
        float standOff = ranged ? Mathf.Clamp(range * 0.6f, 4f, 20f) : Mathf.Max(0.8f, range * 0.5f);
        float height = character.GetCenterPoint().y;

        // Aim just inside the wall, at chest height, so swings and shots go into it.
        Vector3 aim = centre + (outward * (radius - 1f));
        aim.y = height;

        float distance = Utils.DistanceXZ(position, centre);
        if (distance < radius + 0.3f || distance > radius + standOff + 0.5f)
        {
            Vector3 stand = centre + (outward * (radius + standOff));
            stand.y = position.y;
            _ = ai.MoveTo(dt, stand, 0.5f, true);
            return;
        }

        ai.StopMoving();
        ai.LookAt(aim);

        bool ready = !character.InAttack() && !ai.IsTakingOff()
            && Time.time - weapon.m_lastAttackTime > weapon.m_shared.m_aiAttackInterval
            && character.GetTimeSinceLastAttack() >= ai.m_minAttackInterval
            && ai.CanUseAttack(weapon)
            && ai.IsLookingAt(aim, weapon.m_shared.m_aiAttackMaxAngle);
        if (!ready || !character.StartAttack(null, false))
        {
            return;
        }

        ai.m_timeSinceAttacking = 0f;
        if (!ranged)
        {
            Vector3 wall = centre + (outward * radius);
            wall.y = height;
            state.HitAt = Time.time + MeleeHitDelay;
            state.HitDamage = weapon.GetDamage().GetTotalDamage();
            state.HitPoint = wall;
        }
    }

    private static void LandPendingHit(Character character, State state)
    {
        if (state.HitAt < 0f || Time.time < state.HitAt)
        {
            return;
        }

        state.HitAt = -1f;
        if (!character.IsDead())
        {
            state.Bubble.TakeHit(state.HitPoint, state.HitDamage);
        }
    }

    /// <summary>The monster loses interest; with no target, vanilla has it keep its distance from the bubble.</summary>
    private static void GiveUp(MonsterAI ai, State state)
    {
        state.Bubble = null;
        state.OutsideAt = float.NegativeInfinity;
        state.HitAt = -1f;
        ai.SetAlerted(false);
        ai.m_targetCreature = null;
        ai.m_targetStatic = null;
        state.Target = null;
    }

    /// <summary>Forgets monsters that have died or unloaded.</summary>
    private static void Sweep()
    {
        if (Time.time < _nextSweep)
        {
            return;
        }

        _nextSweep = Time.time + SweepInterval;
        foreach (MonsterAI ai in States.Keys)
        {
            if (ai == null)
            {
                Dead.Add(ai);
            }
        }

        foreach (MonsterAI ai in Dead)
        {
            _ = States.Remove(ai);
        }

        Dead.Clear();
    }
}
