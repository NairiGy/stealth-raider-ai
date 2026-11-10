using System;
using System.Collections.Generic;
using UnityEngine;

namespace StealthRaider.AI
{
    /// <summary>Living raiders in the scene. Used to tell when one is the last one standing.</summary>
    public static class RaiderRegistry
    {
        static readonly List<RaiderBrain> alive = new List<RaiderBrain>();

        public static IReadOnlyList<RaiderBrain> Alive => alive;
        public static event Action Changed;

        public static void Register(RaiderBrain raider)
        {
            if (alive.Contains(raider)) return;
            alive.Add(raider);
            Changed?.Invoke();
        }

        public static void Unregister(RaiderBrain raider)
        {
            if (!alive.Remove(raider)) return;
            Changed?.Invoke();
        }

        // Clears state when domain reload is off in the Editor.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            alive.Clear();
            Changed = null;
        }
    }
}
