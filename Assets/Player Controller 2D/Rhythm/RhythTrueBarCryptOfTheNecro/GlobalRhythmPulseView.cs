using UnityEngine;
using UnityEngine.UI;

public class GlobalRhythmPulseView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Image fillImage;
    [SerializeField] private Image outlineImage;

    public RectTransform RectTransform
    {
        get
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            return rectTransform;
        }
    }

    private void Awake()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (fillImage == null)
            fillImage = GetComponentInChildren<Image>();

        if (outlineImage == null && transform.childCount > 1)
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            if (images.Length > 0)
                outlineImage = images[0];

            if (images.Length > 1)
                fillImage = images[1];
        }
    }

    public void SetColor(Color color)
    {
        if (fillImage != null)
            fillImage.color = color;
    }
    public void SetOutlineColor(Color color)
    {
        if (outlineImage != null)
            outlineImage.color = color;
    }

    public void SetAlpha(float alpha)
    {
        alpha = Mathf.Clamp01(alpha);

        if (fillImage != null)
        {
            Color c = fillImage.color;
            c.a = alpha;
            fillImage.color = c;
        }

        if (outlineImage != null)
        {
            Color c = outlineImage.color;
            c.a = alpha;
            outlineImage.color = c;
        }
    }

    public void SetVisible(bool visible)
    {
        if (gameObject.activeSelf != visible)
            gameObject.SetActive(visible);
    }

    public void SetScale(float scale)
    {
        RectTransform.localScale = Vector3.one * Mathf.Max(0.01f, scale);
    }

    public void SetAnchoredPosition(float x, float y = 0f)
    {
        Vector2 pos = RectTransform.anchoredPosition;
        pos.x = x;
        pos.y = y;
        RectTransform.anchoredPosition = pos;
    }
}