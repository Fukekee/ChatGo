using System;
using ChatGo.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChatGo.UI
{
    public class ChatRowUI : MonoBehaviour
    {
        [SerializeField] private Button avatarButton;
        [SerializeField] private Image avatarImage;
        [SerializeField] private TMP_Text contactNameText;
        [SerializeField] private TMP_Text messagePreviewText;
        [SerializeField] private TMP_Text timestampText;
        [SerializeField] private GameObject badgeRoot;
        [SerializeField] private TMP_Text badgeText;
        [SerializeField] private Image statusIcon;

        [Header("状态图标")]
        [SerializeField] private Sprite singleCheckSprite;
        [SerializeField] private Sprite doubleCheckSprite;
        [SerializeField] private Sprite blueCheckSprite;

        public event Action OnAvatarClicked;
        public event Action OnRowClicked;

        private Button rowButton;

        private void Awake()
        {
            if (avatarButton != null)
            {
                avatarButton.onClick.AddListener(() => OnAvatarClicked?.Invoke());
            }

            rowButton = GetComponent<Button>();
            if (rowButton != null)
            {
                rowButton.onClick.AddListener(() => OnRowClicked?.Invoke());
            }
        }

        public void Setup(ContactData contact)
        {
            if (contact == null) return;

            int currentIndex = LevelProgress.GetCurrentLevelIndex(contact);
            var currentLevel = contact.levels[currentIndex];
            bool allCompleted = true;
            int unreadCount = 0;

            for (int i = 0; i < contact.levels.Length; i++)
            {
                if (LevelProgress.IsUnlocked(contact, i) && !LevelProgress.IsCompleted(contact.levels[i].levelId))
                {
                    allCompleted = false;
                    unreadCount++;
                }
            }

            if (avatarImage != null)
            {
                if (contact.avatar != null)
                {
                    avatarImage.sprite = contact.avatar;
                    avatarImage.enabled = true;
                }
                else
                {
                    avatarImage.enabled = false;
                }
            }

            if (contactNameText != null)
            {
                contactNameText.text = contact.displayName;
            }

            if (messagePreviewText != null)
            {
                messagePreviewText.text = currentLevel.lastMessage;
            }

            if (timestampText != null)
            {
                timestampText.text = currentLevel.timestamp;
            }

            SetupBadge(unreadCount);
            SetupStatusIcon(allCompleted, contact);
        }

        private void SetupBadge(int unreadCount)
        {
            if (badgeRoot == null) return;

            if (unreadCount <= 0)
            {
                badgeRoot.SetActive(false);
                return;
            }

            badgeRoot.SetActive(true);
            if (badgeText != null)
            {
                badgeText.text = unreadCount.ToString();
            }
        }

        private void SetupStatusIcon(bool allCompleted, ContactData contact)
        {
            if (statusIcon == null) return;

            if (!allCompleted)
            {
                statusIcon.enabled = false;
                return;
            }

            bool allPerfected = true;
            foreach (var level in contact.levels)
            {
                string best = LevelProgress.GetBestGrade(level.levelId);
                if (LevelProgress.CompareGrade(best, "S") < 0)
                {
                    allPerfected = false;
                    break;
                }
            }

            if (allPerfected && blueCheckSprite != null)
            {
                statusIcon.sprite = blueCheckSprite;
                statusIcon.color = new Color(0.33f, 0.69f, 0.94f);
            }
            else if (doubleCheckSprite != null)
            {
                statusIcon.sprite = doubleCheckSprite;
                statusIcon.color = new Color(0.55f, 0.55f, 0.55f);
            }
            else
            {
                statusIcon.enabled = false;
                return;
            }

            statusIcon.enabled = true;
        }
    }
}
