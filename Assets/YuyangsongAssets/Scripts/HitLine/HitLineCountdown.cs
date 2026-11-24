using System.Collections;
using UnityEngine;

public class HitLineCountdown : MonoBehaviour
{
    [System.Serializable]
    private class Look
    {
        public Gradient core, glow, arc1, arc2;
        public Material smoke;
    }

    [SerializeField] private LineRenderer core, glow, arc1, arc2;
    [SerializeField] private ParticleSystemRenderer smoke;
    [SerializeField] private Look three, two, one, go;

    public void SetCountdownStep(int step)
    {
        Look look;
        switch (step)
        {
            case 3: look = three; break;
            case 2: look = two;   break;
            case 1: look = one;   break;
            case 0: look = go;    break;
            default: return;
        }

        core.colorGradient = look.core;
        glow.colorGradient = look.glow;
        arc1.colorGradient = look.arc1;
        arc2.colorGradient = look.arc2;
        smoke.sharedMaterial = look.smoke;
    }

    [ContextMenu("Test Countdown")]
    public void TestCountdown()
    {
        if (!Application.isPlaying) return;
        StopAllCoroutines();
        StartCoroutine(Preview());
    }

    private IEnumerator Preview()
    {
        // yield return new WaitForSeconds(1f);
        for (int step = 3; step >= 0; step--)
        {
            SetCountdownStep(step);
            yield return new WaitForSeconds(1f);
        }
    }
}