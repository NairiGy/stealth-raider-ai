using StealthRaider.Combat;
using UnityEngine;

namespace StealthRaider.AI.States
{
    /// <summary>
    /// Closes to shooting range and fires while the hero is in sight. Raiders never run away.
    /// Without sight it pushes to the last known position; after a while it turns into a search.
    /// </summary>
    public class CombatState : RaiderState
    {
        float lostTime;

        public CombatState(RaiderBrain brain) : base(brain) { }

        public override RaiderStateId Id => RaiderStateId.Combat;

        public override void Enter()
        {
            lostTime = 0f;
            brain.Detection.Fill();
        }

        public override void Tick(float dt)
        {
            if (brain.TargetHealth != null && brain.TargetHealth.IsDead)
            {
                brain.ChangeState(RaiderStateId.Return, "the hero is down");
                return;
            }

            if (brain.SeesTarget)
            {
                lostTime = 0f;
                brain.Detection.Fill();

                if (brain.TargetDistance > Config.preferredRange)
                    brain.MoveTo(brain.Target.position, Config.combatSpeed);
                else
                    brain.Stop();

                brain.FaceTowards(brain.Target.position, dt);
                if (brain.Gun.IsReady && brain.IsFacing(brain.Target.position, Config.aimTolerance))
                {
                    Vector3 origin = brain.Gun.Muzzle.position;
                    brain.Gun.TryFire(origin, brain.TargetAimPoint - origin);
                }
                return;
            }

            lostTime += dt;
            brain.MoveTo(brain.LastKnownPosition, Config.combatSpeed);
            if (lostTime >= Config.loseSightTime)
                brain.ChangeState(RaiderStateId.Search, "lost sight of the hero");
        }

        public override void OnNoise(NoiseEvent noise)
        {
            // Already fighting. A shot from the unseen hero still tells where they are now.
            if (!brain.SeesTarget && brain.Target != null && noise.Source == brain.Target.root.gameObject)
                brain.RememberPosition(noise.Position);
        }

        public override void OnDamaged(GameObject attacker)
        {
            if (!brain.SeesTarget && attacker != null) brain.RememberPosition(attacker.transform.position);
        }
    }
}
