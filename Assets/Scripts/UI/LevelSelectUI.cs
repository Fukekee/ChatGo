using System;
using System.Collections.Generic;
using ChatGo.Core;
using ChatGo.Data;
using UnityEngine;

namespace ChatGo.UI
{
    public class LevelSelectUI : MonoBehaviour
    {
        [Header("联系人列表")]
        [SerializeField] private ContactData[] contacts;

        [Header("UI 引用（请在 Inspector 中拖好）")]
        [SerializeField] private Transform chatListContainer;
        [SerializeField] private GameObject chatRowPrefab;

        [Header("联系人详情页")]
        [SerializeField] private ContactDetailUI contactDetailUI;

        private void Start()
        {
            if (chatListContainer == null)
            {
                Debug.LogError("LevelSelectUI: chatListContainer 未配置，请在 Inspector 中拖入聊天列表容器。");
                return;
            }

            if (chatRowPrefab == null)
            {
                Debug.LogError("LevelSelectUI: chatRowPrefab 未配置。");
                return;
            }

            BuildChatRows();
        }

        private void BuildChatRows()
        {
            var sorted = GetSortedContacts();

            foreach (var contact in sorted)
            {
                bool hasAnyUnlocked = false;
                for (int i = 0; i < contact.levels.Length; i++)
                {
                    if (LevelProgress.IsUnlocked(contact, i))
                    {
                        hasAnyUnlocked = true;
                        break;
                    }
                }
                if (!hasAnyUnlocked) continue;

                var row = Instantiate(chatRowPrefab, chatListContainer);
                row.name = $"ChatRow_{contact.contactId}";

                var chatRowUI = row.GetComponent<ChatRowUI>();
                if (chatRowUI != null)
                {
                    chatRowUI.Setup(contact);

                    var capturedContact = contact;
                    chatRowUI.OnRowClicked += () => OnContactRowClicked(capturedContact);
                    chatRowUI.OnAvatarClicked += () => OnContactAvatarClicked(capturedContact);
                }
            }
        }

        private List<ContactData> GetSortedContacts()
        {
            var list = new List<ContactData>(contacts);

            list.Sort((a, b) =>
            {
                long tsA = LevelProgress.GetContactLatestTimestamp(a);
                long tsB = LevelProgress.GetContactLatestTimestamp(b);

                if (tsA != tsB) return tsB.CompareTo(tsA);

                return Array.IndexOf(contacts, a).CompareTo(Array.IndexOf(contacts, b));
            });

            return list;
        }

        private void OnContactRowClicked(ContactData contact)
        {
            int index = LevelProgress.GetCurrentLevelIndex(contact);
            var level = contact.levels[index];
            Debug.Log($"LevelSelectUI: 进入 [{contact.displayName}] 关卡 [{level.displayName}] -> {level.sceneName}");
            LevelManager.LoadLevel(level.sceneName, level.levelId);
        }

        private void OnContactAvatarClicked(ContactData contact)
        {
            if (contactDetailUI != null)
            {
                contactDetailUI.Show(contact);
            }
            else
            {
                Debug.LogWarning("LevelSelectUI: contactDetailUI 未配置，直接进入关卡。");
                OnContactRowClicked(contact);
            }
        }
    }
}
