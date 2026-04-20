using ChatGo.Conversation;
using ChatGo.Core;
using ChatGo.Data;
using ChatGo.Opponent;
using ChatGo.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChatGo.UI
{
    public class GameFlowUI : MonoBehaviour
    {
        [Header("返回按钮")]
        [SerializeField] private Button backButton;

        [Header("结算面板")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text resultTitle;
        [SerializeField] private TMP_Text gradeText;
        [SerializeField] private TMP_Text resultDescriptionText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button menuButton;

        private bool gameEnded;
        private ConversationManager conversationManager;
        private LevelResultConfig levelResultConfig;

        private void Start()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
            }

            if (retryButton != null)
            {
                retryButton.onClick.AddListener(OnRetryClicked);
            }

            if (menuButton != null)
            {
                menuButton.onClick.AddListener(OnMenuClicked);
            }

            if (resultPanel != null)
            {
                resultPanel.SetActive(false);
            }

            if (PlayerHealth.Instance != null)
            {
                PlayerHealth.Instance.Died += OnPlayerDied;
            }

            if (OpponentHealth.Instance != null)
            {
                OpponentHealth.Instance.Died += OnOpponentDied;
            }

            conversationManager = FindFirstObjectByType<ConversationManager>();
            if (conversationManager != null)
            {
                conversationManager.ConversationCompleted += OnConversationCompleted;
            }

            levelResultConfig = FindFirstObjectByType<LevelResultConfig>();
        }

        private void OnDestroy()
        {
            if (PlayerHealth.Instance != null)
            {
                PlayerHealth.Instance.Died -= OnPlayerDied;
            }

            if (OpponentHealth.Instance != null)
            {
                OpponentHealth.Instance.Died -= OnOpponentDied;
            }

            if (conversationManager != null)
            {
                conversationManager.ConversationCompleted -= OnConversationCompleted;
            }
        }

        private void OnPlayerDied()
        {
            string failText = levelResultConfig != null ? levelResultConfig.FailureText : "对话失败";
            ShowResult(failText, new Color(0.85f, 0.25f, 0.25f, 1f), null, null);
        }

        private void OnOpponentDied()
        {
            GradeResult topGrade = levelResultConfig != null ? levelResultConfig.GetHighestGrade() : null;
            string defeatedText = levelResultConfig != null ? levelResultConfig.OpponentDefeatedText : "对话成功！";
            string grade = topGrade != null ? topGrade.gradeName : "S";
            string description = topGrade != null ? topGrade.resultText : defeatedText;
            ShowResult(defeatedText, new Color(0.25f, 0.85f, 0.4f, 1f), grade, description);
        }

        private void OnConversationCompleted()
        {
            if (PlayerHealth.Instance != null && PlayerHealth.Instance.IsDead)
            {
                return;
            }

            int playerHP = PlayerHealth.Instance != null ? PlayerHealth.Instance.CurrentHealth : 0;
            int opponentHP = OpponentHealth.Instance != null ? OpponentHealth.Instance.CurrentHealth : 1;

            string grade = null;
            string description = null;

            if (opponentHP <= 0)
            {
                GradeResult topGrade = levelResultConfig != null ? levelResultConfig.GetHighestGrade() : null;
                grade = topGrade != null ? topGrade.gradeName : "S";
                description = topGrade != null ? topGrade.resultText : null;
            }
            else
            {
                float ratio = (float)playerHP / opponentHP;
                GradeResult evaluated = levelResultConfig != null ? levelResultConfig.EvaluateGrade(ratio) : null;
                if (evaluated != null)
                {
                    grade = evaluated.gradeName;
                    description = evaluated.resultText;
                }
            }

            ShowResult("对话成功！", new Color(0.25f, 0.85f, 0.4f, 1f), grade, description);
        }

        private void ShowResult(string title, Color titleColor, string grade, string description)
        {
            if (gameEnded)
            {
                return;
            }

            gameEnded = true;

            if (!string.IsNullOrEmpty(grade) && !string.IsNullOrEmpty(LevelManager.CurrentLevelId))
            {
                LevelProgress.SaveResult(LevelManager.CurrentLevelId, grade);
                TryUnlockNextLevel();
            }

            if (resultPanel != null)
            {
                resultPanel.SetActive(true);
            }

            if (resultTitle != null)
            {
                resultTitle.text = title;
                resultTitle.color = titleColor;
            }

            if (gradeText != null)
            {
                if (!string.IsNullOrEmpty(grade))
                {
                    gradeText.text = grade;
                    gradeText.gameObject.SetActive(true);
                }
                else
                {
                    gradeText.gameObject.SetActive(false);
                }
            }

            if (resultDescriptionText != null)
            {
                if (!string.IsNullOrEmpty(description))
                {
                    resultDescriptionText.text = description;
                    resultDescriptionText.gameObject.SetActive(true);
                }
                else
                {
                    resultDescriptionText.gameObject.SetActive(false);
                }
            }
        }

        private void TryUnlockNextLevel()
        {
            ContactData[] allContacts = null;

            var registry = ContactRegistry.Get();
            if (registry != null && registry.allContacts != null && registry.allContacts.Length > 0)
            {
                allContacts = registry.allContacts;
            }
            else
            {
                // 兜底：在主菜单场景里 ContactData 通常被 LevelSelectUI 引用，能扫到；
                // 在战斗场景里基本扫不到，所以更建议配好 ContactRegistry。
                allContacts = Resources.FindObjectsOfTypeAll<ContactData>();
                Debug.LogWarning($"GameFlowUI: 未找到 ContactRegistry，回退到 FindObjectsOfTypeAll，扫到 {allContacts.Length} 个 ContactData");
            }

            foreach (var contact in allContacts)
            {
                if (contact == null || contact.levels == null) continue;

                foreach (var level in contact.levels)
                {
                    if (level == null || string.IsNullOrEmpty(level.levelId)) continue;
                    // 起始关从一开始就解锁，不应被打 unlockTimestamp，
                    // 否则会和"刚被解锁的下一关"撞在同一 Unix 秒，
                    // 导致 GetContactLatestTimestamp 排序 tie，起始联系人反而置顶。
                    if (level.unlockedFromStart) continue;
                    if (LevelProgress.GetUnlockTimestamp(level.levelId) > 0) continue;

                    if (LevelProgress.IsUnlocked(level))
                    {
                        LevelProgress.MarkUnlocked(level.levelId);
                        Debug.Log($"GameFlowUI: 关卡解锁 -> {contact.displayName}·{level.displayName}");
                    }
                }
            }
        }

        private void OnBackClicked()
        {
            LevelManager.ReturnToMainMenu();
        }

        private void OnRetryClicked()
        {
            LevelManager.ReloadCurrentLevel();
        }

        private void OnMenuClicked()
        {
            LevelManager.ReturnToMainMenu();
        }
    }
}
