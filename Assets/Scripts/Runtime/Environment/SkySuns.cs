using UnityEngine;

namespace StealthRaider.Environment
{
    /// <summary>
    /// Feeds the two-sun skybox with each light's direction and colour, so the suns in the sky
    /// always match the light and shadows. Runs in the editor too, so the sky follows a light
    /// while it is being rotated.
    /// </summary>
    [ExecuteAlways]
    public class SkySuns : MonoBehaviour
    {
        static readonly int SunDir1 = Shader.PropertyToID("_DA_SunDir1");
        static readonly int SunDir2 = Shader.PropertyToID("_DA_SunDir2");
        static readonly int SunColor1 = Shader.PropertyToID("_DA_SunColor1");
        static readonly int SunColor2 = Shader.PropertyToID("_DA_SunColor2");

        [SerializeField] Light primary;
        [SerializeField] Light secondary;

        void OnEnable() => Push();
        void Update() => Push();

        void Push()
        {
            Push(primary, SunDir1, SunColor1);
            Push(secondary, SunDir2, SunColor2);
        }

        static void Push(Light sun, int directionId, int colorId)
        {
            if (sun == null) return;
            // A directional light shines along its forward axis, so the sun is behind it.
            Shader.SetGlobalVector(directionId, -sun.transform.forward);
            Shader.SetGlobalVector(colorId, sun.color.linear);
        }
    }
}
