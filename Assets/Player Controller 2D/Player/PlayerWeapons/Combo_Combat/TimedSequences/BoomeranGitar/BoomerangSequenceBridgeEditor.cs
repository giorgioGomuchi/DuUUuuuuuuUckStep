#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BoomerangSequenceBridge))]
public class BoomerangSequenceBridgeEditor : Editor
{
    private bool showLiveMetrics = true;
    private bool showTotalActions = true;
    private bool showCompletedCycles = true;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDefaultInspector();

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(10f);
        DrawLiveRuntimePanel((BoomerangSequenceBridge)target);
    }

    public override bool RequiresConstantRepaint()
    {
        return Application.isPlaying;
    }

    private void DrawLiveRuntimePanel(BoomerangSequenceBridge bridge)
    {
        EditorGUILayout.BeginVertical("box");
        showLiveMetrics = EditorGUILayout.Foldout(showLiveMetrics, "Live Boomerang Metrics", true);

        if (showLiveMetrics)
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see live boomerang performance metrics.", MessageType.Info);
            }
            else if (bridge == null || bridge.Performance == null)
            {
                EditorGUILayout.HelpBox("No live performance data available.", MessageType.Warning);
            }
            else
            {
                DrawBridgeState(bridge);
                EditorGUILayout.Space(6f);
                DrawPerformance(bridge.Performance);
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawBridgeState(BoomerangSequenceBridge bridge)
    {
        EditorGUILayout.LabelField("Sequence State", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.Toggle("Is Sequence Active", bridge.IsSequenceActive);
            EditorGUILayout.Toggle("Is In Orbit Reward", bridge.IsInOrbitReward);
            EditorGUILayout.TextField("Active Phase", bridge.ActivePhase.ToString());
            EditorGUILayout.IntField("Completed Cycles", bridge.CompletedCycles);
        }
    }

    private void DrawPerformance(BoomerangSequencePerformance perf)
    {
        EditorGUILayout.LabelField("Totals", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.IntField("Total Hit Events", perf.TotalHitEvents);
            EditorGUILayout.IntField("Total Unique Enemies", perf.TotalUniqueEnemiesDamaged);
            EditorGUILayout.IntField("Current Cycle", perf.CurrentCycleNumber);
            EditorGUILayout.IntField("Current Cycle Hit Events", perf.CurrentCycleHitEvents);
            EditorGUILayout.IntField("Current Cycle Unique Enemies", perf.CurrentCycleUniqueEnemiesDamaged);
        }

        EditorGUILayout.Space(6f);
        DrawActionCounters("Current Cycle By Action", perf.CurrentCycleActionCounters);

        EditorGUILayout.Space(6f);
        showTotalActions = EditorGUILayout.Foldout(showTotalActions, "Total By Action", true);
        if (showTotalActions)
        {
            DrawActionCounters(null, perf.TotalActionCounters);
        }

        EditorGUILayout.Space(6f);
        showCompletedCycles = EditorGUILayout.Foldout(showCompletedCycles, "Completed Cycles", true);
        if (showCompletedCycles)
        {
            if (perf.CompletedCycles == null || perf.CompletedCycles.Count == 0)
            {
                EditorGUILayout.HelpBox("No completed cycles recorded yet.", MessageType.None);
            }
            else
            {
                for (int i = 0; i < perf.CompletedCycles.Count; i++)
                {
                    DrawCycleSnapshot(perf.CompletedCycles[i], i);
                    EditorGUILayout.Space(4f);
                }
            }
        }
    }

    private void DrawCycleSnapshot(BoomerangSequencePerformance.CycleSnapshot cycle, int index)
    {
        if (cycle == null)
            return;

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"Cycle {cycle.cycleNumber}", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.IntField("Hit Events", cycle.hitEvents);
            EditorGUILayout.IntField("Unique Enemies", cycle.uniqueEnemiesDamaged);
        }

        DrawActionCounters("By Action", cycle.actionCounters);
        EditorGUILayout.EndVertical();
    }

    private void DrawActionCounters(string title, BoomerangSequencePerformance.ActionHitCounters counters)
    {
        if (counters == null)
            return;

        if (!string.IsNullOrEmpty(title))
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(true))
        {
            DrawActionRow("Outbound", counters.outboundHitEvents, counters.outboundUniqueEnemies);
            DrawActionRow("Returning", counters.returningHitEvents, counters.returningUniqueEnemies);
            DrawActionRow("Reflect Hold", counters.reflectHoldHitEvents, counters.reflectHoldUniqueEnemies);
            DrawActionRow("Reflected Outbound", counters.reflectedOutboundHitEvents, counters.reflectedOutboundUniqueEnemies);
            DrawActionRow("Orbit Reward", counters.orbitRewardHitEvents, counters.orbitRewardUniqueEnemies);
        }
    }

    private void DrawActionRow(string label, int hitEvents, int uniqueEnemies)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(label);
        EditorGUILayout.IntField("Hits", hitEvents);
        EditorGUILayout.IntField("Unique", uniqueEnemies);
        EditorGUILayout.EndHorizontal();
    }
}
#endif