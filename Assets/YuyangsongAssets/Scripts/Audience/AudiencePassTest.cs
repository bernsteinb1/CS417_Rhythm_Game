using UnityEngine;

public class AudiencePassTest : MonoBehaviour
{
    [SerializeField] private AudienceSpawner spawner;

    [ContextMenu("Test Pass")]
    public void TestPass()
    {
        if (!Application.isPlaying) return;

        foreach (AudienceExpression member in spawner.Audience)
        {
            member.ShowSmile();
            member.GetComponent<Animator>().SetTrigger("Clap");
        }
    }
}
