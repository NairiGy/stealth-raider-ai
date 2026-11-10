using UnityEngine;

namespace StealthRaider.AI
{
    /// <summary>All raider tuning in one asset, so designers can tweak it without touching code.</summary>
    [CreateAssetMenu(menuName = "Stealth Raider/Raider Config", fileName = "RaiderConfig")]
    public class RaiderConfig : ScriptableObject
    {
        [Header("Movement")]
        public float patrolSpeed = 1.8f;
        public float searchSpeed = 2.8f;
        public float combatSpeed = 3.6f;
        [Tooltip("Degrees per second when the raider turns to look at something.")]
        public float turnSpeed = 240f;
        [Tooltip("Seconds the raider stands at each patrol point.")]
        public float waypointPause = 1.5f;

        [Header("Vision")]
        public float viewDistance = 15f;
        [Range(10f, 180f)] public float viewAngle = 110f;
        public float eyeHeight = 1.6f;
        [Tooltip("What blocks sight. Characters must not be in this mask.")]
        public LayerMask obstacleMask = ~0;

        [Header("Detection")]
        [Tooltip("Seconds to fill the meter when the hero is right in front of the raider.")]
        public float detectTimeNear = 0.5f;
        [Tooltip("Seconds to fill the meter at the edge of view distance.")]
        public float detectTimeFar = 4f;
        [Tooltip("How much of the meter drains per second while the hero is out of sight.")]
        public float detectionDecay = 0.2f;
        [Tooltip("Meter value at which a patrolling raider becomes suspicious.")]
        [Range(0f, 1f)] public float suspicionThreshold = 0.3f;
        [Tooltip("Fill speed multiplier while the raider is already searching.")]
        public float alertFillMultiplier = 1.75f;

        [Header("Suspicion")]
        [Tooltip("Seconds without seeing the hero before a suspicious raider goes to check.")]
        public float suspicionTimeout = 2.5f;

        [Header("Search")]
        public float searchDuration = 12f;
        [Tooltip("Radius around the last known position in which the raider checks extra points.")]
        public float searchRadius = 6f;
        [Tooltip("Seconds spent looking around at each search point.")]
        public float lookAroundTime = 2f;
        public float lookAroundAngle = 70f;

        [Header("Combat")]
        [Tooltip("The raider closes in until the hero is this close, then holds position and shoots.")]
        public float preferredRange = 9f;
        [Tooltip("Seconds without seeing the hero before combat turns into a search.")]
        public float loseSightTime = 3f;
        [Tooltip("The raider only shoots when facing the hero within this many degrees.")]
        public float aimTolerance = 12f;

        [Header("Surrender (optional)")]
        public bool canSurrender = true;
        [Tooltip("The last raider standing surrenders at or below this share of health.")]
        [Range(0f, 1f)] public float surrenderHealthFraction = 0.35f;
    }
}
