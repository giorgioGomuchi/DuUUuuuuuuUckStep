using UnityEngine;

public static class TimedSequenceVisualSegments
{
    public struct SegmentRange
    {
        public float Min;
        public float Max;

        public SegmentRange(float min, float max)
        {
            Min = Mathf.Clamp01(min);
            Max = Mathf.Clamp01(max);
        }

        public float Length => Mathf.Max(0f, Max - Min);
    }

    public struct SegmentSet
    {
        public SegmentRange FailLeft;
        public SegmentRange GoodLeft;
        public SegmentRange Perfect;
        public SegmentRange GoodRight;
        public SegmentRange FailRight;
    }

    public static SegmentSet Build(TimedSequenceActionRule rule)
    {
        SegmentSet set = new SegmentSet();

        if (rule == null)
            return set;

        float goodMin = 0.5f - rule.GoodHalfWindowNormalized;
        float goodMax = 0.5f + rule.GoodHalfWindowNormalized;

        float perfectMin = 0.5f - rule.PerfectHalfWindowNormalized;
        float perfectMax = 0.5f + rule.PerfectHalfWindowNormalized;

        set.FailLeft = new SegmentRange(0f, goodMin);
        set.GoodLeft = new SegmentRange(goodMin, perfectMin);
        set.Perfect = new SegmentRange(perfectMin, perfectMax);
        set.GoodRight = new SegmentRange(perfectMax, goodMax);
        set.FailRight = new SegmentRange(goodMax, 1f);

        return set;
    }
}