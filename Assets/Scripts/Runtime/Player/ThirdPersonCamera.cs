using UnityEngine;
using UnityEngine.InputSystem;

namespace StealthRaider.Player
{
    /// <summary>Over-the-shoulder orbit camera. Mouse turns it; it pulls in when a wall is behind the hero.</summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector3 pivotOffset = new Vector3(0f, 1.7f, 0f);
        [SerializeField] Vector3 cameraOffset = new Vector3(0.9f, 0.35f, -4.4f);
        [SerializeField] float sensitivity = 0.12f;
        [SerializeField] float minPitch = -30f;
        [SerializeField] float maxPitch = 60f;
        [SerializeField] LayerMask collisionMask = ~0;

        float yaw;
        float pitch = 10f;

        public float Yaw => yaw;
        public static bool CursorLocked => Cursor.lockState == CursorLockMode.Locked;

        void Start()
        {
            if (target != null) yaw = target.eulerAngles.y;
            SetCursorLocked(true);
        }

        void Update()
        {
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) SetCursorLocked(false);
            if (mouse == null) return;
            if (!CursorLocked && mouse.leftButton.wasPressedThisFrame) SetCursorLocked(true);
            if (!CursorLocked) return;

            Vector2 delta = mouse.delta.ReadValue();
            yaw += delta.x * sensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * sensitivity, minPitch, maxPitch);
        }

        void LateUpdate()
        {
            if (target == null) return;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pivot = target.position + pivotOffset;
            Vector3 wanted = pivot + rotation * cameraOffset;

            Vector3 toCamera = wanted - pivot;
            float distance = toCamera.magnitude;
            if (Physics.SphereCast(pivot, 0.2f, toCamera / distance, out RaycastHit hit, distance, collisionMask,
                    QueryTriggerInteraction.Ignore))
                wanted = pivot + toCamera / distance * Mathf.Max(0.2f, hit.distance - 0.05f);

            transform.SetPositionAndRotation(wanted, rotation);
        }

        static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
