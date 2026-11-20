using UnityEngine;
using UnityEngine.UI;

public class ProgressBarTiling : MonoBehaviour
{
    public Material sourceMaterial;
    public float fullTilingX = 10f;

    public Slider slider;
    public float duration = 120f;

    public Image fillImage;
    private Material runtimeMaterial;

    public void SetDuration(float seconds)
    {
        duration = seconds;
    }

    public void SetTime(float seconds)
    {
        slider.value = duration > 0f
            ? Mathf.Clamp01(seconds / duration)
            : 0f;
    }

    public void UpdateTiling(float progress)
    {
        Vector4 tiling = runtimeMaterial.GetVector("_Tiling");
        tiling.x = progress * fullTilingX;
        runtimeMaterial.SetVector("_Tiling", tiling);
    }

    private void Awake()
    {
        runtimeMaterial = new Material(sourceMaterial);
        runtimeMaterial.name = sourceMaterial.name + " (Runtime)";
        fillImage.material = runtimeMaterial;
        UpdateTiling(slider.normalizedValue);
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
    }
}
