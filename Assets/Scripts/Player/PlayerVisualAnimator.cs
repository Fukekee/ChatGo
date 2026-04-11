using UnityEngine;

namespace ChatGo.Player
{
    /// <summary>
    /// 驱动侧身走路 / 正面待机 / 侧身待机，并与 PlayerController 输入同步。
    /// Animator 参数：Speed(float)、MoveX(float)、FacingRight(bool)、HasMoved(bool)。
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerVisualAnimator : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int MoveXId = Animator.StringToHash("MoveX");
        private static readonly int FacingRightId = Animator.StringToHash("FacingRight");
        private static readonly int HasMovedId = Animator.StringToHash("HasMoved");

        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private float moveDeadZone = 0.01f;

        private PlayerController player;
        private bool facingRight = true;
        private bool hasMoved;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void LateUpdate()
        {
            if (animator == null || spriteRenderer == null)
            {
                return;
            }

            float x = player.AnimationInputX;
            float speed = Mathf.Abs(x);
            if (speed > moveDeadZone)
            {
                facingRight = x > 0f;
                hasMoved = true;
            }

            animator.SetFloat(SpeedId, speed);
            animator.SetFloat(MoveXId, x);
            animator.SetBool(FacingRightId, facingRight);
            animator.SetBool(HasMovedId, hasMoved);

            if (speed > moveDeadZone)
            {
                spriteRenderer.flipX = x < -moveDeadZone;
            }
            else if (hasMoved)
            {
                spriteRenderer.flipX = !facingRight;
            }
            else
            {
                spriteRenderer.flipX = false;
            }
        }
    }
}
