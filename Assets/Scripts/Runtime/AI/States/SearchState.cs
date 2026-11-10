using StealthRaider.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace StealthRaider.AI.States
{
    /// <summary>
    /// Goes to the last known position, looks around, then checks random points nearby until
    /// the search time runs out. The meter fills faster here, because the raider is alert.
    /// </summary>
    public class SearchState : RaiderState
    {
        enum Phase
        {
            Moving,
            LookingAround,
        }

        Phase phase;
        float elapsed;
        float lookTimer;
        float baseYaw;
        bool hadSight;

        public SearchState(RaiderBrain brain) : base(brain) { }

        public override RaiderStateId Id => RaiderStateId.Search;

        public override void Enter()
        {
            elapsed = 0f;
            hadSight = false;
            GoTo(brain.HasLastKnownPosition ? brain.LastKnownPosition : brain.transform.position);
        }

        public override void Tick(float dt)
        {
            if (brain.Detection.IsFull)
            {
                brain.ChangeState(RaiderStateId.Combat, "found the hero");
                return;
            }

            elapsed += dt;
            if (elapsed >= Config.searchDuration)
            {
                brain.ChangeState(RaiderStateId.Return, "found nothing");
                return;
            }

            // Hero in sight but not yet recognised: freeze and stare while the meter fills.
            if (brain.SeesTarget)
            {
                hadSight = true;
                brain.Stop();
                brain.FaceTowards(brain.Target.position, dt);
                return;
            }
            if (hadSight)
            {
                hadSight = false;
                GoTo(brain.LastKnownPosition);
            }

            if (phase == Phase.Moving)
            {
                if (!brain.ReachedDestination) return;
                phase = Phase.LookingAround;
                lookTimer = 0f;
                baseYaw = brain.transform.eulerAngles.y;
                brain.Stop();
                return;
            }

            lookTimer += dt;
            float sweep = Mathf.Sin(lookTimer / Config.lookAroundTime * Mathf.PI * 2f) * Config.lookAroundAngle;
            brain.FaceYaw(baseYaw + sweep, dt);
            if (lookTimer >= Config.lookAroundTime) GoTo(RandomPointNear(brain.LastKnownPosition));
        }

        public override void OnNoise(NoiseEvent noise)
        {
            // A new shot restarts the search at the new spot.
            brain.RememberPosition(noise.Position);
            elapsed = 0f;
            GoTo(noise.Position);
        }

        void GoTo(Vector3 point)
        {
            phase = Phase.Moving;
            brain.MoveTo(point, Config.searchSpeed);
        }

        Vector3 RandomPointNear(Vector3 center)
        {
            for (int i = 0; i < 6; i++)
            {
                Vector2 offset = Random.insideUnitCircle * Config.searchRadius;
                Vector3 candidate = center + new Vector3(offset.x, 0f, offset.y);
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                    return hit.position;
            }
            return center;
        }
    }
}
