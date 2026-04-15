using System;
using System.Collections.Generic;
using ChatGo.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChatGo.UI
{
    public class ReplyPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform buttonContainer;
        [Tooltip("可选：在 Inspector 中指定按钮预制体。为空时运行时自动创建。")]
        [SerializeField] private Button buttonPrefab;

        private readonly List<Button> spawnedButtons = new();
        private Action<DialogueChoice> onSelectChoice;

        private void Awake()
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            if (buttonContainer == null)
            {
                buttonContainer = panelRoot.transform;
            }
        }

        public void Show(DialogueChoice[] choices, Action<DialogueChoice> onSelected)
        {
            ClearButtons();
            onSelectChoice = onSelected;

            if (choices == null || choices.Length == 0)
            {
                return;
            }

            foreach (DialogueChoice choice in choices)
            {
                Button btn = CreateChoiceButton(choice);
                spawnedButtons.Add(btn);
            }

            panelRoot.SetActive(true);
        }

        public void Hide()
        {
            ClearButtons();
            panelRoot.SetActive(false);
        }

        private Button CreateChoiceButton(DialogueChoice choice)
        {
            Button btn;
            if (buttonPrefab != null)
            {
                btn = Instantiate(buttonPrefab, buttonContainer);
            }
            else
            {
                btn = CreateDefaultButton();
            }

            TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = string.IsNullOrWhiteSpace(choice.choiceText) ? "..." : choice.choiceText;
            }

            btn.onClick.RemoveAllListeners();
            DialogueChoice captured = choice;
            btn.onClick.AddListener(() =>
            {
                onSelectChoice?.Invoke(captured);
                Hide();
            });

            btn.gameObject.SetActive(true);
            return btn;
        }

        private Button CreateDefaultButton()
        {
            GameObject btnObj = new("ChoiceButton");
            btnObj.transform.SetParent(buttonContainer, false);

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.92f);

            Button btn = btnObj.AddComponent<Button>();

            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(180f, 56f);

            GameObject textNode = new("Label");
            textNode.transform.SetParent(btnObj.transform, false);
            RectTransform textRect = textNode.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TMP_Text text = textNode.AddComponent<TextMeshProUGUI>();
            text.text = "选项";
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.black;
            text.fontSize = 28f;

            return btn;
        }

        private void ClearButtons()
        {
            foreach (Button btn in spawnedButtons)
            {
                if (btn != null)
                {
                    Destroy(btn.gameObject);
                }
            }

            spawnedButtons.Clear();
        }
    }
}
