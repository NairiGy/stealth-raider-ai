using System;
using UnityEngine;

namespace StealthRaider.Combat
{
    public readonly struct NoiseEvent
    {
        public readonly Vector3 Position;
        public readonly float Radius;
        public readonly GameObject Source;

        public NoiseEvent(Vector3 position, float radius, GameObject source)
        {
            Position = position;
            Radius = radius;
            Source = source;
        }
    }

    /// <summary>Global channel for loud sounds. Gunshots go through here; listeners decide if they are in range.</summary>
    public static class NoiseSystem
    {
        public static event Action<NoiseEvent> NoiseMade;

        public static void Emit(Vector3 position, float radius, GameObject source)
        {
            NoiseMade?.Invoke(new NoiseEvent(position, radius, source));
        }

        // Clears listeners when domain reload is off in the Editor.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => NoiseMade = null;
    }
}
