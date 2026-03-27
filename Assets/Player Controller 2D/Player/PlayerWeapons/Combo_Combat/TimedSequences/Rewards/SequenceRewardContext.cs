using System;

[Serializable]
public class SequenceRewardContext
{
    public int TotalHitEvents;
    public int TotalUniqueEnemiesDamaged;

    public int CompletedCycles;
    public int CurrentCycleNumber;

    public int CurrentCycleHitEvents;
    public int CurrentCycleUniqueEnemiesDamaged;

    public static SequenceRewardContext FromBoomerang(BoomerangSequencePerformance performance, int completedCycles)
    {
        if (performance == null)
        {
            return new SequenceRewardContext
            {
                TotalHitEvents = 0,
                TotalUniqueEnemiesDamaged = 0,
                CompletedCycles = completedCycles,
                CurrentCycleNumber = 0,
                CurrentCycleHitEvents = 0,
                CurrentCycleUniqueEnemiesDamaged = 0
            };
        }

        return new SequenceRewardContext
        {
            TotalHitEvents = performance.TotalHitEvents,
            TotalUniqueEnemiesDamaged = performance.TotalUniqueEnemiesDamaged,
            CompletedCycles = completedCycles,
            CurrentCycleNumber = performance.CurrentCycleNumber,
            CurrentCycleHitEvents = performance.CurrentCycleHitEvents,
            CurrentCycleUniqueEnemiesDamaged = performance.CurrentCycleUniqueEnemiesDamaged
        };
    }
}