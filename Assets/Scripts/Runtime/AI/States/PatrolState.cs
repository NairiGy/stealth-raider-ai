namespace StealthRaider.AI.States
{
    /// <summary>Walks the patrol points in a loop and pauses at each one.</summary>
    public class PatrolState : RaiderState
    {
        float pause;

        public PatrolState(RaiderBrain brain) : base(brain) { }

        public override RaiderStateId Id => RaiderStateId.Patrol;

        public override void Enter()
        {
            pause = 0f;
            GoToCurrentPoint();
        }

        public override void Tick(float dt)
        {
            if (ReactToSight()) return;

            int count = brain.Waypoints.Count;
            if (count == 0)
            {
                brain.Stop();
                return;
            }

            if (!brain.ReachedDestination) return;

            pause += dt;
            if (pause < Config.waypointPause) return;

            pause = 0f;
            brain.WaypointIndex = (brain.WaypointIndex + 1) % count;
            GoToCurrentPoint();
        }

        void GoToCurrentPoint()
        {
            if (brain.Waypoints.Count == 0) return;
            brain.MoveTo(brain.Waypoints[brain.WaypointIndex].position, Config.patrolSpeed);
        }
    }
}
