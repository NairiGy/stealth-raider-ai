using StealthRaider.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StealthRaider.Player
{
    /// <summary>WASD movement relative to the camera; the hero always faces where the camera looks.</summary>
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public class HeroController : MonoBehaviour
    {
        [SerializeField] ThirdPersonCamera cameraRig;
        [SerializeField] Transform body;
        [SerializeField] float walkSpeed = 3.2f;
        [SerializeField] float runSpeed = 5.5f;
        [SerializeField] float gravity = -20f;
        [SerializeField] float turnSpeed = 720f;

        CharacterController controller;
        Health health;
        float verticalSpeed;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            health.Died += OnDied;
        }

        void OnDestroy() => health.Died -= OnDied;

        void Update()
        {
            if (health.IsDead) return;
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            Vector2 input = Vector2.zero;
            if (keyboard.wKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;
            input = Vector2.ClampMagnitude(input, 1f);

            float yaw = cameraRig != null ? cameraRig.Yaw : transform.eulerAngles.y;
            Vector3 move = Quaternion.Euler(0f, yaw, 0f) * new Vector3(input.x, 0f, input.y);
            float speed = keyboard.leftShiftKey.isPressed ? runSpeed : walkSpeed;

            verticalSpeed = controller.isGrounded ? -2f : verticalSpeed + gravity * Time.deltaTime;
            controller.Move((move * speed + Vector3.up * verticalSpeed) * Time.deltaTime);

            Quaternion facing = Quaternion.Euler(0f, yaw, 0f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, facing, turnSpeed * Time.deltaTime);
        }

        void OnDied()
        {
            controller.enabled = false;
            if (body == null) return;
            body.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            body.localPosition = new Vector3(0f, 0.3f, 0f);
        }
    }
}
