namespace StealthRaider.AI.States
{
    /// <summary>
    /// The raider saw something. It stops and stares: while the hero stays in sight the meter keeps
    /// filling until combat; if the hero slips away, the raider goes to check the spot.
    /// </summary>
    public class SuspiciousState : RaiderState
    {
        float unseenTime;

        public SuspiciousState(RaiderBrain brain) : base(brain) { }

        public override RaiderStateId Id => RaiderStateId.Suspicious;

        public override void Enter()
        {
            unseenTime = 0f;
            brain.Stop();
        }

        public override void Tick(float dt)
        {
            if (brain.Detection.IsFull)
            {
                brain.ChangeState(RaiderStateId.Combat, "made out the hero");
                return;
            }

            if (brain.SeesTarget)
            {
                unseenTime = 0f;
                brain.FaceTowards(brain.Target.position, dt);
                return;
            }

            unseenTime += dt;
            if (brain.HasLastKnownPosition) brain.FaceTowards(brain.LastKnownPosition, dt);
            if (unseenTime >= Config.suspicionTimeout)
                brain.ChangeState(RaiderStateId.Search, "lost it, going to check");
        }
    }
}
