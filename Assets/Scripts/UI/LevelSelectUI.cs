using System;
using ChatGo.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChatGo.UI
{
    [Serializable]
    public class LevelEntry
    {
        public string displayName;
        public string sceneName;
        [TextArea(1, 2)]
        public string description;
    }

    public class LevelSelectUI : MonoBehaviour
    {
        [Header("关卡列表")]
        [SerializeField] private LevelEntry[] levels;

        [Header("UI 引用")]
        [SerializeField] private Transform buttonContainer;
        [SerializeField] private GameObject buttonPrefab;

        private void Start()
        {
            BuildLevelButtons();
        }

        private void BuildLevelButtons()
        {
            if (buttonContainer == null || buttonPrefab == null)
            {
                Debug.LogError("LevelSelectUI: buttonContainer 或 buttonPrefab 未配置。");
                return;
            }

            for (int i = 0; i < levels.Length; i++)
            {
                LevelEntry level = levels[i];
                GameObject instance = Instantiate(buttonPrefab, buttonContainer);
                instance.name = $"LevelButton_{level.sceneName}";

                Button button = instance.GetComponentInChildren<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() => OnLevelClicked(level));
                }

                TMP_Text label = instance.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = level.displayName;
                }
            }
        }

        private void OnLevelClicked(LevelEntry level)
        {
            Debug.Log($"LevelSelectUI: 进入关卡 [{level.displayName}] -> {level.sceneName}");
            LevelManager.LoadLevel(level.sceneName);
        }
    }
}
