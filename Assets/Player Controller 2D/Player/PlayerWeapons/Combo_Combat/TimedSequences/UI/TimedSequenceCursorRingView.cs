using UnityEngine;
using UnityEngine.UI;

public class TimedSequenceCursorRingView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform ringRoot;

    [SerializeField] private Image failBackgroundRing;
    [SerializeField] private Image goodLeftArc;
    [SerializeField] private Image perfectArc;
    [SerializeField] private Image goodRightArc;

    [SerializeField] private RectTransform markerPivot;

    [Header("Layout")]
    [SerializeField] private bool useShootRuleForRing = true;

    [Tooltip("Normalized 0 starts at bottom.")]
    [SerializeField] private float startAngle = 0f;

    [Tooltip("Use 360 for full circle.")]
    [SerializeField] private float totalSweepDegrees = 360f;

    public void SetDefinition(WeaponSequenceDefinitionSO definition)
    {
        if (definition == null)
            return;

        TimedSequenceActionRule rule = useShootRuleForRing
            ? definition.ShootRule
            : definition.DashRule;

        TimedSequenceVisualSegments.SegmentSet segments = TimedSequenceVisualSegments.Build(rule);

        SetFullRing(failBackgroundRing);
        SetArc(goodLeftArc, segments.GoodLeft);
        SetArc(perfectArc, segments.Perfect);
        SetArc(goodRightArc, segments.GoodRight);
    }

    public void SetMarker(float normalizedTime)
    {
        if (markerPivot == null)
            return;

        float angle = NormalizedToAngle(Mathf.Clamp01(normalizedTime));
        markerPivot.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void SetFullRing(Image ringImage)
    {
        if (ringImage == null)
            return;

        ringImage.type = Image.Type.Filled;
        ringImage.fillMethod = Image.FillMethod.Radial360;
        ringImage.fillOrigin = (int)Image.Origin360.Bottom;
        ringImage.fillClockwise = false;
        ringImage.fillAmount = 1f;

        RectTransform rt = ringImage.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.localRotation = Quaternion.identity;
    }

    private void SetArc(Image arcImage, TimedSequenceVisualSegments.SegmentRange range)
    {
        if (arcImage == null)
            return;

        float start = NormalizedToAngle(range.Min);
        float sweep = range.Length * totalSweepDegrees;

        arcImage.type = Image.Type.Filled;
        arcImage.fillMethod = Image.FillMethod.Radial360;
        arcImage.fillOrigin = (int)Image.Origin360.Bottom;
        arcImage.fillClockwise = false;
        arcImage.fillAmount = Mathf.Clamp01(Mathf.Abs(sweep) / 360f);

        RectTransform rt = arcImage.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.localRotation = Quaternion.Euler(0f, 0f, start);
    }

    private float NormalizedToAngle(float normalized)
    {
        return startAngle + (normalized * totalSweepDegrees);
    }
}