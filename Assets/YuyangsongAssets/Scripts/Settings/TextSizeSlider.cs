using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

public class TextSizeSlider : XRBaseGrabTransformer
{
    [SerializeField] private Transform start;
    [SerializeField] private Transform end;
    [SerializeField] private TextAccessibility textAccessibility;

    private float position;

    public override void Process(XRGrabInteractable grabInteractable, XRInteractionUpdateOrder.UpdatePhase updatePhase,
        ref Pose targetPose, ref Vector3 localScale)
    {
        Vector3 origin = start.position;
        Vector3 track = end.position - origin;
        float next = Mathf.Clamp01(Vector3.Dot(targetPose.position - origin, track) / track.sqrMagnitude);
        targetPose.position = origin + next * track;

        if (!Mathf.Approximately(position, next))
        {
            position = next;
            textAccessibility.SetTextSize(position);
        }
    }
}
