using UnityEngine;

namespace Hellgate
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] float moveSpeed = 7f;
        [SerializeField, Min(0.1f)] float sprintSpeed = 10f;
        [SerializeField, Min(0.1f)] float groundAcceleration = 45f;
        [SerializeField, Min(0.1f)] float airAcceleration = 12f;
        [SerializeField, Min(0.1f)] float jumpHeight = 1.2f;
        [SerializeField, Min(0.1f)] float gravity = 25f;
        CharacterController controller;
        Vector3 horizontalVelocity;
        float verticalVelocity;

        public void ResetMotion()
        {
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0;
        }

        // Input-independent so the same collision behaviour can be checked in the Editor.
        public void Move(Vector2 input, bool sprint, bool jump, float deltaTime)
        {
            if (deltaTime <= 0) return;
            if (controller == null) controller = GetComponent<CharacterController>();
            bool grounded = controller.isGrounded;
            var direction = transform.right * input.x + transform.forward * input.y;
            var target = Vector3.ClampMagnitude(direction, 1) * (sprint ? sprintSpeed : moveSpeed);
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, target,
                (grounded ? groundAcceleration : airAcceleration) * deltaTime);

            if (grounded && verticalVelocity < 0) verticalVelocity = -4f;
            if (grounded && jump) verticalVelocity = Mathf.Sqrt(2 * gravity * jumpHeight);
            verticalVelocity -= gravity * deltaTime;
            var collisions = controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * deltaTime);
            if ((collisions & CollisionFlags.Above) != 0 && verticalVelocity > 0) verticalVelocity = 0;
            if ((collisions & CollisionFlags.Below) != 0 && verticalVelocity < 0) verticalVelocity = -4f;
        }
    }
}
