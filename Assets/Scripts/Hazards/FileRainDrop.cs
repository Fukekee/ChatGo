using UnityEngine;

namespace ChatGo.Hazards
{
    [RequireComponent(typeof(Collider2D))]
    public class FileRainDrop : MonoBehaviour
    {
        [Header("下落参数")]
        [SerializeField] private float fallSpeed = 6f;
        [SerializeField] private int damage = 10;
        [SerializeField] private LayerMask groundMask = 0;

        private FileRainSpawner owner;
        private float despawnY;
        private bool isActive;

        public void Activate(FileRainSpawner spawner, Vector3 worldPosition, float recycleY)
        {
            owner = spawner;
            despawnY = recycleY;
            transform.position = worldPosition;
            gameObject.SetActive(true);
            isActive = true;
        }

        private void Update()
        {
            if (!isActive)
            {
                return;
            }

            transform.position += Vector3.down * (fallSpeed * Time.deltaTime);

            if (transform.position.y <= despawnY)
            {
                Recycle();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isActive)
            {
                return;
            }

            if (other.CompareTag("Player"))
            {
                // 兼容后续接入任意血量组件，当前无对应组件时不会报错。
                other.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
                Recycle();
                return;
            }

            int hitLayerMask = 1 << other.gameObject.layer;
            if ((groundMask.value & hitLayerMask) != 0)
            {
                Recycle();
            }
        }

        private void Recycle()
        {
            if (!isActive)
            {
                return;
            }

            isActive = false;
            if (owner != null)
            {
                owner.ReturnToPool(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
