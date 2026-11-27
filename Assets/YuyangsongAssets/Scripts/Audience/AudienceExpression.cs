using UnityEngine;

public class AudienceExpression : MonoBehaviour
{
    [SerializeField] private GameObject neutral, smile, frown;

    [ContextMenu("Show Neutral")]
    public void ShowNeutral()
    {
        Show(neutral);
    }

    [ContextMenu("Show Smile")]
    public void ShowSmile()
    {
        Show(smile);
    }

    [ContextMenu("Show Frown")]
    public void ShowFrown()
    {
        Show(frown);
    }

    private void Show(GameObject mouth)
    {
        neutral.SetActive(mouth == neutral);
        smile.SetActive(mouth == smile);
        frown.SetActive(mouth == frown);
    }
}
