using UnityEngine;

public class NoteEffectTest : MonoBehaviour
{
    [SerializeField] private Transform hitLine;
    [SerializeField] private ParticleSystem hitEffect;
    [SerializeField] private ParticleSystem missEffect;

    [SerializeField, Min(0.01f)] private float speed = 0.15f;
    [SerializeField, Min(0f)] private float hitRange = 0.005f;

    private void Update()
    {
        if (Time.timeSinceLevelLoad < 10f) return;
        
        Vector3 position = transform.position;
        float effectZ = hitLine.position.z + hitRange;

        position.z = Mathf.MoveTowards(position.z, effectZ, speed * Time.deltaTime);
        transform.position = position;

        if (position.z == effectZ)
            PlayEffect();
    }

    private void PlayEffect()
    {
        ParticleSystem prefab = Random.value < 0.5f ? hitEffect : missEffect;
        ParticleSystem effect = Instantiate(prefab, transform.position, prefab.transform.rotation);
        
        effect.Play();
        gameObject.SetActive(false);
    }
}
