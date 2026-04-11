using UnityEngine;
using UnityEngine.InputSystem;

namespace ChatGo.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("移动")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float groundAcceleration = 50f;
        [SerializeField] private float groundDeceleration = 60f;
        [SerializeField] private float airAcceleration = 28f;
        [SerializeField] private float airDeceleration = 36f;
        [SerializeField] private float jumpForce = 9f;
        [SerializeField] private bool lockRotation = true;

        [Header("下落")]
        [SerializeField] private float fallGravityMultiplier = 2f;
        [SerializeField] private float maxFallSpeed = 18f;

        [Header("地面检测（体积）")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private Vector2 groundCheckBoxSize = new(0.45f, 0.16f);
        [SerializeField] private LayerMask groundLayerMask = ~0;

        [Header("输入")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Player";
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string jumpActionName = "Jump";

        private Rigidbody2D rb;
        private InputAction moveAction;
        private InputAction jumpAction;
        private float moveInputX;
        private float uiMoveInputX;
        private float baseGravityScale;

        public bool CanMove { get; set; } = true;
        public bool IsGrounded { get; private set; }

        /// <summary>供动画使用：锁定移动时视为无水平输入，避免 UI/键盘仍显示走路。</summary>
        public float AnimationInputX => CanMove ? moveInputX : 0f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            baseGravityScale = rb.gravityScale;
            ApplyPhysicsConstraints();
            ResolveInputActions();
        }

        private void OnEnable()
        {
            if (moveAction != null)
            {
                moveAction.Enable();
            }

            if (jumpAction != null)
            {
                jumpAction.Enable();
                jumpAction.performed += OnJumpPerformed;
            }
        }

        private void OnDisable()
        {
            if (jumpAction != null)
            {
                jumpAction.performed -= OnJumpPerformed;
                jumpAction.Disable();
            }

            if (moveAction != null)
            {
                moveAction.Disable();
            }
        }

        private void Update()
        {
            UpdateGroundedState();
            HandleKeyboardJumpFallback();

            if (!CanMove || moveAction == null)
            {
                moveInputX = Mathf.Abs(uiMoveInputX) > 0.01f ? uiMoveInputX : 0f;
                return;
            }

            Vector2 moveVector = moveAction.ReadValue<Vector2>();
            float inputActionX = Mathf.Clamp(moveVector.x, -1f, 1f);
            moveInputX = Mathf.Abs(uiMoveInputX) > 0.01f ? uiMoveInputX : inputActionX;
        }

        private void FixedUpdate()
        {
            Vector2 velocity = rb.linearVelocity;
            float targetSpeedX = CanMove ? moveInputX * moveSpeed : 0f;
            float acceleration = ResolveHorizontalAcceleration(targetSpeedX);
            velocity.x = Mathf.MoveTowards(velocity.x, targetSpeedX, acceleration * Time.fixedDeltaTime);
            velocity = ApplyVerticalTuning(velocity);
            rb.linearVelocity = velocity;
        }

        public void TeleportTo(Vector3 worldPosition)
        {
            transform.position = worldPosition;
            rb.linearVelocity = Vector2.zero;
        }

        public void ForceJump()
        {
            if (!CanMove || !IsGrounded)
            {
                return;
            }

            Vector2 velocity = rb.linearVelocity;
            velocity.y = 0f;
            rb.linearVelocity = velocity;
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }

        public void SetMoveInputFromUI(float horizontal)
        {
            uiMoveInputX = Mathf.Clamp(horizontal, -1f, 1f);
        }

        private void ResolveInputActions()
        {
            if (inputActions == null)
            {
                Debug.LogWarning("PlayerController: 没有设置 InputActionAsset。");
                return;
            }

            InputActionMap map = inputActions.FindActionMap(actionMapName, true);
            moveAction = map?.FindAction(moveActionName, true);
            jumpAction = map?.FindAction(jumpActionName, true);
        }

        private void OnJumpPerformed(InputAction.CallbackContext _)
        {
            ForceJump();
        }

        private void HandleKeyboardJumpFallback()
        {
            // 兜底支持：即使 InputAction 里没配 Jump，也可用 W 或上方向键跳跃。
            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
            {
                ForceJump();
            }
        }

        private void ApplyPhysicsConstraints()
        {
            if (rb == null || !lockRotation)
            {
                return;
            }

            rb.constraints |= RigidbodyConstraints2D.FreezeRotation;
        }

        private float ResolveHorizontalAcceleration(float targetSpeedX)
        {
            bool hasInput = Mathf.Abs(targetSpeedX) > 0.01f;
            if (IsGrounded)
            {
                return hasInput ? groundAcceleration : groundDeceleration;
            }

            return hasInput ? airAcceleration : airDeceleration;
        }

        private Vector2 ApplyVerticalTuning(Vector2 velocity)
        {
            if (rb == null)
            {
                return velocity;
            }

            float safeFallMultiplier = Mathf.Max(1f, fallGravityMultiplier);
            rb.gravityScale = baseGravityScale * (velocity.y < -0.01f ? safeFallMultiplier : 1f);

            float safeMaxFallSpeed = Mathf.Max(1f, maxFallSpeed);
            if (velocity.y < -safeMaxFallSpeed)
            {
                velocity.y = -safeMaxFallSpeed;
            }

            return velocity;
        }


        private void UpdateGroundedState()
        {
            if (groundCheck == null)
            {
                IsGrounded = false;
                return;
            }

            Vector2 checkSize = new(
                Mathf.Max(0.01f, groundCheckBoxSize.x),
                Mathf.Max(0.01f, groundCheckBoxSize.y));
            IsGrounded = Physics2D.OverlapBox(groundCheck.position, checkSize, 0f, groundLayerMask);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (groundCheck == null)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Vector3 boxSize = new(
                Mathf.Max(0.01f, groundCheckBoxSize.x),
                Mathf.Max(0.01f, groundCheckBoxSize.y),
                0f);
            Gizmos.DrawWireCube(groundCheck.position, boxSize);
        }
#endif
    }
}
