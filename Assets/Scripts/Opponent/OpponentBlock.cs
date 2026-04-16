using UnityEngine;

namespace ChatGo.Opponent
{
    /// <summary>
    /// 水平来回移动的 Opponent 方块。
    /// 被炮台子弹命中时通过 Hit() 扣全局 Opponent 血量。
    /// 玩家从下方跳跃撞击同样可造成伤害。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
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

        private Vector3 originPosition;
        private float direction = 1f;
        private float activeTimer;
        private float lastPlayerHitTime = float.NegativeInfinity;

        private void Awake()
        {
            originPosition = transform.position;
            activeTimer = activeTime;

            Collider2D col = GetComponent<Collider2D>();
            if (!col.isTrigger)
            {
                col.isTrigger = true;
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

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            if (Time.time - lastPlayerHitTime < hitCooldown)
            {
                return;
            }

            Rigidbody2D playerRb = other.attachedRigidbody;
            if (playerRb == null)
            {
                return;
            }

            bool isBelow = other.transform.position.y < transform.position.y;
            bool isMovingUp = playerRb.linearVelocity.y > 0.1f;

            if (isBelow && isMovingUp)
            {
                lastPlayerHitTime = Time.time;
                Hit(playerHitDamage);
            }
        }

        private void Move()
        {
            float safeRange = Mathf.Max(0f, moveRange);
            float leftBound = originPosition.x - safeRange;
            float rightBound = originPosition.x + safeRange;

            transform.position += Vector3.right * (direction * moveSpeed * Time.deltaTime);

            if (transform.position.x >= rightBound)
            {
                transform.position = new Vector3(rightBound, transform.position.y, transform.position.z);
                direction = -1f;
            }
            else if (transform.position.x <= leftBound)
            {
                transform.position = new Vector3(leftBound, transform.position.y, transform.position.z);
                direction = 1f;
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
