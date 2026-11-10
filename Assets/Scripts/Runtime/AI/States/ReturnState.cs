using UnityEngine;

namespace StealthRaider.AI.States
{
    /// <summary>Walks back to the nearest patrol point, then patrols again. Still watches on the way.</summary>
    public class ReturnState : RaiderState
    {
        public ReturnState(RaiderBrain brain) : base(brain) { }

        public override RaiderStateId Id => RaiderStateId.Return;

        public override void Enter()
        {
            if (brain.Waypoints.Count == 0)
            {
                brain.Stop();
                return;
            }
            brain.WaypointIndex = NearestPoint();
            brain.MoveTo(brain.Waypoints[brain.WaypointIndex].position, Config.patrolSpeed);
        }

        public override void Tick(float dt)
        {
            if (ReactToSight()) return;
            if (brain.Waypoints.Count == 0 || brain.ReachedDestination)
                brain.ChangeState(RaiderStateId.Patrol, "back on the route");
        }

        int NearestPoint()
        {
            int best = 0;
            float bestDistance = float.MaxValue;
            Vector3 position = brain.transform.position;
            for (int i = 0; i < brain.Waypoints.Count; i++)
            {
                float d = (brain.Waypoints[i].position - position).sqrMagnitude;
                if (d >= bestDistance) continue;
                bestDistance = d;
                best = i;
            }
            return best;
        }
    }
}
