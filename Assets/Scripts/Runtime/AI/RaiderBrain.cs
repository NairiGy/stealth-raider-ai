using System;
using System.Collections.Generic;
using StealthRaider.AI.States;
using StealthRaider.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace StealthRaider.AI
{
    public enum RaiderStateId
    {
        Patrol,
        Suspicious,
        Search,
        Combat,
        Return,
        Surrender,
        Dead,
    }

    /// <summary>
    /// Owns the raider's senses and its state machine. Each frame it updates what the raider
    /// sees, then lets the current state decide what to do and when to switch.
    /// Hearing and taking damage arrive as events and are passed to the current state.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(Health))]
    public class RaiderBrain : MonoBehaviour
    {
        [SerializeField] RaiderConfig config;
        [SerializeField] Transform target;
        [SerializeField] Transform[] waypoints = Array.Empty<Transform>();
        [SerializeField] Gun gun;
        [SerializeField] RaiderVisual visual;

        readonly Dictionary<RaiderStateId, RaiderState> states = new Dictionary<RaiderStateId, RaiderState>();
        RaiderState current;

        public RaiderConfig Config => config;
        public Transform Target => target;
        public Health TargetHealth { get; private set; }
        public NavMeshAgent Agent { get; private set; }
        public Health Health { get; private set; }
        public Gun Gun => gun;
        public RaiderVisual Visual => visual;
        public VisionSensor Vision { get; private set; }
        public DetectionMeter Detection { get; private set; }
        public IReadOnlyList<Transform> Waypoints => waypoints;
        public int WaypointIndex { get; set; }

        public bool SeesTarget { get; private set; }
        public float TargetDistance { get; private set; }
        public bool HasLastKnownPosition { get; private set; }
        public Vector3 LastKnownPosition { get; private set; }

        public RaiderStateId State => current != null ? current.Id : RaiderStateId.Patrol;
        /// <summary>Last switch with its reason, e.g. "Patrol → Suspicious: glimpsed something".</summary>
        public string LastTransition { get; private set; } = "";
        public event Action<RaiderStateId, RaiderStateId, string> StateChanged;

        public Vector3 TargetAimPoint => target.position + Vector3.up * 1.2f;

        void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            Health = GetComponent<Health>();
            Vision = new VisionSensor(transform, config);
            Detection = new DetectionMeter(config.detectTimeNear, config.detectTimeFar, config.detectionDecay);
            if (target != null) TargetHealth = target.GetComponent<Health>();

            Add(new PatrolState(this));
            Add(new SuspiciousState(this));
            Add(new SearchState(this));
            Add(new CombatState(this));
            Add(new ReturnState(this));
            Add(new SurrenderState(this));
            Add(new DeadState(this));
        }

        void Add(RaiderState state) => states.Add(state.Id, state);

        void OnEnable()
        {
            NoiseSystem.NoiseMade += OnNoise;
            Health.Damaged += OnDamaged;
            Health.Died += OnDied;
            RaiderRegistry.Changed += OnRegistryChanged;
        }

        void OnDisable()
        {
            NoiseSystem.NoiseMade -= OnNoise;
            Health.Damaged -= OnDamaged;
            Health.Died -= OnDied;
            RaiderRegistry.Changed -= OnRegistryChanged;
            RaiderRegistry.Unregister(this);
        }

        void Start()
        {
            // Registered in Start, not OnEnable: Health sets its value in its own Awake.
            RaiderRegistry.Register(this);
            ChangeState(RaiderStateId.Patrol, "start");
        }

        void Update()
        {
            if (current == null || State == RaiderStateId.Dead) return;
            float dt = Time.deltaTime;
            Perceive(dt);
            current.Tick(dt);
        }

        void Perceive(float dt)
        {
            bool targetAlive = target != null && (TargetHealth == null || !TargetHealth.IsDead);
            float distance = 0f;
            SeesTarget = targetAlive && Vision.CanSee(target, out distance);
            TargetDistance = SeesTarget ? distance
                : target != null ? Vector3.Distance(transform.position, target.position) : float.MaxValue;

            if (State == RaiderStateId.Surrender) return;

            float fillMultiplier = State == RaiderStateId.Search ? config.alertFillMultiplier : 1f;
            Detection.Tick(SeesTarget, TargetDistance / config.viewDistance, dt, fillMultiplier);

            // A glimpse below the suspicion threshold is not enough to remember where the hero was.
            if (SeesTarget && Detection.Value >= config.suspicionThreshold)
                RememberPosition(target.position);
        }

        public void ChangeState(RaiderStateId id, string reason)
        {
            if (current != null && current.Id == id) return;

            RaiderStateId from = State;
            bool first = current == null;
            current?.Exit();
            current = states[id];
            LastTransition = first ? id.ToString() : $"{from} → {id}: {reason}";
            Debug.Log($"[{name}] {LastTransition}", this);
            current.Enter();
            StateChanged?.Invoke(from, id, reason);
        }

        public void RememberPosition(Vector3 position)
        {
            HasLastKnownPosition = true;
            LastKnownPosition = position;
        }

        void OnNoise(NoiseEvent noise)
        {
            if (current == null || noise.Source == gameObject) return;
            if ((noise.Position - transform.position).sqrMagnitude > noise.Radius * noise.Radius) return;
            current.OnNoise(noise);
        }

        void OnDamaged(float amount, GameObject attacker)
        {
            if (Health.IsDead) return;
            if (TrySurrender()) return;
            current?.OnDamaged(attacker);
        }

        void OnDied()
        {
            ChangeState(RaiderStateId.Dead, "killed");
            RaiderRegistry.Unregister(this);
        }

        void OnRegistryChanged() => TrySurrender();

        /// <summary>The last raider standing gives up once badly hurt.</summary>
        bool TrySurrender()
        {
            if (!config.canSurrender || Health.IsDead) return false;
            if (State == RaiderStateId.Surrender || State == RaiderStateId.Dead) return false;
            if (RaiderRegistry.Alive.Count != 1 || Health.Fraction > config.surrenderHealthFraction) return false;
            ChangeState(RaiderStateId.Surrender, "last one standing and badly hurt");
            return true;
        }

        // ---- Movement helpers used by the states ----

        public void MoveTo(Vector3 destination, float speed)
        {
            if (!Agent.enabled || !Agent.isOnNavMesh) return;
            Agent.isStopped = false;
            Agent.speed = speed;
            Agent.updateRotation = true;
            Agent.SetDestination(destination);
        }

        public void Stop()
        {
            if (!Agent.enabled || !Agent.isOnNavMesh) return;
            Agent.isStopped = true;
            Agent.ResetPath();
        }

        public bool ReachedDestination =>
            Agent.enabled && !Agent.pathPending && Agent.remainingDistance <= Agent.stoppingDistance + 0.2f;

        public void FaceTowards(Vector3 point, float dt)
        {
            Vector3 direction = point - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;
            FaceYaw(Quaternion.LookRotation(direction).eulerAngles.y, dt);
        }

        public void FaceYaw(float yaw, float dt)
        {
            Agent.updateRotation = false;
            Quaternion wanted = Quaternion.Euler(0f, yaw, 0f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, wanted, config.turnSpeed * dt);
        }

        public bool IsFacing(Vector3 point, float toleranceDegrees)
        {
            Vector3 direction = point - transform.position;
            direction.y = 0f;
            return Vector3.Angle(transform.forward, direction) <= toleranceDegrees;
        }

        void OnDrawGizmosSelected()
        {
            if (waypoints == null) return;
            Gizmos.color = Color.cyan;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null) continue;
                Gizmos.DrawWireSphere(waypoints[i].position, 0.3f);
                Transform next = waypoints[(i + 1) % waypoints.Length];
                if (next != null) Gizmos.DrawLine(waypoints[i].position, next.position);
            }
        }
    }
}
