using System;
using UnityEngine;

namespace ChatGo.Opponent
{
    public class OpponentHealth : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;

        public static OpponentHealth Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        public int CurrentHealth { get; private set; }
        public int MaxHealth => Mathf.Max(1, maxHealth);
        public bool IsDead => CurrentHealth <= 0;

        public event Action<int, int> HealthChanged;
        public event Action Died;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("OpponentHealth: 场景中存在多个实例，销毁多余的。");
                Destroy(this);
                return;
            }

            Instance = this;
            CurrentHealth = MaxHealth;
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void TakeDamage(int amount)
        {
            int damage = Mathf.Max(0, amount);
            if (damage <= 0 || IsDead)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
            Debug.Log($"OpponentHealth: 受到 {damage} 点伤害，剩余 {CurrentHealth}");
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);

            if (CurrentHealth <= 0)
            {
                Debug.Log("OpponentHealth: Opponent 已被消灭。");
                Died?.Invoke();
            }
        }

        public void ResetToMax()
        {
            CurrentHealth = MaxHealth;
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }
    }
}
