using StealthRaider.Combat;
using UnityEngine;

namespace StealthRaider.AI.States
{
    /// <summary>Drops the gun, raises hands and keeps facing the hero. Ignores noise; can still be shot.</summary>
    public class SurrenderState : RaiderState
    {
        public SurrenderState(RaiderBrain brain) : base(brain) { }

        public override RaiderStateId Id => RaiderStateId.Surrender;

        public override void Enter()
        {
            brain.Stop();
            if (brain.Visual != null) brain.Visual.SetSurrendered();
        }

        public override void Tick(float dt)
        {
            if (brain.Target != null) brain.FaceTowards(brain.Target.position, dt);
        }

        public override void OnNoise(NoiseEvent noise) { }
        public override void OnDamaged(GameObject attacker) { }
    }
}
