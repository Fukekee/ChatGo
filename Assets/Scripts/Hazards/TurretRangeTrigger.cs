using UnityEngine;

namespace ChatGo.Hazards
{
    [RequireComponent(typeof(Collider2D))]
    public class TurretRangeTrigger : MonoBehaviour
    {
        [SerializeField] private TurretShooter shooter;

        private int playerCountInRange;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            playerCountInRange++;
            shooter?.SetShootingActive(true);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            playerCountInRange = Mathf.Max(0, playerCountInRange - 1);
            if (playerCountInRange == 0)
            {
                shooter?.SetShootingActive(false);
            }
        }
    }
}
