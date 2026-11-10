using UnityEngine;

namespace StealthRaider.AI
{
    /// <summary>
    /// Vision cone with a line-of-sight check. Several points on the target's body are tested,
    /// so a hero whose head shows over low cover can still be seen.
    /// </summary>
    public class VisionSensor
    {
        static readonly float[] TargetHeights = { 0.4f, 1.0f, 1.6f };

        readonly Transform self;
        readonly RaiderConfig config;

        public VisionSensor(Transform self, RaiderConfig config)
        {
            this.self = self;
            this.config = config;
        }

        public Vector3 Eye => self.position + Vector3.up * config.eyeHeight;

        public bool IsInCone(Vector3 point)
        {
            Vector3 flat = point - self.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f) return true;
            return Vector3.Angle(self.forward, flat) <= config.viewAngle * 0.5f;
        }

        /// <summary>True if any tested point of the target is in the cone, in range and not blocked.</summary>
        public bool CanSee(Transform target, out float distance)
        {
            distance = float.MaxValue;
            if (target == null) return false;

            Vector3 eye = Eye;
            bool seen = false;
            foreach (float height in TargetHeights)
            {
                Vector3 point = target.position + Vector3.up * height;
                Vector3 toPoint = point - eye;
                float d = toPoint.magnitude;
                if (d > config.viewDistance || d < 0.001f) continue;
                if (!IsInCone(point)) continue;
                if (Physics.Raycast(eye, toPoint / d, d, config.obstacleMask, QueryTriggerInteraction.Ignore)) continue;

                seen = true;
                if (d < distance) distance = d;
            }
            return seen;
        }
    }
}
