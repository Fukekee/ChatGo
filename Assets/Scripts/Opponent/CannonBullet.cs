using UnityEngine;

namespace ChatGo.Opponent
{
    /// <summary>
    /// 炮台发出的子弹，向上飞行，命中 OpponentBlock 时扣血并销毁自身。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CannonBullet : MonoBehaviour
    {
        [SerializeField] private float speed = 10f;
        [SerializeField] private int damage = 10;
        [SerializeField] private float maxRange = 2f;

        private float traveled;
        private bool isActive;

        private void OnEnable()
        {
            traveled = 0f;
            isActive = true;

            Collider2D col = GetComponent<Collider2D>();
            if (col != null && !col.isTrigger)
            {
                col.isTrigger = true;
            }
        }

        /// <summary>由 CannonController 调用，设置子弹参数并激活。</summary>
        public void Launch(float bulletSpeed, int hitDamage, float shootRange)
        {
            speed = bulletSpeed;
            damage = hitDamage;
            maxRange = shootRange;
            traveled = 0f;
            isActive = true;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!isActive)
            {
                return;
            }

            float step = speed * Time.deltaTime;
            transform.position += Vector3.up * step;
            traveled += step;

            if (traveled >= Mathf.Max(0.1f, maxRange))
            {
                Deactivate();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isActive)
            {
                return;
            }

            // 用 SendMessage 避免命名空间编译顺序依赖
            if (other.CompareTag("OpponentBlock"))
            {
                other.SendMessage("Hit", damage, SendMessageOptions.DontRequireReceiver);
                Deactivate();
                return;
            }

            // 碰到除炮台本身和玩家以外的实体时停止
            if (!other.CompareTag("Player") && !other.CompareTag("Cannon"))
            {
                Deactivate();
            }
        }

        private void Deactivate()
        {
            isActive = false;
            gameObject.SetActive(false);
        }
    }
}
