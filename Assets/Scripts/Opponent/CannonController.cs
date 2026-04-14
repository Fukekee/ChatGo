using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChatGo.Opponent
{
    /// <summary>
    /// 可被玩家推动的炮台。
    /// 玩家进入检测范围后持续向上发射炮弹。
    /// 物理推动依靠 Rigidbody2D Dynamic + BoxCollider2D 实体自动实现，无需额外代码。
    /// </summary>
    public class CannonController : MonoBehaviour
    {
        [Header("子弹")]
        [SerializeField] private CannonBullet bulletPrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private int bulletDamage = 10;
        [SerializeField] private float bulletSpeed = 10f;
        [SerializeField] private float shootRange = 2f;

        [Header("连发")]
        [SerializeField] private float fireInterval = 0.4f;

        [Header("对象池")]
        [SerializeField] private int prewarmCount = 6;
        [SerializeField] private Transform poolRoot;

        private readonly Queue<CannonBullet> pool = new();
        private readonly List<Collider2D> playersInRange = new();
        private Coroutine fireRoutine;

        private void Awake()
        {
            if (poolRoot == null)
            {
                poolRoot = transform;
            }

            if (bulletPrefab != null)
            {
                Prewarm();
            }

            if (firePoint == null)
            {
                firePoint = transform;
            }

            gameObject.tag = "Cannon";
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            if (!playersInRange.Contains(other))
            {
                playersInRange.Add(other);
            }

            if (fireRoutine == null)
            {
                fireRoutine = StartCoroutine(FireLoop());
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            playersInRange.Remove(other);

            if (playersInRange.Count == 0 && fireRoutine != null)
            {
                StopCoroutine(fireRoutine);
                fireRoutine = null;
            }
        }

        private IEnumerator FireLoop()
        {
            while (true)
            {
                Fire();
                yield return new WaitForSeconds(Mathf.Max(0.05f, fireInterval));
            }
        }

        private void Fire()
        {
            if (bulletPrefab == null)
            {
                return;
            }

            CannonBullet bullet = GetFromPool();
            if (bullet == null)
            {
                return;
            }

            bullet.transform.SetParent(null, true);
            bullet.transform.position = firePoint != null ? firePoint.position : transform.position;
            bullet.transform.rotation = Quaternion.identity;
            bullet.Launch(bulletSpeed, bulletDamage, shootRange);
        }

        private CannonBullet GetFromPool()
        {
            while (pool.Count > 0)
            {
                CannonBullet candidate = pool.Dequeue();
                if (candidate != null)
                {
                    return candidate;
                }
            }

            return CreateInstance();
        }

        private void ReturnToPool(CannonBullet bullet)
        {
            if (bullet == null)
            {
                return;
            }

            bullet.gameObject.SetActive(false);
            bullet.transform.SetParent(poolRoot, true);
            pool.Enqueue(bullet);
        }

        private void Prewarm()
        {
            int count = Mathf.Max(0, prewarmCount);
            for (int i = 0; i < count; i++)
            {
                CannonBullet instance = CreateInstance();
                if (instance != null)
                {
                    ReturnToPool(instance);
                }
            }
        }

        private CannonBullet CreateInstance()
        {
            if (bulletPrefab == null)
            {
                return null;
            }

            CannonBullet instance = Instantiate(bulletPrefab, poolRoot);
            instance.gameObject.SetActive(false);
            return instance;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.5f);
            Vector3 fp = firePoint != null ? firePoint.position : transform.position;
            Gizmos.DrawLine(fp, fp + Vector3.up * shootRange);
            Gizmos.DrawWireSphere(fp + Vector3.up * shootRange, 0.1f);
        }
#endif
    }
}
