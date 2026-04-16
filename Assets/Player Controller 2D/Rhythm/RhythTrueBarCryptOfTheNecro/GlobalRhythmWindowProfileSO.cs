using UnityEngine;

[CreateAssetMenu(
    fileName = "GlobalRhythmWindowProfile",
    menuName = "Game/Rhythm/Global Rhythm Window Profile")]
public class GlobalRhythmWindowProfileSO : ScriptableObject
{
    [Header("Timing Rule")]
    [SerializeField] private TimedSequenceActionRule rule = new();

    [Header("Pulse")]
    [SerializeField] private int visiblePulsePairs = 3;

    [Min(1)]
    [SerializeField] private int subdivision = 4;

    [Header("Travel")]
    [SerializeField] private float travelBeats = 2f;

    [Header("Look")]
    [SerializeField] private bool showSubdivisions = false;

    public TimedSequenceActionRule Rule => rule;
    public int VisiblePulsePairs => Mathf.Max(1, visiblePulsePairs);
    public int Subdivision => Mathf.Max(1, subdivision);
    public float TravelBeats => Mathf.Max(0.25f, travelBeats);
    public bool ShowSubdivisions => showSubdivisions;

    public bool IsValid()
    {
        return rule != null && rule.Enabled;
    }
}