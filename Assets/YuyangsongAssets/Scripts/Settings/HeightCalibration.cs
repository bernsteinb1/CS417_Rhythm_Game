using Unity.XR.CoreUtils;
using UnityEngine;

public class HeightCalibration : MonoBehaviour
{
    [SerializeField] private Transform targetEyeHeight;

    public void Calibrate()
    {
        Camera head = Camera.main;
        XROrigin rig = head != null ? head.GetComponentInParent<XROrigin>() : null;
        if (rig == null || rig.Origin == null || targetEyeHeight == null)
        {
            Debug.LogWarning("Height calibration needs an XR camera and a target eye height.", this);
            return;
        }

        float offset = targetEyeHeight.position.y - head.transform.position.y;
        rig.Origin.transform.position += Vector3.up * offset;
    }
}
