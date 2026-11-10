using System;

namespace StealthRaider.AI
{
    /// <summary>
    /// How sure a raider is that it sees the hero, from 0 (nothing) to 1 (spotted).
    /// Plain C# with no Unity calls, so it is covered by edit-mode tests.
    /// </summary>
    public class DetectionMeter
    {
        readonly float timeNear;
        readonly float timeFar;
        readonly float decayPerSecond;

        public float Value { get; private set; }
        public bool IsFull => Value >= 1f;

        /// <param name="timeNear">Seconds to fill when the hero is right in front.</param>
        /// <param name="timeFar">Seconds to fill at the edge of view distance.</param>
        /// <param name="decayPerSecond">How much drains per second while the hero is unseen.</param>
        public DetectionMeter(float timeNear, float timeFar, float decayPerSecond)
        {
            if (timeNear <= 0f) throw new ArgumentOutOfRangeException(nameof(timeNear));
            if (timeFar <= 0f) throw new ArgumentOutOfRangeException(nameof(timeFar));
            if (decayPerSecond < 0f) throw new ArgumentOutOfRangeException(nameof(decayPerSecond));
            this.timeNear = timeNear;
            this.timeFar = timeFar;
            this.decayPerSecond = decayPerSecond;
        }

        /// <summary>Seconds to fill an empty meter at this distance (0 = touching, 1 = edge of view).</summary>
        public float FillTimeAt(float distance01)
        {
            float t = Clamp01(distance01);
            return timeNear + (timeFar - timeNear) * t;
        }

        /// <param name="seen">Whether the hero is in sight this frame.</param>
        /// <param name="distance01">Distance to the hero divided by view distance.</param>
        /// <param name="deltaTime">Frame time in seconds.</param>
        /// <param name="fillMultiplier">Above 1 when the raider is already alert.</param>
        public void Tick(bool seen, float distance01, float deltaTime, float fillMultiplier = 1f)
        {
            if (seen)
                Value += deltaTime * fillMultiplier / FillTimeAt(distance01);
            else
                Value -= decayPerSecond * deltaTime;
            Value = Clamp01(Value);
        }

        public void Fill() => Value = 1f;
        public void Reset() => Value = 0f;

        static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
