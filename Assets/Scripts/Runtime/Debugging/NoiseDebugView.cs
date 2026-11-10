using System.Collections.Generic;
using StealthRaider.Combat;
using UnityEngine;

namespace StealthRaider.Debugging
{
    /// <summary>Shows each gunshot's hearing radius as a fading ring on the ground.</summary>
    public class NoiseDebugView : MonoBehaviour
    {
        const int Segments = 64;
        const float Lifetime = 1.2f;

        [SerializeField] Material lineMaterial;

        readonly List<(LineRenderer line, float bornAt)> rings = new List<(LineRenderer, float)>();
        readonly Stack<LineRenderer> pool = new Stack<LineRenderer>();

        void OnEnable() => NoiseSystem.NoiseMade += OnNoise;
        void OnDisable() => NoiseSystem.NoiseMade -= OnNoise;

        void OnNoise(NoiseEvent noise)
        {
            if (!DebugOverlay.Visible) return;
            LineRenderer line = pool.Count > 0 ? pool.Pop() : CreateRing();
            line.gameObject.SetActive(true);
            Vector3 centre = new Vector3(noise.Position.x, 0.1f, noise.Position.z);
            for (int i = 0; i < Segments; i++)
            {
                float a = i / (float)Segments * Mathf.PI * 2f;
                line.SetPosition(i, centre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * noise.Radius);
            }
            rings.Add((line, Time.time));
        }

        void Update()
        {
            for (int i = rings.Count - 1; i >= 0; i--)
            {
                (LineRenderer line, float bornAt) = rings[i];
                float t = (Time.time - bornAt) / Lifetime;
                if (t >= 1f || !DebugOverlay.Visible)
                {
                    line.gameObject.SetActive(false);
                    pool.Push(line);
                    rings.RemoveAt(i);
                    continue;
                }
                Color c = DebugOverlay.Amber;
                c.a = 0.8f * (1f - t);
                line.startColor = c;
                line.endColor = c;
            }
        }

        LineRenderer CreateRing()
        {
            var go = new GameObject("Noise Ring");
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = lineMaterial;
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = Segments;
            line.widthMultiplier = 0.12f;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }
    }
}
