using UnityEngine;

public class TimedSequencePlayerBarView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform barArea;
    [SerializeField] private RectTransform failLeftZone;
    [SerializeField] private RectTransform goodLeftZone;
    [SerializeField] private RectTransform perfectZone;
    [SerializeField] private RectTransform goodRightZone;
    [SerializeField] private RectTransform failRightZone;
    [SerializeField] private RectTransform marker;

    public void SetDefinition(WeaponSequenceDefinitionSO definition)
    {
        if (definition == null || barArea == null)
            return;

        ApplyRule(definition.ShootRule);
    }

    public void SetRule(TimedSequenceActionRule rule)
    {
        if (rule == null || barArea == null)
            return;

        ApplyRule(rule);
    }

    public void SetMarker(float normalizedTime)
    {
        if (barArea == null || marker == null)
            return;

        float width = barArea.rect.width;
        float x = Mathf.Lerp(-width * 0.5f, width * 0.5f, Mathf.Clamp01(normalizedTime));

        marker.anchorMin = new Vector2(0.5f, marker.anchorMin.y);
        marker.anchorMax = new Vector2(0.5f, marker.anchorMax.y);
        marker.pivot = new Vector2(0.5f, 0.5f);

        Vector2 pos = marker.anchoredPosition;
        pos.x = x;
        marker.anchoredPosition = pos;
    }

    private void ApplyRule(TimedSequenceActionRule rule)
    {
        TimedSequenceVisualSegments.SegmentSet segments = TimedSequenceVisualSegments.Build(rule);
        float width = barArea.rect.width;

        SetZone(failLeftZone, width, segments.FailLeft);
        SetZone(goodLeftZone, width, segments.GoodLeft);
        SetZone(perfectZone, width, segments.Perfect);
        SetZone(goodRightZone, width, segments.GoodRight);
        SetZone(failRightZone, width, segments.FailRight);
    }

    private void SetZone(RectTransform zone, float totalWidth, TimedSequenceVisualSegments.SegmentRange range)
    {
        if (zone == null)
            return;

        float xMin = Mathf.Lerp(-totalWidth * 0.5f, totalWidth * 0.5f, range.Min);
        float xMax = Mathf.Lerp(-totalWidth * 0.5f, totalWidth * 0.5f, range.Max);
        float zoneWidth = Mathf.Max(0f, xMax - xMin);

        zone.anchorMin = new Vector2(0.5f, zone.anchorMin.y);
        zone.anchorMax = new Vector2(0.5f, zone.anchorMax.y);
        zone.pivot = new Vector2(0f, 0.5f);
        zone.anchoredPosition = new Vector2(xMin, zone.anchoredPosition.y);
        zone.sizeDelta = new Vector2(zoneWidth, zone.sizeDelta.y);
    }
}