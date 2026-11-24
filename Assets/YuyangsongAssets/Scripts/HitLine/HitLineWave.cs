using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class HitLineWave : MonoBehaviour
{
    [SerializeField, Min(0f)] private float amplitude = 0.014f;
    [SerializeField, Min(0f)] private float waveCount = 2f;
    [SerializeField] private float speed = 0.5f;

    private const int Segments = 40;
    private LineRenderer line;
    private Vector3 leftEnd;
    private Vector3 rightEnd;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        leftEnd = line.GetPosition(0);
        rightEnd = line.GetPosition(line.positionCount - 1);

        line.positionCount = Segments + 1;
        line.SetPosition(0, leftEnd);
        line.SetPosition(Segments, rightEnd);
    }

    private void Update()
    {
        for (int i = 1; i < Segments; i++)
        {
            float t = (float)i / Segments;
            Vector3 point = Vector3.Lerp(leftEnd, rightEnd, t);

            float phase = 2f * Mathf.PI * (waveCount * t - speed * Time.time);
            float wave = Mathf.Sin(phase);
            float fade = Mathf.Sin(Mathf.PI * t);

            point.y += amplitude * wave * fade;
            line.SetPosition(i, point);
        }
    }
}