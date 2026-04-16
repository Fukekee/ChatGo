using System;
using System.Collections.Generic;
using ChatGo.Core;
using ChatGo.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChatGo.UI
{
    public class LevelSelectUI : MonoBehaviour
    {
        [Header("联系人列表")]
        [SerializeField] private ContactData[] contacts;

        [Header("UI 引用")]
        [SerializeField] private Transform chatListContainer;
        [SerializeField] private GameObject chatRowPrefab;

        [Header("联系人详情页")]
        [SerializeField] private ContactDetailUI contactDetailUI;

        [Header("WhatsApp 配色")]
        [SerializeField] private Color topBarColor = new Color(0f, 0.59f, 0.53f);
        [SerializeField] private Color backgroundColor = new Color(1f, 1f, 1f);

        private Canvas rootCanvas;

        private void Start()
        {
            if (chatListContainer == null)
            {
                BuildFullUI();
            }

            BuildChatRows();
        }

        private void BuildFullUI()
        {
            rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas == null)
            {
                rootCanvas = FindFirstObjectByType<Canvas>();
            }
            if (rootCanvas == null)
            {
                Debug.LogError("LevelSelectUI: 场景中没有 Canvas。");
                return;
            }

            ConfigureCanvasScaler();

            var bg = CreatePanel("Background", rootCanvas.transform);
            SetAnchorsStretch(bg.GetComponent<RectTransform>());
            bg.GetComponent<Image>().color = backgroundColor;

            CreateTopBar(bg.transform);
            CreateScrollArea(bg.transform);
        }

        private void ConfigureCanvasScaler()
        {
            var scaler = rootCanvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0f;
            }
        }

        private void CreateTopBar(Transform parent)
        {
            var topBar = CreatePanel("TopBar", parent);
            var topBarRect = topBar.GetComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0, 1);
            topBarRect.anchorMax = new Vector2(1, 1);
            topBarRect.pivot = new Vector2(0.5f, 1);
            topBarRect.sizeDelta = new Vector2(0, 160);
            topBarRect.anchoredPosition = Vector2.zero;
            topBar.GetComponent<Image>().color = topBarColor;

            var titleObj = new GameObject("Title", typeof(RectTransform), typeof(TMP_Text));
            titleObj.transform.SetParent(topBar.transform, false);
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = new Vector2(40, 10);
            titleRect.offsetMax = new Vector2(-40, -10);

            var titleText = titleObj.GetComponent<TMP_Text>();
            titleText.text = "ChatGo";
            titleText.fontSize = 52;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = Color.white;
            titleText.alignment = TextAlignmentOptions.BottomLeft;
        }

        private void CreateScrollArea(Transform parent)
        {
            var scrollObj = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollObj.transform.SetParent(parent, false);
            var scrollRect = scrollObj.GetComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(0, 0);
            scrollRect.offsetMax = new Vector2(0, -160);

            scrollObj.GetComponent<Image>().color = backgroundColor;

            var scroll = scrollObj.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollObj.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            SetAnchorsStretch(viewportRect);
            viewport.GetComponent<Image>().color = Color.white;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);

            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 0;

            var csf = content.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRect;
            scroll.content = contentRect;

            chatListContainer = content.transform;
        }

        private void BuildChatRows()
        {
            if (chatListContainer == null)
            {
                Debug.LogError("LevelSelectUI: chatListContainer 为空。");
                return;
            }

            if (chatRowPrefab == null)
            {
                Debug.LogError("LevelSelectUI: chatRowPrefab 未配置。");
                return;
            }

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

        private static GameObject CreatePanel(string name, Transform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            SetAnchorsStretch(obj.GetComponent<RectTransform>());
            return obj;
        }

        private static void SetAnchorsStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
