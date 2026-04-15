using ChatGo.Data;
using UnityEngine;

namespace ChatGo.Pickups
{
    [RequireComponent(typeof(Collider2D))]
    public class CoinPickup : MonoBehaviour
    {
        [SerializeField] private int value = 1;

        private bool collected;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected)
            {
                return;
            }

            if (!other.CompareTag("Player"))
            {
                return;
            }

            collected = true;
            CoinWallet.Add(value);
            gameObject.SetActive(false);
        }
    }
}
