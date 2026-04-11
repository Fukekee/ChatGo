using UnityEngine;

namespace ChatGo.Hazards
{
    [RequireComponent(typeof(Collider2D))]
    public class FileRainTrigger : MonoBehaviour
    {
        [SerializeField] private FileRainSpawner spawner;
        [SerializeField] private bool triggerOnce = true;

        private bool used;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (spawner == null)
            {
                return;
            }

            if (triggerOnce && used)
            {
                return;
            }

            if (!other.CompareTag("Player"))
            {
                return;
            }

            spawner.TryTrigger();
            used = true;
        }
    }
}
