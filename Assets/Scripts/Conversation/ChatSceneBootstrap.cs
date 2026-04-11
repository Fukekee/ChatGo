using ChatGo.Bubble;
using ChatGo.Player;
using ChatGo.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ChatGo.Conversation
{
    public class ChatSceneBootstrap : MonoBehaviour
    {
        [SerializeField] private bool buildOnStart = true;

        private void Start()
        {
            if (!buildOnStart)
            {
                return;
            }

            BuildIfMissing();
        }

        [ContextMenu("Build Chat Scene")]
        public void BuildIfMissing()
        {
            EnsureEventSystem();

            ConversationManager conversationManager = FindFirstObjectByType<ConversationManager>();
            if (conversationManager != null)
            {
                return;
            }

            BubblePool bubblePool = EnsureBubblePool();
            PlayerController player = EnsurePlayer();
            CameraFollow cameraFollow = EnsureCameraFollow();
            ReplyPanel replyPanel = EnsureReplyPanel(player);

            GameObject managerNode = new("ConversationManager");
            conversationManager = managerNode.AddComponent<ConversationManager>();

            SerializedFieldSetter.SetField(conversationManager, "bubblePool", bubblePool);
            SerializedFieldSetter.SetField(conversationManager, "playerController", player);
            SerializedFieldSetter.SetField(conversationManager, "replyPanel", replyPanel);
            SerializedFieldSetter.SetField(conversationManager, "cameraFollow", cameraFollow);
            SerializedFieldSetter.SetField(conversationManager, "firstBubblePosition", new Vector3(0f, 0f, 0f));
            SerializedFieldSetter.SetField(conversationManager, "verticalSpacing", -3f);
        }

        private static BubblePool EnsureBubblePool()
        {
            BubblePool bubblePool = FindFirstObjectByType<BubblePool>();
            if (bubblePool != null)
            {
                return bubblePool;
            }

            GameObject poolNode = new("BubblePool");
            return poolNode.AddComponent<BubblePool>();
        }

        private static PlayerController EnsurePlayer()
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                return player;
            }

            GameObject playerNode = new("Player");
            playerNode.tag = "Player";
            playerNode.transform.position = new Vector3(-2f, 1f, 0f);

            Rigidbody2D rb = playerNode.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.freezeRotation = true;
            playerNode.AddComponent<BoxCollider2D>();

            GameObject feet = new("GroundCheck");
            feet.transform.SetParent(playerNode.transform, false);
            feet.transform.localPosition = new Vector3(0f, -0.52f, 0f);

            PlayerController controller = playerNode.AddComponent<PlayerController>();
            if (playerNode.GetComponent<PlayerHealth>() == null)
            {
                playerNode.AddComponent<PlayerHealth>();
            }
            SerializedFieldSetter.SetField(controller, "groundCheck", feet.transform);
            SerializedFieldSetter.SetField(controller, "groundCheckRadius", 0.16f);
            return controller;
        }

        private static CameraFollow EnsureCameraFollow()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject cameraNode = new("Main Camera");
                cameraNode.tag = "MainCamera";
                mainCamera = cameraNode.AddComponent<Camera>();
                mainCamera.orthographic = true;
                mainCamera.orthographicSize = 5.5f;
            }

            CameraFollow follow = mainCamera.GetComponent<CameraFollow>();
            if (follow == null)
            {
                follow = mainCamera.gameObject.AddComponent<CameraFollow>();
            }

            return follow;
        }

        private static ReplyPanel EnsureReplyPanel(PlayerController player)
        {
            ReplyPanel panel = FindFirstObjectByType<ReplyPanel>();
            if (panel != null)
            {
                return panel;
            }

            Canvas canvas = EnsureCanvas();

            GameObject panelRoot = new("ReplyPanel");
            panelRoot.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = panelRoot.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.1f, 0.03f);
            panelRect.anchorMax = new Vector2(0.9f, 0.2f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelImage = panelRoot.AddComponent<Image>();
            panelImage.color = new Color(0.18f, 0.18f, 0.18f, 0.7f);

            ReplyPanel replyPanel = panelRoot.AddComponent<ReplyPanel>();

            Button option1 = CreateButton(panelRoot.transform, "Option1", new Vector2(0.25f, 0.5f));
            Button option2 = CreateButton(panelRoot.transform, "Option2", new Vector2(0.75f, 0.5f));
            TMP_Text option1Text = option1.GetComponentInChildren<TMP_Text>();
            TMP_Text option2Text = option2.GetComponentInChildren<TMP_Text>();

            SerializedFieldSetter.SetField(replyPanel, "panelRoot", panelRoot);
            SerializedFieldSetter.SetField(replyPanel, "option1Button", option1);
            SerializedFieldSetter.SetField(replyPanel, "option2Button", option2);
            SerializedFieldSetter.SetField(replyPanel, "option1Text", option1Text);
            SerializedFieldSetter.SetField(replyPanel, "option2Text", option2Text);

            CreateMobileButtons(canvas.transform, player);

            panelRoot.SetActive(false);
            return replyPanel;
        }

        private static Canvas EnsureCanvas()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                return canvas;
            }

            GameObject canvasNode = new("Canvas");
            canvas = canvasNode.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasNode.AddComponent<CanvasScaler>();
            canvasNode.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void CreateMobileButtons(Transform canvas, PlayerController player)
        {
            GameObject inputBridgeNode = new("MobileInputBridge");
            inputBridgeNode.transform.SetParent(canvas, false);
            MobileInputBridge bridge = inputBridgeNode.AddComponent<MobileInputBridge>();
            bridge.Bind(player);

            Button leftButton = CreateButton(canvas, "LeftButton", new Vector2(0.12f, 0.1f), "左");
            Button rightButton = CreateButton(canvas, "RightButton", new Vector2(0.26f, 0.1f), "右");
            Button stopButton = CreateButton(canvas, "StopButton", new Vector2(0.40f, 0.1f), "停");
            Button jumpButton = CreateButton(canvas, "JumpButton", new Vector2(0.88f, 0.1f), "跳");

            leftButton.onClick.AddListener(bridge.HoldLeft);
            rightButton.onClick.AddListener(bridge.HoldRight);
            stopButton.onClick.AddListener(bridge.ReleaseHorizontal);
            jumpButton.onClick.AddListener(bridge.Jump);
        }

        private static Button CreateButton(Transform parent, string name, Vector2 anchor, string label = "选项")
        {
            GameObject buttonNode = new(name);
            buttonNode.transform.SetParent(parent, false);

            RectTransform rect = buttonNode.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(180f, 56f);

            Image image = buttonNode.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.92f);

            Button button = buttonNode.AddComponent<Button>();

            GameObject textNode = new("Label");
            textNode.transform.SetParent(buttonNode.transform, false);
            RectTransform textRect = textNode.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TMP_Text text = textNode.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.black;
            text.fontSize = 28f;

            return button;
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemNode = new("EventSystem");
                eventSystem = eventSystemNode.AddComponent<EventSystem>();
            }

            StandaloneInputModule oldModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (oldModule != null)
            {
                Destroy(oldModule);
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }
    }

    internal static class SerializedFieldSetter
    {
        public static void SetField<TTarget, TValue>(TTarget target, string fieldName, TValue value)
            where TTarget : Object
        {
            if (target == null)
            {
                return;
            }

            System.Reflection.FieldInfo field = typeof(TTarget)
                .GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            field?.SetValue(target, value);
        }
    }
}
