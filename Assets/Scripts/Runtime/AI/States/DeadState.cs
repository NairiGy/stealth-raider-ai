using StealthRaider.Combat;
using UnityEngine;

namespace StealthRaider.AI.States
{
    public class DeadState : RaiderState
    {
        public DeadState(RaiderBrain brain) : base(brain) { }

        public override RaiderStateId Id => RaiderStateId.Dead;

        public override void Enter()
        {
            brain.Stop();
            brain.Agent.enabled = false;
            foreach (Collider c in brain.GetComponentsInChildren<Collider>()) c.enabled = false;
            if (brain.Visual != null) brain.Visual.SetDead();
        }

        public override void Tick(float dt) { }
        public override void OnNoise(NoiseEvent noise) { }
        public override void OnDamaged(GameObject attacker) { }
    }
}
