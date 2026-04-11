using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChatGo.Hazards
{
    public class FileRainSpawner : MonoBehaviour
    {
        [Header("下落物预制体（可配置多个样式）")]
        [SerializeField] private FileRainDrop[] dropPrefabs;

        [Header("一次波次")]
        [SerializeField] private int spawnCount = 8;
        [SerializeField] private float spawnInterval = 0.12f;

        [Header("生成范围（本地坐标）")]
        [SerializeField] private float minX = -4f;
        [SerializeField] private float maxX = 4f;
        [SerializeField] private float spawnY = 5f;

        [Header("回收线（本地坐标 Y）")]
        [SerializeField] private float despawnY = -6f;

        [Header("对象池")]
        [SerializeField] private int prewarmCount = 12;
        [SerializeField] private Transform poolRoot;

        private readonly Queue<FileRainDrop> pool = new();
        private bool hasTriggered;
        private Coroutine spawnCoroutine;

        public void TryTrigger()
        {
            if (hasTriggered || !enabled)
            {
                return;
            }

            if (dropPrefabs == null || dropPrefabs.Length == 0)
            {
                Debug.LogError("FileRainSpawner: 未配置 dropPrefabs。");
                return;
            }

            hasTriggered = true;
            spawnCoroutine = StartCoroutine(SpawnWaveOnce());
        }

        public void ReturnToPool(FileRainDrop drop)
        {
            if (drop == null)
            {
                return;
            }

            drop.gameObject.SetActive(false);
            if (poolRoot != null)
            {
                drop.transform.SetParent(poolRoot, true);
            }

            pool.Enqueue(drop);
        }

        private void Awake()
        {
            if (poolRoot == null)
            {
                poolRoot = transform;
            }

            if (dropPrefabs != null && dropPrefabs.Length > 0)
            {
                Prewarm();
            }
        }

        private void OnDisable()
        {
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
        }

        private IEnumerator SpawnWaveOnce()
        {
            int count = Mathf.Max(0, spawnCount);
            for (int i = 0; i < count; i++)
            {
                SpawnOne();

                if (i < count - 1 && spawnInterval > 0f)
                {
                    yield return new WaitForSeconds(spawnInterval);
                }
            }

            spawnCoroutine = null;
        }

        private void SpawnOne()
        {
            FileRainDrop drop = GetFromPool();
            if (drop == null)
            {
                return;
            }

            float x = Random.Range(minX, maxX);
            Vector3 spawnPosition = transform.TransformPoint(new Vector3(x, spawnY, 0f));
            float despawnWorldY = transform.TransformPoint(new Vector3(0f, despawnY, 0f)).y;
            drop.Activate(this, spawnPosition, despawnWorldY);
        }

        private FileRainDrop GetFromPool()
        {
            if (pool.Count > 0)
            {
                return pool.Dequeue();
            }

            return CreateInstance();
        }

        private void Prewarm()
        {
            int count = Mathf.Max(0, prewarmCount);
            for (int i = 0; i < count; i++)
            {
                FileRainDrop instance = CreateInstance();
                if (instance == null)
                {
                    continue;
                }

                instance.gameObject.SetActive(false);
                pool.Enqueue(instance);
            }
        }

        private FileRainDrop CreateInstance()
        {
            if (dropPrefabs == null || dropPrefabs.Length == 0)
            {
                return null;
            }

            int index = Random.Range(0, dropPrefabs.Length);
            FileRainDrop prefab = dropPrefabs[index];
            if (prefab == null)
            {
                return null;
            }

            FileRainDrop instance = Instantiate(prefab, poolRoot);
            instance.gameObject.SetActive(false);
            return instance;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 topLeft = transform.TransformPoint(new Vector3(minX, spawnY, 0f));
            Vector3 topRight = transform.TransformPoint(new Vector3(maxX, spawnY, 0f));
            Vector3 bottomLeft = transform.TransformPoint(new Vector3(minX, despawnY, 0f));
            Vector3 bottomRight = transform.TransformPoint(new Vector3(maxX, despawnY, 0f));

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(topLeft, topRight);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(bottomLeft, bottomRight);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(topLeft, bottomLeft);
            Gizmos.DrawLine(topRight, bottomRight);
        }
#endif
    }
}
