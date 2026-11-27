using UnityEngine;

public class AudienceEyeTracking : MonoBehaviour
{
    [SerializeField] private Transform playerHead;
    [SerializeField] private Transform leftEye;
    [SerializeField] private Transform rightEye;
    [SerializeField, Min(0f)] private float sensitivity = 0.6f;
    [SerializeField, Range(0f, 0.05f)] private float maxOffset = 0.035f;

    private Vector3 leftRest, rightRest, eyeCenter;
    private Vector2 startingDirection, offset;

    private void Start()
    {
        if (playerHead == null && Camera.main != null)
            playerHead = Camera.main.transform;

        if (playerHead == null || leftEye == null || rightEye == null)
        {
            enabled = false;
            return;
        }

        leftRest = leftEye.localPosition;
        rightRest = rightEye.localPosition;
        eyeCenter = (leftRest + rightRest) * 0.5f;
        startingDirection = LookDirection();
    }

    private Vector2 LookDirection()
    {
        Vector3 direction = (playerHead.position - leftEye.parent.TransformPoint(eyeCenter)).normalized;
        return leftEye.parent.InverseTransformDirection(direction);
    }

    private void LateUpdate()
    {
        Vector2 target = Vector2.ClampMagnitude((LookDirection() - startingDirection) * sensitivity, maxOffset);
        offset = Vector2.MoveTowards(offset, target, 0.22f * Time.deltaTime);
        MoveEye(leftEye, leftRest);
        MoveEye(rightEye, rightRest);
    }

    private void MoveEye(Transform eye, Vector3 rest)
    {
        // Follow the capsule's curved front as the eye slides sideways.
        const float radiusX = 0.455f;
        const float radiusZ = 0.35f;
        float x = rest.x + offset.x;
        float z = radiusZ * Mathf.Sqrt(1f - x * x / (radiusX * radiusX));
        float restZ = radiusZ * Mathf.Sqrt(1f - rest.x * rest.x / (radiusX * radiusX));
        eye.localPosition = new Vector3(x, rest.y + offset.y, rest.z + z - restZ);
    }
}
