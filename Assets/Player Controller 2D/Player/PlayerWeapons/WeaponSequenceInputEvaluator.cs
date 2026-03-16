using UnityEngine;

public static class WeaponSequenceInputEvaluator
{
    public static bool IsSwitchForbidden(WeaponSequenceDefinitionSO definition)
    {
        return definition != null && definition.FailOnSwitchWeaponInput;
    }

    public static bool IsSecondaryFireForbidden(WeaponSequenceDefinitionSO definition)
    {
        return definition != null && definition.FailOnSecondaryInput;
    }

    public static bool ShouldFailOnTimeout(WeaponSequenceDefinitionSO definition)
    {
        return definition != null && definition.FailOnTimeout;
    }

    public static TimingJudgement EvaluateShoot(WeaponSequenceDefinitionSO definition, float normalizedTime)
    {
        if (definition == null || !definition.ShootRule.Enabled)
            return TimingJudgement.Fail;

        return definition.ShootRule.Evaluate(normalizedTime);
    }

    public static TimingJudgement EvaluateDash(WeaponSequenceDefinitionSO definition, float normalizedTime)
    {
        if (definition == null || !definition.DashRule.Enabled)
            return TimingJudgement.Fail;

        return definition.DashRule.Evaluate(normalizedTime);
    }
}