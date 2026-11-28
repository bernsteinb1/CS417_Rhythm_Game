using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ColorFilter : MonoBehaviour
{
    [Serializable]
    private struct Preset
    {
        public string name;
        public Texture2D lut;
    }

    [SerializeField] private Volume volume;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Preset[] presets;

    private ColorLookup lookup;
    private int currentPreset;

    private void Awake()
    {
        if (volume == null || label == null || presets == null || presets.Length == 0
            || !volume.profile.TryGet(out lookup))
        {
            Debug.LogWarning("Color Filter needs a Volume with Color Lookup, a label, and at least one preset.", this);
            return;
        }

        ApplyPreset();
    }

    public void CycleFilter()
    {
        if (lookup == null)
            return;

        currentPreset = (currentPreset + 1) % presets.Length;
        ApplyPreset();
    }

    private void ApplyPreset()
    {
        Preset preset = presets[currentPreset];
        lookup.texture.Override(preset.lut);
        lookup.contribution.Override(preset.lut == null ? 0f : 1f);
        label.text = $"Filter: {preset.name}";
    }
}
