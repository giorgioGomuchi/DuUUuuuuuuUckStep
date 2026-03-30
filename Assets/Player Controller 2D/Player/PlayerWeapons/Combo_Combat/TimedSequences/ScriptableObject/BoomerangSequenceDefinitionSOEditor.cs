#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BoomerangSequenceDefinitionSO))]
public class BoomerangSequenceDefinitionSOEditor : Editor
{
    private SerializedProperty sequenceIdProp;
    private SerializedProperty recallRuleProp;
    private SerializedProperty reflectRuleProp;
    private SerializedProperty dashRuleProp;

    private SerializedProperty recallWindowDurationProp;
    private SerializedProperty returnToReflectDurationProp;
    private SerializedProperty reflectWindowDurationProp;
    private SerializedProperty uiPhaseTransitionHoldDurationProp;
    private SerializedProperty reflectActivationNormalizedProp;
    private SerializedProperty requiredSuccessfulCyclesProp;

    private SerializedProperty allowDashDuringRecallProp;
    private SerializedProperty allowDashDuringReflectProp;
    private SerializedProperty failOnBadDashProp;

    private SerializedProperty failOnSwitchWeaponInputProp;
    private SerializedProperty clearWeaponOverrideOnFailProp;

    private SerializedProperty destroyProjectileOnFailProp;
    private SerializedProperty destroyProjectileOnFailDelayProp;

    private SerializedProperty playerUIWorldOffsetProp;

    private SerializedProperty modularRewardPolicyProp;
    private SerializedProperty useOrbitRewardProp;
    private SerializedProperty orbitDurationProp;
    private SerializedProperty orbitTurnsProp;
    private SerializedProperty rewardPolicyProp;

    private void OnEnable()
    {
        sequenceIdProp = serializedObject.FindProperty("sequenceId");
        recallRuleProp = serializedObject.FindProperty("recallRule");
        reflectRuleProp = serializedObject.FindProperty("reflectRule");
        dashRuleProp = serializedObject.FindProperty("dashRule");

        recallWindowDurationProp = serializedObject.FindProperty("recallWindowDuration");
        returnToReflectDurationProp = serializedObject.FindProperty("returnToReflectDuration");
        reflectWindowDurationProp = serializedObject.FindProperty("reflectWindowDuration");
        uiPhaseTransitionHoldDurationProp = serializedObject.FindProperty("uiPhaseTransitionHoldDuration");
        reflectActivationNormalizedProp = serializedObject.FindProperty("reflectActivationNormalized");

        allowDashDuringRecallProp = serializedObject.FindProperty("allowDashDuringRecall");
        allowDashDuringReflectProp = serializedObject.FindProperty("allowDashDuringReflect");
        failOnBadDashProp = serializedObject.FindProperty("failOnBadDash");

        failOnSwitchWeaponInputProp = serializedObject.FindProperty("failOnSwitchWeaponInput");
        clearWeaponOverrideOnFailProp = serializedObject.FindProperty("clearWeaponOverrideOnFail");

        destroyProjectileOnFailProp = serializedObject.FindProperty("destroyProjectileOnFail");
        destroyProjectileOnFailDelayProp = serializedObject.FindProperty("destroyProjectileOnFailDelay");

        playerUIWorldOffsetProp = serializedObject.FindProperty("playerUIWorldOffset");


        requiredSuccessfulCyclesProp = serializedObject.FindProperty("requiredSteps");
        modularRewardPolicyProp = serializedObject.FindProperty("rewardPolicy");
        useOrbitRewardProp = serializedObject.FindProperty("useOrbitReward");
        orbitDurationProp = serializedObject.FindProperty("orbitDuration");
        orbitTurnsProp = serializedObject.FindProperty("orbitTurns");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(sequenceIdProp);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Rules", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(recallRuleProp, true);
        EditorGUILayout.PropertyField(reflectRuleProp, true);
        EditorGUILayout.PropertyField(dashRuleProp, true);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Main Timing", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(recallWindowDurationProp);
        EditorGUILayout.PropertyField(returnToReflectDurationProp);
        EditorGUILayout.PropertyField(reflectWindowDurationProp);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("UI / Transition", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(uiPhaseTransitionHoldDurationProp);
        EditorGUILayout.PropertyField(reflectActivationNormalizedProp);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Progress", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(requiredSuccessfulCyclesProp);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Dash Behaviour", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(allowDashDuringRecallProp);
        EditorGUILayout.PropertyField(allowDashDuringReflectProp);
        EditorGUILayout.PropertyField(failOnBadDashProp);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Cancel / Fail", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(failOnSwitchWeaponInputProp);
        EditorGUILayout.PropertyField(clearWeaponOverrideOnFailProp);
        EditorGUILayout.PropertyField(destroyProjectileOnFailProp);

        if (destroyProjectileOnFailProp.boolValue)
            EditorGUILayout.PropertyField(destroyProjectileOnFailDelayProp);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("UI", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(playerUIWorldOffsetProp);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Reward", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(useOrbitRewardProp);

        if (useOrbitRewardProp.boolValue)
        {
            EditorGUILayout.PropertyField(orbitDurationProp);
            EditorGUILayout.PropertyField(orbitTurnsProp);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Legacy Reward Policy", EditorStyles.miniBoldLabel);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Modular Reward Policy", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(modularRewardPolicyProp);


            if (modularRewardPolicyProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "Modular Reward Policy is empty. New modular reward preview/gating will be disabled.",
                    MessageType.Info);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif