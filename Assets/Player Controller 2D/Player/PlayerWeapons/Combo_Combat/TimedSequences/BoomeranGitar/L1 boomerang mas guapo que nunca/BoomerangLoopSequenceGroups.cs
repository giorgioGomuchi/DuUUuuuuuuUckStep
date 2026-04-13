using UnityEngine;

[System.Serializable]
public class BoomerangLoopRecallSettings
{
    public TimedSequenceActionRule recallRule = new();

    [Min(0.05f)]
    public float recallWindowDuration = 1.2f;
}

[System.Serializable]
public class BoomerangLoopCatchSettings
{
    [Min(0.05f)]
    public float returnHoldDuration = 0.6f;

    public TimedSequenceActionRule decisionRule = new();

    [Min(0.05f)]
    public float decisionWindowDuration = 0.5f;
}

[System.Serializable]
public class BoomerangLoopReflectSettings
{
    public TimedSequenceActionRule reflectRule = new();

    [Min(0.05f)]
    public float reflectWindowDuration = 1f;

    [Range(0f, 1f)]
    public float reflectActivationNormalized = 0.3f;
}

[System.Serializable]
public class BoomerangLoopRecoverySettings
{
    [Min(0.05f)]
    public float recoveryCooldownOnEarlyRelease = 0.25f;

    public bool allowDashDuringRecall = true;
    public bool allowDashDuringReflect = true;
    public bool failOnBadDash = false;
    public bool failOnSwitchWeaponInput = true;
    public bool clearWeaponOverrideOnFail = true;
    
    [Min(0f)]
    public float failCooldownDuration = 0.5f;

    public bool keepUIVisibleDuringFailCooldown = true;
}

[System.Serializable]
public class BoomerangLoopRewardSettings
{
    public bool requireExplicitRewardTrigger = false;

    public BoomerangLoopRewardTriggerInput rewardTriggerInput = BoomerangLoopRewardTriggerInput.OnReflectL2;

    public bool requireSuccessfulLoopCount = true;

    public int minSuccessfulLoopsForReward = 3;

    public int minReflectSuccessesForReward = 1;

    public bool useOrbitReward = true;

    [Min(0.05f)]
    public float orbitDuration = 3.5f;

    [Min(0.1f)]
    public float requiredWeightedScore = 3f;

    [Min(0.1f)]
    public float relaunchScoreWeight = 1f;

    [Min(0.1f)]
    public float reflectScoreWeight = 1.5f;
}

[System.Serializable]
public class BoomerangLoopCleanupSettings
{
    public bool destroyProjectileOnFail = true;

    [Min(0f)]
    public float destroyProjectileOnFailDelay = 0f;
}