using UnityEngine;

namespace ChatGo.Opponent
{
    /// <summary>
    /// 水平来回移动的 Opponent 方块。
    /// 被炮台子弹命中时通过 Hit() 扣全局 Opponent 血量。
    /// 玩家从下方跳跃撞击同样可造成伤害；横向无法穿过（实体碰撞体 + Kinematic Rigidbody2D）。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class OpponentBlock : MonoBehaviour
    {
        [Header("移动")]
        [SerializeField] private float moveSpeed = 2f;
        [Tooltip("以初始位置为中心，左右各移动的距离（米）")]
        [SerializeField] private float moveRange = 3f;

        [Header("生存时间")]
        [Tooltip("大于 0 时方块会在该秒数后销毁；0 或负数表示永久存在")]
        [SerializeField] private float activeTime = 10f;

        [Header("玩家跳跃攻击")]
        [SerializeField] private int playerHitDamage = 10;
        [Tooltip("同一次跳跃中不会重复计算伤害的冷却时间（秒）")]
        [SerializeField] private float hitCooldown = 0.5f;
        [Tooltip("接触法线 y 分量大于该阈值时，视为玩家从下方顶到方块。范围 0~1，0.5 是经验值。")]
        [SerializeField] private float bottomHitNormalThreshold = 0.5f;

        private Vector3 originPosition;
        private float direction = 1f;
        private float activeTimer;
        private float lastPlayerHitTime = float.NegativeInfinity;
        private Rigidbody2D rb;

        private void Awake()
        {
            originPosition = transform.position;
            activeTimer = activeTime;

            rb = GetComponent<Rigidbody2D>();
            // 让方块成为可移动的实体碰撞体：
            // - Kinematic：不受重力 / 力影响，由我们用 MovePosition 驱动；
            // - 非 trigger：玩家无法横穿。
            rb.bodyType = RigidbodyType2D.Kinematic;

            Collider2D col = GetComponent<Collider2D>();
            if (col.isTrigger)
            {
                col.isTrigger = false;
            }
        }

        private void OnEnable()
        {
            if (OpponentHealth.Instance != null)
            {
                OpponentHealth.Instance.Died += OnOpponentDied;
            }
        }

        private void OnDisable()
        {
            if (OpponentHealth.Instance != null)
            {
                OpponentHealth.Instance.Died -= OnOpponentDied;
            }
        }

        private void Update()
        {
            Move();
            TickActiveTimer();
        }

        /// <summary>被炮台子弹或玩家跳跃调用，扣全局 Opponent 血量。</summary>
        public void Hit(int damage)
        {
            OpponentHealth.Instance?.TakeDamage(damage);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryHandlePlayerHit(collision);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            // 玩家可能贴着方块持续向上撞，Stay 期间也尝试结算（受 hitCooldown 约束，不会刷伤害）。
            TryHandlePlayerHit(collision);
        }

        private void TryHandlePlayerHit(Collision2D collision)
        {
            if (!collision.collider.CompareTag("Player"))
            {
                return;
            }

            if (Time.time - lastPlayerHitTime < hitCooldown)
            {
                return;
            }

            // 接触法线指向「从对方表面指向自己」，玩家从下方顶上来时法线大致是 (0, 1)。
            int contactCount = collision.contactCount;
            for (int i = 0; i < contactCount; i++)
            {
                if (collision.GetContact(i).normal.y > bottomHitNormalThreshold)
                {
                    lastPlayerHitTime = Time.time;
                    Hit(playerHitDamage);
                    return;
                }
            }
        }

        private void Move()
        {
            float safeRange = Mathf.Max(0f, moveRange);
            float leftBound = originPosition.x - safeRange;
            float rightBound = originPosition.x + safeRange;

            Vector3 current = transform.position;
            Vector3 target = current + Vector3.right * (direction * moveSpeed * Time.deltaTime);

            if (target.x >= rightBound)
            {
                target.x = rightBound;
                direction = -1f;
            }
            else if (target.x <= leftBound)
            {
                target.x = leftBound;
                direction = 1f;
            }

            if (rb != null)
            {
                rb.MovePosition(target);
            }
            else
            {
                transform.position = target;
            }
        }

        private void TickActiveTimer()
        {
            if (activeTime <= 0f)
            {
                return;
            }

            activeTimer -= Time.deltaTime;
            if (activeTimer <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private void OnOpponentDied()
        {
            Destroy(gameObject);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 origin = Application.isPlaying ? originPosition : transform.position;
            float safeRange = Mathf.Max(0f, moveRange);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origin + Vector3.left * safeRange, origin + Vector3.right * safeRange);
            Gizmos.DrawWireCube(origin + Vector3.left * safeRange, Vector3.one * 0.18f);
            Gizmos.DrawWireCube(origin + Vector3.right * safeRange, Vector3.one * 0.18f);
        }
#endif
    }
}
