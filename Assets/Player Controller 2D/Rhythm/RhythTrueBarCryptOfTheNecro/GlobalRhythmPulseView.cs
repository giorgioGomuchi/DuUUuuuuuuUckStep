using UnityEngine;
using UnityEngine.UI;

public class GlobalRhythmPulseView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Image image;

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

        if (image == null)
            image = GetComponent<Image>();
    }

    public void SetColor(Color color)
    {
        if (image != null)
            image.color = color;
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