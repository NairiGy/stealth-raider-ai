using StealthRaider.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StealthRaider.Player
{
    /// <summary>Left click fires along the camera's centre line, where the crosshair is.</summary>
    [RequireComponent(typeof(Health))]
    public class HeroShooter : MonoBehaviour
    {
        [SerializeField] Gun gun;
        [SerializeField] Camera aimCamera;

        Health health;

        void Awake() => health = GetComponent<Health>();

        void Update()
        {
            if (health.IsDead || gun == null || aimCamera == null) return;
            if (!ThirdPersonCamera.CursorLocked) return;
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

            Transform view = aimCamera.transform;
            gun.TryFire(view.position, view.forward);
        }
    }
}
