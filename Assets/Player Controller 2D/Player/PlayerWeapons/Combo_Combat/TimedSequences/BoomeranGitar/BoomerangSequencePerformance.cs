using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BoomerangSequencePerformance
{
    [Serializable]
    public class ActionHitCounters
    {
        public int outboundHitEvents;
        public int outboundUniqueEnemies;

        public int returningHitEvents;
        public int returningUniqueEnemies;

        public int reflectHoldHitEvents;
        public int reflectHoldUniqueEnemies;

        public int reflectedOutboundHitEvents;
        public int reflectedOutboundUniqueEnemies;

        public int orbitRewardHitEvents;
        public int orbitRewardUniqueEnemies;

        public void Reset()
        {
            outboundHitEvents = 0;
            outboundUniqueEnemies = 0;
            returningHitEvents = 0;
            returningUniqueEnemies = 0;
            reflectHoldHitEvents = 0;
            reflectHoldUniqueEnemies = 0;
            reflectedOutboundHitEvents = 0;
            reflectedOutboundUniqueEnemies = 0;
            orbitRewardHitEvents = 0;
            orbitRewardUniqueEnemies = 0;
        }

        public ActionHitCounters Clone()
        {
            return new ActionHitCounters
            {
                outboundHitEvents = outboundHitEvents,
                outboundUniqueEnemies = outboundUniqueEnemies,
                returningHitEvents = returningHitEvents,
                returningUniqueEnemies = returningUniqueEnemies,
                reflectHoldHitEvents = reflectHoldHitEvents,
                reflectHoldUniqueEnemies = reflectHoldUniqueEnemies,
                reflectedOutboundHitEvents = reflectedOutboundHitEvents,
                reflectedOutboundUniqueEnemies = reflectedOutboundUniqueEnemies,
                orbitRewardHitEvents = orbitRewardHitEvents,
                orbitRewardUniqueEnemies = orbitRewardUniqueEnemies
            };
        }

        public void Register(BoomerangDamageActionType actionType, bool countsAsNewUnique)
        {
            switch (actionType)
            {
                case BoomerangDamageActionType.Outbound:
                    outboundHitEvents++;
                    if (countsAsNewUnique) outboundUniqueEnemies++;
                    break;

                case BoomerangDamageActionType.Returning:
                    returningHitEvents++;
                    if (countsAsNewUnique) returningUniqueEnemies++;
                    break;

                case BoomerangDamageActionType.ReflectHold:
                    reflectHoldHitEvents++;
                    if (countsAsNewUnique) reflectHoldUniqueEnemies++;
                    break;

                case BoomerangDamageActionType.ReflectedOutbound:
                    reflectedOutboundHitEvents++;
                    if (countsAsNewUnique) reflectedOutboundUniqueEnemies++;
                    break;

                case BoomerangDamageActionType.OrbitReward:
                    orbitRewardHitEvents++;
                    if (countsAsNewUnique) orbitRewardUniqueEnemies++;
                    break;
            }
        }
    }

    [Serializable]
    public class CycleSnapshot
    {
        public int cycleNumber;
        public int hitEvents;
        public int uniqueEnemiesDamaged;
        public ActionHitCounters actionCounters = new();
    }

    [Header("Totals")]
    [SerializeField] private int totalHitEvents;
    [SerializeField] private int totalUniqueEnemiesDamaged;

    [Header("Current Cycle")]
    [SerializeField] private int currentCycleNumber;
    [SerializeField] private int currentCycleHitEvents;
    [SerializeField] private int currentCycleUniqueEnemiesDamaged;
    [SerializeField] private ActionHitCounters currentCycleActionCounters = new();

    [Header("Aggregate By Action")]
    [SerializeField] private ActionHitCounters totalActionCounters = new();

    [Header("Completed Cycles")]
    [SerializeField] private List<CycleSnapshot> completedCycles = new();

    private readonly HashSet<int> totalUniqueEnemyIds = new();
    private readonly HashSet<int> currentCycleUniqueEnemyIds = new();

    public int TotalHitEvents => totalHitEvents;
    public int TotalUniqueEnemiesDamaged => totalUniqueEnemiesDamaged;
    public int CurrentCycleNumber => currentCycleNumber;
    public int CurrentCycleHitEvents => currentCycleHitEvents;
    public int CurrentCycleUniqueEnemiesDamaged => currentCycleUniqueEnemiesDamaged;
    public ActionHitCounters CurrentCycleActionCounters => currentCycleActionCounters;
    public ActionHitCounters TotalActionCounters => totalActionCounters;
    public IReadOnlyList<CycleSnapshot> CompletedCycles => completedCycles;

    public void ResetAll()
    {
        totalHitEvents = 0;
        totalUniqueEnemiesDamaged = 0;

        currentCycleNumber = 0;
        currentCycleHitEvents = 0;
        currentCycleUniqueEnemiesDamaged = 0;

        currentCycleActionCounters.Reset();
        totalActionCounters.Reset();
        completedCycles.Clear();

        totalUniqueEnemyIds.Clear();
        currentCycleUniqueEnemyIds.Clear();
    }

    public void BeginCycle(int cycleNumber)
    {
        currentCycleNumber = Mathf.Max(1, cycleNumber);
        currentCycleHitEvents = 0;
        currentCycleUniqueEnemiesDamaged = 0;
        currentCycleActionCounters.Reset();
        currentCycleUniqueEnemyIds.Clear();
    }

    public void RegisterDamage(Collider2D target, BoomerangDamageActionType actionType)
    {
        if (target == null)
            return;

        int id = target.GetInstanceID();

        bool isNewUniqueTotal = totalUniqueEnemyIds.Add(id);
        bool isNewUniqueCycle = currentCycleUniqueEnemyIds.Add(id);

        totalHitEvents++;
        currentCycleHitEvents++;

        if (isNewUniqueTotal)
            totalUniqueEnemiesDamaged++;

        if (isNewUniqueCycle)
            currentCycleUniqueEnemiesDamaged++;

        totalActionCounters.Register(actionType, isNewUniqueTotal);
        currentCycleActionCounters.Register(actionType, isNewUniqueCycle);
    }

    public void CommitCurrentCycle()
    {
        if (currentCycleNumber <= 0)
            return;

        completedCycles.Add(new CycleSnapshot
        {
            cycleNumber = currentCycleNumber,
            hitEvents = currentCycleHitEvents,
            uniqueEnemiesDamaged = currentCycleUniqueEnemiesDamaged,
            actionCounters = currentCycleActionCounters.Clone()
        });
    }
}