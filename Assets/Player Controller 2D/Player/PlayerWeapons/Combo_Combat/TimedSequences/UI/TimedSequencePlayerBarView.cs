using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private Image barBackground;
    [SerializeField] private Image decisionLine;
    [SerializeField] private Image catchPulse;

    [Header("Background Colors")]
    [SerializeField] private Color activeBackgroundColor = new Color(0f, 0f, 0f, 0.55f);
    [SerializeField] private Color neutralBackgroundColor = new Color(0.18f, 0.18f, 0.18f, 0.7f);
    [SerializeField] private Image[] tintedZoneImages;


    private Color currentBarTint = Color.white;

    [Header("Marker Colors")]
    [SerializeField] private Image markerImage;
    [SerializeField] private Color defaultMarkerColor = Color.white;
    [SerializeField] private Color releaseMarkerColor = new Color(0.55f, 1f, 0.55f, 1f);
    [SerializeField] private Color reflectMarkerColor = new Color(1f, 0.92f, 0.35f, 1f);

    public void SetDefinition(WeaponSequenceDefinitionSO definition)
    {
        if (definition == null || barArea == null)
            return;

        SetNeutralMode(false);
        ApplyRule(definition.ShootRule);
    }

    public void SetRule(TimedSequenceActionRule rule)
    {
        if (barArea == null)
            return;

        if (rule == null)
        {
            ClearZones();
            return;
        }

        ApplyRule(rule);
    }

    public void SetNeutralMode(bool neutral)
    {
        if (barBackground == null)
            return;

        barBackground.color = neutral ? neutralBackgroundColor : currentBarTint;
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

    private void ClearZones()
    {
        SetZoneNormalized(failLeftZone, 0f, 0f);
        SetZoneNormalized(goodLeftZone, 0f, 0f);
        SetZoneNormalized(perfectZone, 0f, 0f);
        SetZoneNormalized(goodRightZone, 0f, 0f);
        SetZoneNormalized(failRightZone, 0f, 0f);
    }

    private void SetZoneNormalized(RectTransform zone, float min, float max)
    {
        if (zone == null || barArea == null)
            return;

        float width = barArea.rect.width;
        float xMin = Mathf.Lerp(-width * 0.5f, width * 0.5f, min);
        float xMax = Mathf.Lerp(-width * 0.5f, width * 0.5f, max);
        float zoneWidth = Mathf.Max(0f, xMax - xMin);

        zone.anchorMin = new Vector2(0.5f, zone.anchorMin.y);
        zone.anchorMax = new Vector2(0.5f, zone.anchorMax.y);
        zone.pivot = new Vector2(0f, 0.5f);
        zone.anchoredPosition = new Vector2(xMin, zone.anchoredPosition.y);
        zone.sizeDelta = new Vector2(zoneWidth, zone.sizeDelta.y);
    }

    public void SetMarkerColor(Color color)
    {
        if (markerImage != null)
            markerImage.color = color;
    }

    public void SetBarTint(Color color)
    {
        currentBarTint = color;

        if (barBackground != null)
            barBackground.color = color;
    }

    public void ClearBarTint()
    {
        currentBarTint = activeBackgroundColor;

        if (barBackground != null)
            barBackground.color = activeBackgroundColor;
    }

    public void SetDecisionLineVisible(bool visible)
    {
        if (decisionLine != null)
            decisionLine.enabled = visible;
    }

    public void SetDecisionLineColor(Color color)
    {
        if (decisionLine != null)
            decisionLine.color = color;
    }

    public void FlashCatchPulse(Color color)
    {
        if (catchPulse != null)
        {
            catchPulse.enabled = true;
            catchPulse.color = color;
        }
    }

    public void SetCatchPulseVisible(bool visible)
    {
        if (catchPulse != null)
            catchPulse.enabled = visible;
    }
}