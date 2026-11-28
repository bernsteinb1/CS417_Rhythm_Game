using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TextAccessibility : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset simpleFont;
    [SerializeField] private Font stylizedSource;
    [SerializeField] private TMP_Text fontLabel;
    [SerializeField] private TMP_Text sizeLabel;
    [SerializeField] private float minScale = 1f;
    [SerializeField] private float maxScale = 1.5f;

    private readonly Dictionary<TMP_Text, (float size, float min, float max)> originalSizes = new();
    private TMP_FontAsset stylizedFont;
    private bool useStylizedFont;
    private float scale;

    private void Start()
    {
        scale = minScale;
        RefreshText();
    }

    public void ToggleFont()
    {
        if (!useStylizedFont && stylizedFont == null)
        {
            stylizedFont = TMP_FontAsset.CreateFontAsset(stylizedSource);
            if (stylizedFont == null)
                return;
        }

        useStylizedFont = !useStylizedFont;
        fontLabel.text = useStylizedFont ? "Font: Stylized" : "Font: Simple";
        RefreshText();
    }

    public void RefreshText()
    {
        foreach (TMP_Text text in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))
        {
            if (text.gameObject.scene == gameObject.scene)
                originalSizes.TryAdd(text, (text.fontSize, text.fontSizeMin, text.fontSizeMax));
        }

        ApplyText();
    }

    public void SetTextSize(float position)
    {
        scale = Mathf.Lerp(minScale, maxScale, position);
        ApplyText();
    }

    private void ApplyText()
    {
        sizeLabel.text = $"Text Size: {Mathf.RoundToInt(scale * 100f)}%";
        TMP_FontAsset font = useStylizedFont ? stylizedFont : simpleFont;

        foreach (var (text, original) in originalSizes)
        {
            if (text == null)
                continue;

            text.font = font;
            if (text.enableAutoSizing)
            {
                text.fontSizeMin = original.min * scale;
                text.fontSizeMax = original.max * scale;
            }
            else
                text.fontSize = original.size * scale;
        }
    }

    private void OnDestroy()
    {
        if (stylizedFont != null)
            Destroy(stylizedFont);
    }
}
