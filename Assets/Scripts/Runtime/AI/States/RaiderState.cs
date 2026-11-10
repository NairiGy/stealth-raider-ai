using StealthRaider.Combat;
using UnityEngine;

namespace StealthRaider.AI.States
{
    /// <summary>
    /// One state of the raider. A state moves the raider each frame and decides when to switch;
    /// all switches go through <see cref="RaiderBrain.ChangeState"/> with a reason for the debug view.
    /// </summary>
    public abstract class RaiderState
    {
        protected readonly RaiderBrain brain;

        protected RaiderState(RaiderBrain brain) => this.brain = brain;

        protected RaiderConfig Config => brain.Config;

        public abstract RaiderStateId Id { get; }

        public virtual void Enter() { }
        public virtual void Exit() { }
        public abstract void Tick(float dt);

        /// <summary>By default a gunshot sends the raider to search where it came from.</summary>
        public virtual void OnNoise(NoiseEvent noise)
        {
            brain.RememberPosition(noise.Position);
            brain.ChangeState(RaiderStateId.Search, "heard a gunshot");
        }

        /// <summary>By default being shot means a fight: the raider knows roughly where the shooter is.</summary>
        public virtual void OnDamaged(GameObject attacker)
        {
            if (attacker != null) brain.RememberPosition(attacker.transform.position);
            brain.Detection.Fill();
            brain.ChangeState(RaiderStateId.Combat, "took a hit");
        }

        /// <summary>Shared reaction of calm states to what the raider sees. True if the state changed.</summary>
        protected bool ReactToSight()
        {
            if (brain.Detection.IsFull)
            {
                brain.ChangeState(RaiderStateId.Combat, "spotted the hero");
                return true;
            }
            if (brain.SeesTarget && brain.Detection.Value >= Config.suspicionThreshold)
            {
                brain.ChangeState(RaiderStateId.Suspicious, "glimpsed something");
                return true;
            }
            return false;
        }
    }
}
