using UnityEngine;

namespace ChatGo.Hazards
{
    [RequireComponent(typeof(Collider2D))]
    public class TurretBullet : MonoBehaviour
    {
        [SerializeField] private float speed = 8f;
        [SerializeField] private int damage = 10;
        [SerializeField] private float lifeTime = 4f;
        [SerializeField] private LayerMask blockLayers;

        private Vector2 direction = Vector2.left;
        private float lifeTimer;
        private bool isActive;

        public void Activate(Vector2 moveDirection, float moveSpeed, int hitDamage)
        {
            direction = moveDirection.sqrMagnitude > 0.0001f ? moveDirection.normalized : Vector2.left;
            speed = moveSpeed;
            damage = hitDamage;
            lifeTimer = lifeTime;
            isActive = true;
            gameObject.SetActive(true);
        }

        private void OnEnable()
        {
            lifeTimer = lifeTime;
            isActive = true;
        }

        private void Update()
        {
            if (!isActive)
            {
                return;
            }

            transform.position += (Vector3)(direction * (speed * Time.deltaTime));
            lifeTimer -= Time.deltaTime;

            if (lifeTimer <= 0f)
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

            if (other.CompareTag("Player"))
            {
                other.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
                Deactivate();
                return;
            }

            int otherLayerMask = 1 << other.gameObject.layer;
            if ((blockLayers.value & otherLayerMask) != 0)
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
