using System;
using UnityEngine;

namespace ChatGo.Data
{
    public static class CoinWallet
    {
        private const string SaveKey = "CoinWallet_Total";

        public static int Total
        {
            get => PlayerPrefs.GetInt(SaveKey, 0);
            private set
            {
                PlayerPrefs.SetInt(SaveKey, Mathf.Max(0, value));
                PlayerPrefs.Save();
            }
        }

        public static event Action<int> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Changed = null;
        }

        public static void Add(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Total += amount;
            Debug.Log($"CoinWallet: +{amount}, total = {Total}");
            Changed?.Invoke(Total);
        }

        public static bool TrySpend(int amount)
        {
            if (amount <= 0 || Total < amount)
            {
                return false;
            }

            Total -= amount;
            Debug.Log($"CoinWallet: -{amount}, total = {Total}");
            Changed?.Invoke(Total);
            return true;
        }
    }
}
