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
/// players already inside keep away as before. While it lasts, the monster's own AI is skipped: left running, it
/// tries to flee from the target inside, picks weapons by the distance to the target rather than the wall, and can
/// switch to attacking buildings. The siege ends when its time runs out, the bubble goes down, or the target dies or
/// leaves the bubble (the monster then goes back to chasing it). When time runs out the monster loses interest and
/// wanders off, and doesn't start again until it sees its target outside once more.
/// </para>
/// </summary>
internal static class BubbleSiege
{
    /// <summary>How recently the target must have been outside for its running in to count as escaping a fight.</summary>
    private const float ChaseMemory = 5f;

    /// <summary>A monster further than this beyond the edge when its target ran in wasn't close enough to follow.</summary>
    private const float MaxStartDistance = 30f;

    /// <summary>Allies within this distance of the monster that starts a siege join it.</summary>
    private const float JoinRange = 20f;

    /// <summary>A target this far from the monster (teleported away, say) ends the siege.</summary>
    private const float MaxTargetDistance = 80f;

    /// <summary>How far past its stand-off spot a monster can be and still attack, in metres.</summary>
    private const float StandTolerance = 0.5f;

    /// <summary>Seconds from the start of a melee swing to when it lands on the wall.</summary>
    private const float MeleeHitDelay = 0.5f;

    private const float SweepInterval = 10f;

    private sealed class State
    {
        public Character Target;
        public float OutsideAt = float.NegativeInfinity;

        /// <summary>The bubble under siege; null when there is no siege.</summary>
        public HomeBubbleZone Bubble;
        public float Until;
        public float StandOff;
        public float HitAt = -1f;
        public float HitDamage;
        public Vector3 HitPoint;
    }

    private static readonly Dictionary<MonsterAI, State> States = [];
    private static readonly List<MonsterAI> Dead = [];
    private static float _nextSweep;

    public static bool IsBesieging(MonsterAI ai)
    {
        return States.TryGetValue(ai, out State state) && state.Bubble != null;
    }

    /// <summary>
    /// Runs after the monster's own AI when it is not besieging: remembers when its target was last outside, and
    /// starts a siege if the target has just run into a bubble. Returns true if a siege started.
    /// </summary>
    public static bool Watch(MonsterAI ai, Character character)
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
        }

        HomeBubbleZone bubble = HomeBubbleZone.FindBubbleInside(target.transform.position);
        if (bubble == null)
        {
            state.OutsideAt = Time.time;
            return false;
        }

        float beyondEdge = Utils.DistanceXZ(character.transform.position, bubble.Centre) - bubble.Radius;
        if (Time.time - state.OutsideAt > ChaseMemory || !ai.IsAlerted() || beyondEdge > MaxStartDistance
            || !Begin(ai, humanoid, state, bubble))
        {
            return false;
        }

        Recruit(ai, character, target, bubble);
        return true;
    }

    /// <summary>
    /// A besieging monster hit by a player keeps going: its siege time starts over, so fighting back from inside
    /// holds the monsters at the wall, while ignoring them lets them give up.
    /// </summary>
    public static void OnDamaged(MonsterAI ai, Character attacker)
    {
        if (attacker != null && attacker.IsPlayer() && States.TryGetValue(ai, out State state) && state.Bubble != null)
        {
            state.Until = Time.time + BubbleConfig.SiegeSeconds.Value;
        }
    }

    private static bool Begin(MonsterAI ai, Humanoid humanoid, State state, HomeBubbleZone bubble)
    {
        if (!TryGetStandOff(ai, humanoid, out float standOff))
        {
            return false;
        }

        state.Bubble = bubble;
        state.Until = Time.time + BubbleConfig.SiegeSeconds.Value;
        state.StandOff = standOff;
        state.HitAt = -1f;
        HomeBubblePlugin.Log.LogInfo(humanoid.m_name + " attacks the bubble, standing "
            + standOff.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " m out");
        return true;
    }

    /// <summary>
    /// Brings in the starter's allies nearby that are already in a fight, so a whole pack hits the wall. Only
    /// monsters this game runs the AI for can be told what to do; the rest join on their own if they were chasing
    /// the same target.
    /// </summary>
    private static void Recruit(MonsterAI starter, Character starterCharacter, Character target, HomeBubbleZone bubble)
    {
        Vector3 origin = starterCharacter.transform.position;
        foreach (BaseAI other in BaseAI.BaseAIInstances)
        {
            if (other is not MonsterAI ai || ai == starter || IsBesieging(ai) || !ai.IsAlerted()
                || ai.m_nview == null || !ai.m_nview.IsValid() || !ai.m_nview.IsOwner()
                || ai.m_character is not Humanoid humanoid || humanoid.IsDead() || humanoid.IsTamed() || humanoid.IsFlying()
                || humanoid.GetFaction() != starterCharacter.GetFaction() || !ai.IsEnemy(target)
                || Vector3.Distance(origin, humanoid.transform.position) > JoinRange)
            {
                continue;
            }

            if (!States.TryGetValue(ai, out State state))
            {
                state = new State();
                States[ai] = state;
            }

            state.Target = target;
            if (Begin(ai, humanoid, state, bubble))
            {
                ai.m_targetCreature = target;
                ai.m_targetStatic = null;
            }
        }
    }

    /// <summary>
    /// Runs in place of the monster's own AI while it besieges a bubble. Returns false when there is no siege (or it
    /// has just ended), so the monster's own AI runs instead.
    /// </summary>
    public static bool Run(MonsterAI ai, float dt)
    {
        if (!States.TryGetValue(ai, out State state) || state.Bubble == null)
        {
            return false;
        }

        Character character = ai.m_character;
        Character target = state.Target;
        if (ai.m_nview == null || !ai.m_nview.IsValid() || !ai.m_nview.IsOwner() || character == null
            || character is not Humanoid humanoid || !BubbleConfig.AttackBubble.Value)
        {
            state.Bubble = null;
            return false;
        }

        if (character.IsDead() || target == null || target.IsDead() || state.Bubble == null || !state.Bubble.IsUp
            || Time.time >= state.Until
            || Vector3.Distance(character.transform.position, target.transform.position) > MaxTargetDistance)
        {
            GiveUp(ai, state);
            return false;
        }

        HomeBubbleZone bubble = HomeBubbleZone.FindBubbleInside(target.transform.position);
        if (bubble == null)
        {
            // The target came back out: back to the ordinary chase.
            state.Bubble = null;
            state.OutsideAt = Time.time;
            return false;
        }

        // Overlapping bubbles: go for the one the target is in now.
        state.Bubble = bubble;

        // What the skipped base AI would have done, and keep the monster on its target so it isn't dropped or
        // swapped for a building once the siege is over.
        ai.m_timeSinceHurt += dt;
        ai.UpdateRegeneration(dt);
        ai.m_targetCreature = target;
        ai.m_targetStatic = null;
        ai.m_timeSinceSensedTargetCreature = 0f;

        LandPendingHit(character, state);
        Besiege(ai, character, humanoid, state, dt);
        return true;
    }

    private static void Besiege(MonsterAI ai, Character character, Humanoid humanoid, State state, float dt)
    {
        HomeBubbleZone bubble = state.Bubble;
        Vector3 position = character.transform.position;
        Vector3 centre = bubble.Centre;
        float radius = bubble.Radius;
        Vector3 outward = position - centre;
        outward.y = 0f;
        outward = outward.sqrMagnitude < 0.01f ? Vector3.forward : outward.normalized;

        // Anywhere from the wall out to a little past the stand-off spot will do, so a monster that arrives
        // slightly off it attacks rather than shuffling back and forth.
        float toWall = Utils.DistanceXZ(position, centre) - radius;
        if (toWall < 0f || toWall > state.StandOff + StandTolerance)
        {
            Vector3 stand = centre + (outward * (radius + state.StandOff));
            stand.y = position.y;
            _ = ai.MoveTo(dt, stand, StandTolerance * 0.5f, true);
            return;
        }

        // Aim just inside the wall, at chest height, so swings and shots go into it.
        float height = character.GetCenterPoint().y;
        Vector3 aim = centre + (outward * (radius - 1f));
        aim.y = height;

        ai.StopMoving();
        ai.LookAt(aim);

        ItemDrop.ItemData weapon = PickWeapon(ai, humanoid, toWall);
        if (weapon == null)
        {
            return;
        }

        bool ready = !character.InAttack() && !ai.IsTakingOff()
            && Time.time - weapon.m_lastAttackTime > weapon.m_shared.m_aiAttackInterval
            && character.GetTimeSinceLastAttack() >= ai.m_minAttackInterval
            && ai.IsLookingAt(aim, Mathf.Max(weapon.m_shared.m_aiAttackMaxAngle, 10f));
        if (!ready || !character.StartAttack(null, false))
        {
            return;
        }

        ai.m_timeSinceAttacking = 0f;
        if (!IsRanged(weapon))
        {
            Vector3 wall = centre + (outward * radius);
            wall.y = height;
            state.HitAt = Time.time + MeleeHitDelay;
            state.HitDamage = HitDamage(character, weapon);
            state.HitPoint = wall;
        }
    }

    /// <summary>
    /// Equips an attack that reaches the wall from where the monster stands and is off cooldown, preferring the ones
    /// the monster's own AI prefers. Keeps the current weapon if none is ready, so the monster waits for it.
    /// </summary>
    private static ItemDrop.ItemData PickWeapon(MonsterAI ai, Humanoid humanoid, float toWall)
    {
        ItemDrop.ItemData current = humanoid.GetCurrentWeapon();
        if (humanoid.InAttack())
        {
            return current;
        }

        ItemDrop.ItemData pick = null;
        ItemDrop.ItemData fallback = null;
        foreach (ItemDrop.ItemData item in humanoid.GetInventory().GetAllItems())
        {
            if (!IsEnemyAttack(ai, item) || toWall < item.m_shared.m_aiAttackRangeMin
                || toWall > item.m_shared.m_aiAttackRange)
            {
                continue;
            }

            fallback ??= item;
            if (Time.time - item.m_lastAttackTime > item.m_shared.m_aiAttackInterval
                && (pick == null || (item.m_shared.m_aiPrioritized && !pick.m_shared.m_aiPrioritized)))
            {
                pick = item;
            }
        }

        pick ??= current != null && IsEnemyAttack(ai, current) ? current : fallback;
        if (pick != null && pick != current)
        {
            _ = humanoid.EquipItem(pick);
        }

        return pick;
    }

    /// <summary>
    /// How far outside the wall to stand: close enough for every melee attack to reach it, or for a monster with
    /// only ranged attacks, well within range. False if the monster has no attack that can hit the wall.
    /// </summary>
    private static bool TryGetStandOff(MonsterAI ai, Humanoid humanoid, out float standOff)
    {
        float melee = float.MaxValue;
        float ranged = float.MaxValue;
        foreach (ItemDrop.ItemData item in humanoid.GetInventory().GetAllItems())
        {
            if (!IsEnemyAttack(ai, item))
            {
                continue;
            }

            if (IsRanged(item))
            {
                ranged = Mathf.Min(ranged, item.m_shared.m_aiAttackRange);
            }
            else
            {
                melee = Mathf.Min(melee, item.m_shared.m_aiAttackRange);
            }
        }

        // Far enough in from the longest reach allowed (stand-off plus tolerance) for the shortest attack to land.
        standOff = melee < float.MaxValue
            ? Mathf.Clamp((melee * 0.6f) - 0.2f, 0.2f, melee - StandTolerance)
            : Mathf.Clamp(ranged * 0.6f, 4f, 20f);
        return melee < float.MaxValue || ranged < float.MaxValue;
    }

    /// <summary>The damage the swing would do to a player: the weapon's, scaled by star level and world difficulty.</summary>
    private static float HitDamage(Character character, ItemDrop.ItemData weapon)
    {
        float level = 1f + (Mathf.Max(0, character.GetLevel() - 1) * 0.5f);
        return weapon.GetDamage().GetTotalDamage() * level * Game.m_enemyDamageRate;
    }

    private static bool IsEnemyAttack(MonsterAI ai, ItemDrop.ItemData item)
    {
        return item.IsWeapon() && item.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.Enemy && ai.CanUseAttack(item);
    }

    private static bool IsRanged(ItemDrop.ItemData item)
    {
        return item.m_shared.m_attack.m_attackType == Attack.AttackType.Projectile;
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
        HomeBubblePlugin.Log.LogInfo(ai.m_character.m_name + " stops attacking the bubble");
        state.Bubble = null;
        state.Target = null;
        state.OutsideAt = float.NegativeInfinity;
        state.HitAt = -1f;
        ai.SetAlerted(false);
        ai.m_targetCreature = null;
        ai.m_targetStatic = null;
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
