using System.Collections;
using UnityEngine;

namespace ChatGo.Hazards
{
    public class TurretShooter : MonoBehaviour
    {
        [Header("发射设置")]
        [SerializeField] private TurretBullet bulletPrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private Vector2 fireDirection = Vector2.left;
        [SerializeField] private float fireInterval = 1.2f;
        [SerializeField] private float bulletSpeed = 8f;
        [SerializeField] private int bulletDamage = 10;
        [SerializeField] private bool fireImmediatelyOnActivate = true;

        private bool isActive;
        private Coroutine fireCoroutine;

        public void SetShootingActive(bool active)
        {
            if (isActive == active)
            {
                return;
            }

            isActive = active;
            if (isActive)
            {
                StartShooting();
            }
            else
            {
                StopShooting();
            }
        }

        private void OnDisable()
        {
            StopShooting();
        }

        private void StartShooting()
        {
            if (fireCoroutine != null)
            {
                StopCoroutine(fireCoroutine);
            }

            fireCoroutine = StartCoroutine(FireLoop());
        }

        private void StopShooting()
        {
            if (fireCoroutine != null)
            {
                StopCoroutine(fireCoroutine);
                fireCoroutine = null;
            }
        }

        private IEnumerator FireLoop()
        {
            if (fireImmediatelyOnActivate)
            {
                SpawnBullet();
            }

            while (true)
            {
                yield return new WaitForSeconds(Mathf.Max(0.01f, fireInterval));
                SpawnBullet();
            }
        }

        private void SpawnBullet()
        {
            if (bulletPrefab == null)
            {
                return;
            }

            Transform spawnPoint = firePoint != null ? firePoint : transform;
            TurretBullet bullet = Instantiate(bulletPrefab, spawnPoint.position, Quaternion.identity);
            bullet.Activate(fireDirection, bulletSpeed, bulletDamage);
        }
    }
}
