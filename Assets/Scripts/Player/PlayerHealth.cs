using System;
using UnityEngine;

namespace ChatGo.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private bool destroyOnZero = false;
        [SerializeField] private bool disableMovementOnDeath = true;
        [Tooltip("受伤后在此时间内免疫后续伤害（秒）")]
        [SerializeField] private float damageInvincibilityDuration = 0.3f;

        public static PlayerHealth Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }
        public int CurrentHealth { get; private set; }
        public int MaxHealth => Mathf.Max(1, maxHealth);
        public bool IsDead => CurrentHealth <= 0;
        public bool IsInvincible => !IsDead && Time.time < invincibilityEndTime;

        public event Action<int, int> HealthChanged;
        public event Action Died;

        private PlayerController playerController;
        private float invincibilityEndTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("PlayerHealth: duplicate instance detected, destroying latest one.");
                Destroy(this);
                return;
            }

            Instance = this;
            playerController = GetComponent<PlayerController>();
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
            if (damage <= 0 || IsDead || IsInvincible)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
            float duration = Mathf.Max(0f, damageInvincibilityDuration);
            if (duration > 0f)
            {
                invincibilityEndTime = Time.time + duration;
            }
            Debug.Log($"PlayerHealth: take {damage}, hp = {CurrentHealth}");
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);

            if (CurrentHealth == 0)
            {
                HandleDeath();
            }
        }

        public void Heal(int amount)
        {
            int heal = Mathf.Max(0, amount);
            if (heal <= 0 || CurrentHealth <= 0)
            {
                return;
            }

            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + heal);
            Debug.Log($"PlayerHealth: heal {heal}, hp = {CurrentHealth}");
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void ResetToMax()
        {
            invincibilityEndTime = 0f;
            CurrentHealth = MaxHealth;
            if (disableMovementOnDeath && playerController != null)
            {
                playerController.CanMove = true;
            }

            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        private void HandleDeath()
        {
            Debug.Log("PlayerHealth: player down.");

            if (disableMovementOnDeath && playerController != null)
            {
                playerController.CanMove = false;
            }

            Died?.Invoke();

            if (destroyOnZero)
            {
                Destroy(gameObject);
            }
        }
    }
}
