using UnityEngine;

namespace StealthRaider.Combat
{
    /// <summary>
    /// Hitscan gun used by the hero and the raiders. Every shot is loud: it always goes to the
    /// noise system, because the game has no silencers.
    /// </summary>
    public class Gun : MonoBehaviour
    {
        [SerializeField] Transform muzzle;
        [SerializeField] float damage = 10f;
        [SerializeField] float fireInterval = 0.5f;
        [SerializeField] float spreadDegrees = 2f;
        [SerializeField] float range = 60f;
        [SerializeField] float noiseRadius = 40f;
        [SerializeField] LayerMask hitMask = ~0;
        [SerializeField] Material tracerMaterial;
        [SerializeField] Color tracerColor = new Color(0.92f, 0.63f, 0.22f);
        [SerializeField] float tracerTime = 0.06f;

        float nextShotTime;
        float tracerHideTime;
        LineRenderer tracer;
        Health ownerHealth;

        public Transform Muzzle => muzzle != null ? muzzle : transform;
        public bool IsReady => Time.time >= nextShotTime;
        public float NoiseRadius => noiseRadius;
        GameObject Owner => transform.root.gameObject;

        void Awake()
        {
            ownerHealth = Owner.GetComponent<Health>();

            var go = new GameObject("Tracer");
            go.transform.SetParent(transform, false);
            tracer = go.AddComponent<LineRenderer>();
            tracer.useWorldSpace = true;
            tracer.positionCount = 2;
            tracer.widthMultiplier = 0.04f;
            tracer.sharedMaterial = tracerMaterial;
            tracer.startColor = tracerColor;
            tracer.endColor = new Color(tracerColor.r, tracerColor.g, tracerColor.b, 0.2f);
            tracer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tracer.enabled = false;
        }

        void Update()
        {
            if (tracer.enabled && Time.time >= tracerHideTime) tracer.enabled = false;
        }

        /// <param name="aimOrigin">Where the aim ray starts: the camera for the hero, the muzzle for raiders.</param>
        /// <param name="aimDirection">Direction of the aim ray.</param>
        public bool TryFire(Vector3 aimOrigin, Vector3 aimDirection)
        {
            if (!IsReady || aimDirection.sqrMagnitude < 0.0001f) return false;
            nextShotTime = Time.time + fireInterval;

            Vector3 direction = ApplySpread(aimDirection.normalized);
            Vector3 end = aimOrigin + direction * range;
            if (RaycastIgnoringOwner(aimOrigin, direction, out RaycastHit hit))
            {
                end = hit.point;
                var health = hit.collider.GetComponentInParent<Health>();
                bool friendly = health != null && ownerHealth != null && health.Team == ownerHealth.Team;
                if (health != null && !friendly) health.Damage(damage, Owner);
            }

            ShowTracer(Muzzle.position, end);
            NoiseSystem.Emit(Muzzle.position, noiseRadius, Owner);
            return true;
        }

        bool RaycastIgnoringOwner(Vector3 origin, Vector3 direction, out RaycastHit closest)
        {
            closest = default;
            bool found = false;
            float best = float.MaxValue;
            Transform owner = transform.root;
            foreach (RaycastHit hit in Physics.RaycastAll(origin, direction, range, hitMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(owner)) continue;
                if (hit.distance >= best) continue;
                best = hit.distance;
                closest = hit;
                found = true;
            }
            return found;
        }

        Vector3 ApplySpread(Vector3 direction)
        {
            if (spreadDegrees <= 0f) return direction;
            Vector3 side = Vector3.Cross(direction, Vector3.up);
            if (side.sqrMagnitude < 0.001f) side = Vector3.right;
            Quaternion tilt = Quaternion.AngleAxis(Random.Range(0f, spreadDegrees), side.normalized);
            Quaternion roll = Quaternion.AngleAxis(Random.Range(0f, 360f), direction);
            return roll * tilt * direction;
        }

        void ShowTracer(Vector3 from, Vector3 to)
        {
            tracer.SetPosition(0, from);
            tracer.SetPosition(1, to);
            tracer.enabled = true;
            tracerHideTime = Time.time + tracerTime;
        }
    }
}
