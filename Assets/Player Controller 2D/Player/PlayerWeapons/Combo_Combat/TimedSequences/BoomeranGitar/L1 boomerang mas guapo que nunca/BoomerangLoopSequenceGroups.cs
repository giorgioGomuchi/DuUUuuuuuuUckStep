using UnityEngine;

[System.Serializable]
public class BoomerangLoopRecallSettings
{
    [Tooltip("Timing window shared by L1 recall and R2 shot redirect arming.")]
    public TimedSequenceActionRule recallRule = new();

    [Min(0.05f)]
    [Tooltip("Total duration of the recall timing window.")]
    public float recallWindowDuration = 1.2f;
}

[System.Serializable]
public class BoomerangLoopRedirectShotSettings
{

    [Min(1f)]
    [Tooltip("Temporary hit radius multiplier while shot redirect is active.")]
    public float damageRadiusMultiplier = 1.5f;

    [Min(0f)]
    [Tooltip("Rotation speed for the redirect aura root while shot redirect is active.")]
    public float auraSpinSpeedDegPerSec = 540f;
    [Min(0f)]
    [Tooltip("Fixed redirect angle applied when the bullet hits the boomerang. Sign is decided by hit side.")]
    public float redirectAngleDegrees = 35f;

    [Range(0f, 1f)]
    [Tooltip("How much the shot redirect keeps blending toward the new direction over time.")]
    public float redirectDirectionBlend = 0.42f;

    [Min(0.01f)]
    [Tooltip("How long the redirect blend lasts before settling into the redirected direction.")]
    public float redirectBlendDuration = 0.16f;

    [Min(0.01f)]
    [Tooltip("How long the armed R2 redirect stays valid while waiting for the bullet to hit.")]
    public float redirectWindowDuration = 0.4f;

    [Min(0.01f)]
    [Tooltip("Duration of the short outbound redirected segment after a successful shot redirect.")]
    public float redirectedOutboundDuration = 0.3f;

    [Min(0.05f)]
    [Tooltip("Duration of the second recall window shown after the redirected outbound segment.")]
    public float postRedirectRecallWindowDuration = 1.00f;
}

[System.Serializable]
public class BoomerangLoopCatchSettings
{
    [Min(0.05f)]
    [Tooltip("How long the player must hold L1 while the boomerang is returning before catch decision starts.")]
    public float returnHoldDuration = 0.6f;

    [Tooltip("Timing window used by the catch decision phase. L1 accepts Good/Perfect, L2 uses Perfect only in current logic.")]
    public TimedSequenceActionRule decisionRule = new();

    [Min(0.05f)]
    [Tooltip("Total duration of the catch decision timing window.")]
    public float decisionWindowDuration = 0.5f;
}

[System.Serializable]
public class BoomerangLoopDecisionSettings
{
    [Min(0.01f)]
    [Tooltip("Short grace period that lets L2 override a just-released L1 during catch decision.")]
    public float releaseToReflectGraceSeconds = 0.05f;
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

    [Min(0)]
    public int minSuccessfulLoopsForReward = 3;

    [Min(0)]
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