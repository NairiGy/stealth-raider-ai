using StealthRaider.AI;
using UnityEngine;

namespace StealthRaider.Debugging
{
    /// <summary>Shared switch and colours for the in-game debug drawing (F1 toggles it).</summary>
    public static class DebugOverlay
    {
        public static bool Visible = true;

        // Interface palette from the game's brief.
        public static readonly Color White = Hex(0xE5E0D4);
        public static readonly Color Muted = Hex(0x8C8578);
        public static readonly Color Amber = Hex(0xEBA138);
        public static readonly Color Red = Hex(0xD63D2B);
        public static readonly Color Panel = Hex(0x22201D);
        public static readonly Color Background = Hex(0x171413);
        public static readonly Color Orange = Hex(0xD9772B);

        public static Color StateColor(RaiderStateId state)
        {
            switch (state)
            {
                case RaiderStateId.Patrol: return White;
                case RaiderStateId.Suspicious: return Amber;
                case RaiderStateId.Search: return Orange;
                case RaiderStateId.Combat: return Red;
                case RaiderStateId.Return: return Muted;
                case RaiderStateId.Surrender: return White;
                default: return Muted;
            }
        }

        static Color Hex(int rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Visible = true;
    }
}
