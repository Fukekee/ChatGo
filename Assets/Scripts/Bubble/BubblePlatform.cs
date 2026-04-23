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

        [Header("机关（玩家选项）")]
        [Tooltip("按顺序拖入各套机关根物体；Prefab 里默认全部关闭。选项通过 DialogueChoice.hazardGroupIndex 指定启用哪一项")]
        [SerializeField] private GameObject[] hazardGroups;

        public bool AvatarOnLeft => avatarOnLeft;
        public ReadReceiptTrigger ReadReceiptTrigger => readReceiptTrigger;
        public DialogueNode CurrentNode { get; private set; }

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

        public void Init(DialogueNode nodeData)
        {
            CurrentNode = nodeData;
            ResolveChatTextIfNeeded();

            bool isPlayerLine = nodeData != null && nodeData.speaker == SpeakerSide.Player;
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
                    chatText.text = nodeData?.text ?? string.Empty;
                }
            }

            if (readReceiptTrigger != null)
            {
                readReceiptTrigger.BindPlatform(this);
                readReceiptTrigger.ResetTriggerState();
            }

            ResetHazardGroups();
        }

        /// <summary>关闭 hazardGroups 中所有项（换行或选关机关时调用）。</summary>
        public void ResetHazardGroups()
        {
            if (hazardGroups == null)
            {
                return;
            }

            for (int i = 0; i < hazardGroups.Length; i++)
            {
                if (hazardGroups[i] != null)
                {
                    hazardGroups[i].SetActive(false);
                }
            }
        }

        /// <summary>根据选项启用 hazardGroups[hazardGroupIndex]，其余关闭。</summary>
        public void ApplyHazardFromChoice(DialogueChoice choice)
        {
            if (choice == null || !choice.useHazardGroup)
            {
                ResetHazardGroups();
                return;
            }

            ResetHazardGroups();
            int index = choice.hazardGroupIndex;
            if (hazardGroups == null || index < 0 || index >= hazardGroups.Length)
            {
                return;
            }

            if (hazardGroups[index] != null)
            {
                hazardGroups[index].SetActive(true);
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
