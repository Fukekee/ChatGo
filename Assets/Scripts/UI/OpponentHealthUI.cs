using ChatGo.Opponent;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChatGo.UI
{
    /// <summary>
    /// 订阅 OpponentHealth 事件，刷新屏幕上固定位置的血条与文本。
    /// </summary>
    public class OpponentHealthUI : MonoBehaviour
    {
        [SerializeField] private Slider healthSlider;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text defeatedHintText;
        [SerializeField] private string defeatedMessage = "Opponent 已被消灭";

        private bool subscribed;

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void Start()
        {
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

        private void Subscribe()
        {
            if (subscribed || OpponentHealth.Instance == null)
            {
                return;
            }

            OpponentHealth.Instance.HealthChanged += OnHealthChanged;
            OpponentHealth.Instance.Died += OnDefeated;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || OpponentHealth.Instance == null)
            {
                return;
            }

            OpponentHealth.Instance.HealthChanged -= OnHealthChanged;
            OpponentHealth.Instance.Died -= OnDefeated;
            subscribed = false;
        }

        private void OnHealthChanged(int current, int max)
        {
            SetDisplay(current, max, current <= 0);
        }

        private void OnDefeated()
        {
            if (defeatedHintText != null)
            {
                defeatedHintText.text = defeatedMessage;
                defeatedHintText.gameObject.SetActive(true);
            }

            Refresh();
        }

        private void Refresh()
        {
            if (OpponentHealth.Instance == null)
            {
                SetDisplay(0, 1, false);
                return;
            }

            SetDisplay(
                OpponentHealth.Instance.CurrentHealth,
                OpponentHealth.Instance.MaxHealth,
                OpponentHealth.Instance.IsDead);
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
                healthText.text = $"敌方: {Mathf.Max(0, current)}/{Mathf.Max(1, max)}";
            }

            if (defeatedHintText != null && !isDead)
            {
                defeatedHintText.gameObject.SetActive(false);
            }
        }
    }
}
