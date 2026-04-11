using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChatGo.UI
{
    public class ReplyPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button option1Button;
        [SerializeField] private Button option2Button;
        [SerializeField] private TMP_Text option1Text;
        [SerializeField] private TMP_Text option2Text;

        private Action<string> onSelectReply;

        private void Awake()
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }
        }

        public void Show(string option1, string option2, Action<string> onSelected)
        {
            onSelectReply = onSelected;

            string text1 = string.IsNullOrWhiteSpace(option1) ? "好的" : option1;
            string text2 = string.IsNullOrWhiteSpace(option2) ? "收到" : option2;

            if (option1Text != null)
            {
                option1Text.text = text1;
            }

            if (option2Text != null)
            {
                option2Text.text = text2;
            }

            if (option1Button != null)
            {
                option1Button.onClick.RemoveAllListeners();
                option1Button.onClick.AddListener(() => SelectReply(text1));
            }

            if (option2Button != null)
            {
                option2Button.onClick.RemoveAllListeners();
                option2Button.onClick.AddListener(() => SelectReply(text2));
            }

            panelRoot.SetActive(true);
        }

        public void Hide()
        {
            panelRoot.SetActive(false);
        }

        private void SelectReply(string replyText)
        {
            onSelectReply?.Invoke(replyText);
            Hide();
        }
    }
}
