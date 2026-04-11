using System;
using ChatGo.Data;
using TMPro;
using UnityEngine;

namespace ChatGo.Bubble
{
    public class BubblePlatform : MonoBehaviour
    {
        [Header("平台标记")]
        [SerializeField] private bool avatarOnLeft = true;

        [Header("引用")]
        [SerializeField] private Transform startAnchor;
        [SerializeField] private Transform endAnchor;
        [SerializeField] private TMP_Text chatText;
        [SerializeField] private GameObject typingIndicator;
        [SerializeField] private ReadReceiptTrigger readReceiptTrigger;

        public bool AvatarOnLeft => avatarOnLeft;
        public ReadReceiptTrigger ReadReceiptTrigger => readReceiptTrigger;
        public DialogueLine CurrentLine { get; private set; }

        public void ConfigureRuntime(
            bool avatarOnLeftValue,
            Transform startAnchorValue,
            Transform endAnchorValue,
            TMP_Text chatTextValue,
            GameObject typingIndicatorValue,
            ReadReceiptTrigger readReceiptTriggerValue)
        {
            avatarOnLeft = avatarOnLeftValue;
            startAnchor = startAnchorValue;
            endAnchor = endAnchorValue;
            chatText = chatTextValue;
            typingIndicator = typingIndicatorValue;
            readReceiptTrigger = readReceiptTriggerValue;
        }

        public void Init(DialogueLine lineData)
        {
            CurrentLine = lineData;
            ResolveChatTextIfNeeded();

            bool isPlayerLine = lineData != null && lineData.speaker == SpeakerSide.Player;
            if (typingIndicator != null)
            {
                typingIndicator.SetActive(isPlayerLine);
            }

            if (chatText != null)
            {
                if (isPlayerLine)
                {
                    chatText.text = "正在输入中...";
                }
                else
                {
                    chatText.text = lineData?.text ?? string.Empty;
                }
            }

            if (readReceiptTrigger != null)
            {
                readReceiptTrigger.BindPlatform(this);
                readReceiptTrigger.ResetTriggerState();
            }
        }

        public void SetReplyText(string replyText)
        {
            ResolveChatTextIfNeeded();
            if (chatText != null)
            {
                chatText.text = replyText;
            }
#if UNITY_EDITOR
            else
            {
                Debug.LogWarning($"{nameof(BubblePlatform)} on {name}: 未找到聊天文本 TMP，请在 Inspector 绑定 Chat Text 或放置名为 ChatText 的子物体。");
            }
#endif

            if (typingIndicator != null)
            {
                typingIndicator.SetActive(false);
            }
        }

        private void ResolveChatTextIfNeeded()
        {
            if (chatText != null)
            {
                return;
            }

            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text t in texts)
            {
                if (typingIndicator != null && t.transform.IsChildOf(typingIndicator.transform))
                {
                    continue;
                }

                if (t.gameObject.name.IndexOf("ChatText", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    chatText = t;
                    return;
                }
            }

            foreach (TMP_Text t in texts)
            {
                if (typingIndicator != null && t.transform.IsChildOf(typingIndicator.transform))
                {
                    continue;
                }

                chatText = t;
                return;
            }
        }

        public Vector3 GetStartWorldPosition()
        {
            if (startAnchor != null)
            {
                return startAnchor.position;
            }

            return transform.position + Vector3.up * 0.6f;
        }

        public Vector3 GetCircleWorldPosition()
        {
            if (endAnchor != null)
            {
                return endAnchor.position;
            }

            return transform.position + Vector3.up;
        }
    }
}
