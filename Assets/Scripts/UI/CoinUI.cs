using ChatGo.Data;
using TMPro;
using UnityEngine;

namespace ChatGo.UI
{
    public class CoinUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text coinText;
        [SerializeField] private string format = "金币: {0}";

        private void OnEnable()
        {
            CoinWallet.Changed += OnCoinChanged;
            Refresh();
        }

        private void OnDisable()
        {
            CoinWallet.Changed -= OnCoinChanged;
        }

        private void OnCoinChanged(int total)
        {
            UpdateDisplay(total);
        }

        private void Refresh()
        {
            UpdateDisplay(CoinWallet.Total);
        }

        private void UpdateDisplay(int total)
        {
            if (coinText != null)
            {
                coinText.text = string.Format(format, total);
            }
        }
    }
}
