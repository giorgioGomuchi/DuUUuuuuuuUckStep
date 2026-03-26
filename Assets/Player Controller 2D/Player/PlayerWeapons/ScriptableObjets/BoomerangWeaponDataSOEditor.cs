#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BoomerangWeaponDataSO))]
public class BoomerangWeaponDataSOEditor : Editor
{
    private SerializedProperty sequenceDefinitionProp;
    private SerializedProperty projectileConfigProp;

    private bool showSequence = true;
    private bool showProjectile = true;

    private void OnEnable()
    {
        sequenceDefinitionProp = serializedObject.FindProperty("sequenceDefinition");
        projectileConfigProp = serializedObject.FindProperty("projectileConfig");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawBaseWeaponSection();

        EditorGUILayout.Space(8f);

        showSequence = EditorGUILayout.BeginFoldoutHeaderGroup(showSequence, "Boomerang Sequence");
        if (showSequence)
        {
            EditorGUILayout.PropertyField(sequenceDefinitionProp);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(6f);

        showProjectile = EditorGUILayout.BeginFoldoutHeaderGroup(showProjectile, "Projectile Feel / Visuals");
        if (showProjectile)
        {
            DrawProjectileConfig(projectileConfigProp);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawBaseWeaponSection()
    {
        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",
            "sequenceDefinition",
            "projectileConfig");
    }

    private void DrawProjectileConfig(SerializedProperty root)
    {
        if (root == null)
            return;

        DrawChild(root, "outboundDistance");
        DrawChild(root, "outboundDistanceAfterDeflect");
        DrawChild(root, "deflectOnlyWhileReturning");
        DrawChild(root, "holdReflectAtOwnerCenter");

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Return Presentation", EditorStyles.boldLabel);
        DrawChild(root, "timedReturnArcStrength");
        DrawChild(root, "timedReturnPresentationDistance");
        DrawChild(root, "driftDeceleration");

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Dash Bonuses", EditorStyles.boldLabel);
        DrawChild(root, "dashReturnSpeedMultiplierBonus");
        DrawChild(root, "dashReturnSteeringBonus");
        DrawChild(root, "dashReflectSpeedMultiplierBonus");

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Projectile Interaction", EditorStyles.boldLabel);
        DrawChild(root, "destroyEnemyProjectileMask");

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Orbit Reward", EditorStyles.boldLabel);
        DrawChild(root, "orbitStartRadius");
        DrawChild(root, "orbitRadiusGrowthPerSecond");
        DrawChild(root, "orbitMaxRadius");
        DrawChild(root, "orbitAngularSpeedDegPerSec");
        DrawChild(root, "orbitSpeedMultiplier");
        DrawChild(root, "orbitClockwise");
        DrawChild(root, "orbitContactDamageInterval");

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Visual Feedback", EditorStyles.boldLabel);
        DrawChild(root, "returningColor");
        DrawChild(root, "reflectableColor");
        DrawChild(root, "reflectableFlashDuration");
        DrawChild(root, "orbitStartFlashColor");
        DrawChild(root, "orbitStartFlashDuration");
        DrawChild(root, "orbitStartPulseScaleMultiplier");
        DrawChild(root, "orbitStartPulseDuration");

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Spin", EditorStyles.boldLabel);
        DrawChild(root, "enableSpin");
        DrawChild(root, "spinDegPerSec");
    }

    private void DrawChild(SerializedProperty root, string childName)
    {
        SerializedProperty child = root.FindPropertyRelative(childName);
        if (child != null)
            EditorGUILayout.PropertyField(child);
    }
}
#endif