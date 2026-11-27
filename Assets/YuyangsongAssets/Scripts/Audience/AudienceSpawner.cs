using System.Collections.Generic;
using UnityEngine;

public class AudienceSpawner : MonoBehaviour
{
    [SerializeField] private AudienceExpression[] prefabs;
    [SerializeField] private Transform[] leftPositions, rightPositions;

    private readonly List<AudienceExpression> audience = new List<AudienceExpression>();
    public IReadOnlyList<AudienceExpression> Audience => audience;

    private void Awake()
    {
        var remaining = new List<AudienceExpression>(prefabs);
        SpawnSide(leftPositions, remaining);
        SpawnSide(rightPositions, remaining);
    }

    private void SpawnSide(Transform[] positions, List<AudienceExpression> remaining)
    {
        int emptySeat = Random.Range(0, positions.Length);
        for (int i = 0; i < positions.Length; i++)
        {
            if (i == emptySeat) continue;

            int index = Random.Range(0, remaining.Count);
            Transform seat = positions[i];
            audience.Add(Instantiate(remaining[index], seat.position, seat.rotation, transform));
            remaining.RemoveAt(index);
        }
    }
}
