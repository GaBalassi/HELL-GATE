using UnityEngine;
using UnityEngine.InputSystem;

namespace Hellgate
{
    [RequireComponent(typeof(PlayerMotor), typeof(CharacterController))]
    public sealed class FirstPersonPlayer : MonoBehaviour
    {
        [SerializeField] InputActionAsset inputActions;
        [SerializeField] Transform view;
        [SerializeField, Range(0.01f, 1f)] float mouseSensitivity = 0.1f;
        [SerializeField, Range(30, 89)] float pitchLimit = 85;
        PlayerMotor motor;
        CharacterController controller;
        InputActionAsset ownedActions;
        InputAction move, look, jump, sprint;
        Vector3 spawnPosition;
        Quaternion spawnRotation;
        float pitch;
        bool focused = true;

        void Awake()
        {
            motor = GetComponent<PlayerMotor>();
            controller = GetComponent<CharacterController>();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            if (inputActions == null || view == null)
            {
                Debug.LogError("FirstPersonPlayer requires Input Actions and a camera transform.", this);
                enabled = false;
                return;
            }
            // Own the enabled actions; do not change the shared project input asset.
            ownedActions = Instantiate(inputActions);
            ownedActions.bindingMask = InputBinding.MaskByGroup("Keyboard&Mouse");
            var map = ownedActions.FindActionMap("Player", true);
            move = map.FindAction("Move", true);
            look = map.FindAction("Look", true);
            jump = map.FindAction("Jump", true);
            sprint = map.FindAction("Sprint", true);
        }

        void OnEnable()
        {
            if (ownedActions == null) return;
            move.Enable(); look.Enable(); jump.Enable(); sprint.Enable();
            motor.ResetMotion();
            SetCursorCaptured(true);
        }

        void OnDisable()
        {
            if (ownedActions != null) ownedActions.Disable();
            SetCursorCaptured(false);
        }

        void OnDestroy()
        {
            if (ownedActions != null) Destroy(ownedActions);
        }

        void OnApplicationFocus(bool hasFocus)
        {
            focused = hasFocus;
            if (!hasFocus) SetCursorCaptured(false);
        }

        void Update()
        {
            if (!focused || Time.timeScale == 0) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetCursorCaptured(false);
                return;
            }
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                // Keep gravity active while the cursor is free, but suppress player input.
                motor.Move(Vector2.zero, false, false, Time.deltaTime);
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                    SetCursorCaptured(true);
            }
            else
            {
                var delta = look.ReadValue<Vector2>() * mouseSensitivity;
                // Mouse delta is already accumulated per frame; do not multiply by deltaTime.
                transform.Rotate(0, delta.x, 0);
                pitch = Mathf.Clamp(pitch - delta.y, -pitchLimit, pitchLimit);
                view.localRotation = Quaternion.Euler(pitch, 0, 0);
                motor.Move(move.ReadValue<Vector2>(), sprint.IsPressed(), jump.WasPressedThisFrame(), Time.deltaTime);
            }
            if (transform.position.y < -15) Respawn();
        }

        void Respawn()
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            controller.enabled = true;
            pitch = 0;
            view.localRotation = Quaternion.identity;
            motor.ResetMotion();
        }

        static void SetCursorCaptured(bool captured)
        {
            Cursor.lockState = captured ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !captured;
        }
    }
}
