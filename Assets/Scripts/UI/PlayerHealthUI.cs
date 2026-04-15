using ChatGo.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChatGo.UI
{
    public class PlayerHealthUI : MonoBehaviour
    {
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private Slider healthSlider;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text deathHintText;
        [SerializeField] private string deathHintMessage = "你已死亡，无法继续移动";

        private bool subscribed;

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void Start()
        {
            if (playerHealth == null)
            {
                playerHealth = PlayerHealth.Instance;
            }

            if (!subscribed)
            {
                Subscribe();
                Refresh();
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Bind(PlayerHealth health)
        {
            if (playerHealth == health)
            {
                Refresh();
                return;
            }

            Unsubscribe();
            playerHealth = health;
            Subscribe();
            Refresh();
        }

        private void Subscribe()
        {
            if (subscribed || playerHealth == null)
            {
                return;
            }

            playerHealth.HealthChanged += OnHealthChanged;
            playerHealth.Died += OnDied;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || playerHealth == null)
            {
                return;
            }

            playerHealth.HealthChanged -= OnHealthChanged;
            playerHealth.Died -= OnDied;
            subscribed = false;
        }

        private void OnHealthChanged(int current, int max)
        {
            SetDisplay(current, max, current <= 0);
        }

        private void OnDied()
        {
            if (deathHintText != null)
            {
                deathHintText.text = deathHintMessage;
                deathHintText.gameObject.SetActive(true);
            }

            Refresh();
        }

        private void Refresh()
        {
            if (playerHealth == null)
            {
                SetDisplay(0, 1, false);
                return;
            }

            SetDisplay(playerHealth.CurrentHealth, playerHealth.MaxHealth, playerHealth.IsDead);
        }

        private void SetDisplay(int current, int max, bool isDead)
        {
            if (healthSlider != null)
            {
                healthSlider.minValue = 0f;
                healthSlider.maxValue = Mathf.Max(1, max);
                healthSlider.value = Mathf.Clamp(current, 0, max);
            }

            if (healthText != null)
            {
                healthText.text = $"HP: {Mathf.Max(0, current)}/{Mathf.Max(1, max)}";
            }

            if (deathHintText != null && !isDead)
            {
                deathHintText.gameObject.SetActive(false);
            }
        }
    }
}
