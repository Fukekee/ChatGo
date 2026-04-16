using System.Collections;
using ChatGo.Core;
using ChatGo.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChatGo.UI
{
    public class ContactDetailUI : MonoBehaviour
    {
        [Header("面板根节点")]
        [SerializeField] private RectTransform panelRoot;

        [Header("顶部")]
        [SerializeField] private Image headerAvatar;
        [SerializeField] private TMP_Text headerName;
        [SerializeField] private Button backButton;

        [Header("关卡列表")]
        [SerializeField] private Transform levelListContainer;
        [SerializeField] private GameObject levelRowPrefab;

        [Header("动画")]
        [SerializeField] private float slideDuration = 0.3f;

        private ContactData currentContact;
        private Coroutine slideCoroutine;

        private void Awake()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(Hide);
            }

            if (panelRoot != null)
            {
                panelRoot.gameObject.SetActive(false);
            }
        }

        public void Show(ContactData contact)
        {
            currentContact = contact;

            if (headerAvatar != null)
            {
                if (contact.avatar != null)
                {
                    headerAvatar.sprite = contact.avatar;
                    headerAvatar.enabled = true;
                }
                else
                {
                    headerAvatar.enabled = false;
                }
            }

            if (headerName != null)
            {
                headerName.text = contact.displayName;
            }

            BuildLevelList();

            panelRoot.gameObject.SetActive(true);
            SlideIn();
        }

        public void Hide()
        {
            SlideOut();
        }

        private void BuildLevelList()
        {
            if (levelListContainer == null) return;

            for (int i = levelListContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(levelListContainer.GetChild(i).gameObject);
            }

            if (currentContact == null || currentContact.levels == null) return;

            for (int i = 0; i < currentContact.levels.Length; i++)
            {
                var level = currentContact.levels[i];
                bool unlocked = LevelProgress.IsUnlocked(currentContact, i);
                bool completed = LevelProgress.IsCompleted(level.levelId);
                string bestGrade = LevelProgress.GetBestGrade(level.levelId);
                int currentIndex = LevelProgress.GetCurrentLevelIndex(currentContact);
                bool isCurrent = i == currentIndex && !completed;

                GameObject row;
                if (levelRowPrefab != null)
                {
                    row = Instantiate(levelRowPrefab, levelListContainer);
                }
                else
                {
                    row = BuildLevelRowFromCode(level, unlocked, completed, bestGrade, isCurrent);
                }

                row.name = $"LevelRow_{level.levelId}";

                SetupLevelRowText(row, level, unlocked, completed, bestGrade, isCurrent);

                if (unlocked)
                {
                    var button = row.GetComponent<Button>();
                    if (button == null)
                    {
                        button = row.AddComponent<Button>();
                    }

                    var capturedLevel = level;
                    button.onClick.AddListener(() =>
                    {
                        Debug.Log($"ContactDetailUI: 进入 [{capturedLevel.displayName}] -> {capturedLevel.sceneName}");
                        LevelManager.LoadLevel(capturedLevel.sceneName, capturedLevel.levelId);
                    });

                    var colors = button.colors;
                    colors.normalColor = isCurrent
                        ? new Color(0.9f, 0.97f, 0.95f)
                        : Color.white;
                    colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f);
                    colors.pressedColor = new Color(0.9f, 0.9f, 0.9f);
                    button.colors = colors;
                }
                else
                {
                    var button = row.GetComponent<Button>();
                    if (button != null)
                    {
                        button.interactable = false;
                    }
                }
            }
        }

        private void SetupLevelRowText(GameObject row, LevelData level, bool unlocked,
            bool completed, string bestGrade, bool isCurrent)
        {
            var texts = row.GetComponentsInChildren<TMP_Text>(true);
            foreach (var text in texts)
            {
                switch (text.gameObject.name)
                {
                    case "LevelName":
                        text.text = level.displayName;
                        text.color = unlocked
                            ? new Color(0.13f, 0.13f, 0.13f)
                            : new Color(0.7f, 0.7f, 0.7f);
                        if (isCurrent) text.fontStyle = FontStyles.Bold;
                        break;
                    case "LevelStatus":
                        if (!unlocked)
                        {
                            text.text = string.IsNullOrEmpty(level.requiredGrade)
                                ? "🔒 未解锁"
                                : $"🔒 需要上一关达到 {level.requiredGrade}";
                            text.color = new Color(0.7f, 0.7f, 0.7f);
                        }
                        else if (completed)
                        {
                            text.text = string.IsNullOrEmpty(bestGrade)
                                ? "✅ 已通关"
                                : $"✅ 最佳评级: {bestGrade}";
                            text.color = new Color(0.33f, 0.69f, 0.33f);
                        }
                        else if (isCurrent)
                        {
                            text.text = "▶ 新关卡";
                            text.color = new Color(0f, 0.59f, 0.53f);
                        }
                        else
                        {
                            text.text = "";
                        }
                        break;
                }
            }
        }

        private GameObject BuildLevelRowFromCode(LevelData level, bool unlocked,
            bool completed, string bestGrade, bool isCurrent)
        {
            float rowHeight = 140;

            var row = new GameObject("LevelRow", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            row.transform.SetParent(levelListContainer, false);

            if (!unlocked)
                row.GetComponent<Image>().color = new Color(0.96f, 0.96f, 0.96f);
            else if (isCurrent)
                row.GetComponent<Image>().color = new Color(0.9f, 0.97f, 0.95f);
            else
                row.GetComponent<Image>().color = Color.white;

            row.GetComponent<LayoutElement>().preferredHeight = rowHeight;

            var nameObj = new GameObject("LevelName", typeof(RectTransform), typeof(TMP_Text));
            nameObj.transform.SetParent(row.transform, false);
            var nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.5f);
            nameRect.anchorMax = new Vector2(0.6f, 1f);
            nameRect.offsetMin = new Vector2(40, 0);
            nameRect.offsetMax = Vector2.zero;

            var nameText = nameObj.GetComponent<TMP_Text>();
            nameText.text = level.displayName;
            nameText.fontSize = 36;
            nameText.alignment = TextAlignmentOptions.BottomLeft;
            nameText.color = unlocked ? new Color(0.13f, 0.13f, 0.13f) : new Color(0.7f, 0.7f, 0.7f);
            if (isCurrent) nameText.fontStyle = FontStyles.Bold;

            var statusObj = new GameObject("LevelStatus", typeof(RectTransform), typeof(TMP_Text));
            statusObj.transform.SetParent(row.transform, false);
            var statusRect = statusObj.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0, 0);
            statusRect.anchorMax = new Vector2(1f, 0.5f);
            statusRect.offsetMin = new Vector2(40, 0);
            statusRect.offsetMax = new Vector2(-30, 0);

            var statusText = statusObj.GetComponent<TMP_Text>();
            statusText.fontSize = 28;
            statusText.alignment = TextAlignmentOptions.TopLeft;

            var divider = new GameObject("Divider", typeof(RectTransform), typeof(Image));
            divider.transform.SetParent(row.transform, false);
            var divRect = divider.GetComponent<RectTransform>();
            divRect.anchorMin = new Vector2(0, 0);
            divRect.anchorMax = new Vector2(1, 0);
            divRect.pivot = new Vector2(0.5f, 0);
            divRect.offsetMin = new Vector2(40, 0);
            divRect.offsetMax = new Vector2(0, 2);
            divider.GetComponent<Image>().color = new Color(0.92f, 0.92f, 0.92f);

            return row;
        }

        private void SlideIn()
        {
            if (slideCoroutine != null) StopCoroutine(slideCoroutine);
            slideCoroutine = StartCoroutine(SlideAnimation(1f, 0f));
        }

        private void SlideOut()
        {
            if (slideCoroutine != null) StopCoroutine(slideCoroutine);
            slideCoroutine = StartCoroutine(SlideAnimation(0f, 1f, deactivateOnComplete: true));
        }

        private IEnumerator SlideAnimation(float fromX, float toX, bool deactivateOnComplete = false)
        {
            if (panelRoot == null) yield break;

            float elapsed = 0f;
            Vector2 size = panelRoot.rect.size;
            float screenWidth = size.x > 0 ? size.x : Screen.width;

            while (elapsed < slideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
                float x = Mathf.Lerp(fromX * screenWidth, toX * screenWidth, t);
                panelRoot.anchoredPosition = new Vector2(x, panelRoot.anchoredPosition.y);
                yield return null;
            }

            panelRoot.anchoredPosition = new Vector2(toX * screenWidth, panelRoot.anchoredPosition.y);

            if (deactivateOnComplete)
            {
                panelRoot.gameObject.SetActive(false);
            }

            slideCoroutine = null;
        }
    }
}
