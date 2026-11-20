using UnityEngine;

namespace BrianRhythm
{
    public struct LaneHit
    {
        public int lane;
        public Vector3 worldPoint;
        public float offsetFromCenter;
        public float speed;
        public float entryAngle;
        public double time;
        public double dspTime;
        public Transform striker;
    }
}
