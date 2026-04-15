using System.Collections.Generic;
using ChatGo.Data;
using TMPro;
using UnityEngine;

namespace ChatGo.Bubble
{
    public class BubblePool : MonoBehaviour
    {
        [Header("可选：手动指定预制体，不指定则自动创建占位模板")]
        [SerializeField] private BubblePlatform opponentBubblePrefab;
        [SerializeField] private BubblePlatform playerBubblePrefab;
        [SerializeField] private Transform spawnParent;

        [Header("池配置")]
        [SerializeField] private int prewarmPerType = 2;
        [SerializeField] private int maxVisibleBubbles = 3;

        private readonly Queue<BubblePlatform> opponentPool = new();
        private readonly Queue<BubblePlatform> playerPool = new();
        private readonly LinkedList<BubblePlatform> activeBubbles = new();

        private BubblePlatform runtimeOpponentTemplate;
        private BubblePlatform runtimePlayerTemplate;

        public IReadOnlyCollection<BubblePlatform> ActiveBubbles => activeBubbles;

        private void Awake()
        {
            if (spawnParent == null)
            {
                spawnParent = transform;
            }

            EnsureTemplates();
            PrewarmPool(opponentPool, runtimeOpponentTemplate);
            PrewarmPool(playerPool, runtimePlayerTemplate);
        }

        public BubblePlatform SpawnLine(DialogueNode nodeData, Vector3 worldPosition)
        {
            if (nodeData == null)
            {
                return null;
            }

            Queue<BubblePlatform> pool = nodeData.speaker == SpeakerSide.Player ? playerPool : opponentPool;
            BubblePlatform template = nodeData.speaker == SpeakerSide.Player ? runtimePlayerTemplate : runtimeOpponentTemplate;
            BubblePlatform instance = pool.Count > 0 ? pool.Dequeue() : CreateFromTemplate(template);

            instance.transform.SetParent(spawnParent, true);
            instance.transform.position = worldPosition;
            instance.gameObject.SetActive(true);
            instance.Init(nodeData);

            activeBubbles.AddLast(instance);
            if (activeBubbles.Count > maxVisibleBubbles)
            {
                RecycleOldest();
            }

            return instance;
        }

        public void RecycleOldest()
        {
            if (activeBubbles.Count == 0)
            {
                return;
            }

            BubblePlatform oldest = activeBubbles.First.Value;
            activeBubbles.RemoveFirst();
            RecycleBubble(oldest);
        }

        private void RecycleBubble(BubblePlatform bubble)
        {
            if (bubble == null)
            {
                return;
            }

            bubble.gameObject.SetActive(false);
            Queue<BubblePlatform> targetPool = bubble.AvatarOnLeft ? opponentPool : playerPool;
            targetPool.Enqueue(bubble);
        }

        private void EnsureTemplates()
        {
            runtimeOpponentTemplate = opponentBubblePrefab != null
                ? opponentBubblePrefab
                : BubbleRuntimeFactory.CreateTemplate("OpponentBubbleTemplate", true, spawnParent);

            runtimePlayerTemplate = playerBubblePrefab != null
                ? playerBubblePrefab
                : BubbleRuntimeFactory.CreateTemplate("PlayerBubbleTemplate", false, spawnParent);

            runtimeOpponentTemplate.gameObject.SetActive(false);
            runtimePlayerTemplate.gameObject.SetActive(false);
        }

        private void PrewarmPool(Queue<BubblePlatform> pool, BubblePlatform template)
        {
            for (int i = 0; i < Mathf.Max(1, prewarmPerType); i++)
            {
                BubblePlatform instance = CreateFromTemplate(template);
                instance.gameObject.SetActive(false);
                pool.Enqueue(instance);
            }
        }

        private BubblePlatform CreateFromTemplate(BubblePlatform template)
        {
            return Instantiate(template, spawnParent);
        }
    }

    internal static class BubbleRuntimeFactory
    {
        public static BubblePlatform CreateTemplate(string name, bool avatarOnLeft, Transform parent)
        {
            GameObject root = new(name);
            root.transform.SetParent(parent, false);

            BubblePlatform platform = root.AddComponent<BubblePlatform>();

            GameObject surface = new("Surface");
            surface.transform.SetParent(root.transform, false);
            surface.transform.localScale = new Vector3(6f, 1f, 1f);

            BoxCollider2D collider = surface.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1f, 0.18f);
            collider.offset = new Vector2(0f, 0.48f);

            GameObject startAnchor = new("StartAnchor");
            startAnchor.transform.SetParent(root.transform, false);
            startAnchor.transform.localPosition = avatarOnLeft ? new Vector3(-2.6f, 0.65f, 0f) : new Vector3(2.6f, 0.65f, 0f);

            GameObject endAnchor = new("EndAnchor");
            endAnchor.transform.SetParent(root.transform, false);
            endAnchor.transform.localPosition = avatarOnLeft ? new Vector3(2.6f, 1.35f, 0f) : new Vector3(-2.6f, 1.35f, 0f);

            GameObject textNode = new("ChatText");
            textNode.transform.SetParent(root.transform, false);
            textNode.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            TextMeshPro text = textNode.AddComponent<TextMeshPro>();
            text.fontSize = 3.6f;
            text.text = "占位聊天内容";
            text.alignment = TextAlignmentOptions.Midline;

            GameObject typingNode = new("TypingIndicator");
            typingNode.transform.SetParent(root.transform, false);
            typingNode.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            TextMeshPro typingText = typingNode.AddComponent<TextMeshPro>();
            typingText.fontSize = 3.6f;
            typingText.text = "正在输入中...";
            typingText.alignment = TextAlignmentOptions.Midline;

            GameObject receiptNode = new("ReadReceipt");
            receiptNode.transform.SetParent(root.transform, false);
            receiptNode.transform.localPosition = endAnchor.transform.localPosition;
            CircleCollider2D triggerCollider = receiptNode.AddComponent<CircleCollider2D>();
            triggerCollider.isTrigger = true;
            triggerCollider.radius = 0.35f;
            ReadReceiptTrigger trigger = receiptNode.AddComponent<ReadReceiptTrigger>();

            GameObject unreadNode = new("UnreadVisual");
            unreadNode.transform.SetParent(receiptNode.transform, false);
            TextMeshPro unreadText = unreadNode.AddComponent<TextMeshPro>();
            unreadText.text = "○";
            unreadText.fontSize = 3f;
            unreadText.alignment = TextAlignmentOptions.Center;

            GameObject readNode = new("ReadVisual");
            readNode.transform.SetParent(receiptNode.transform, false);
            TextMeshPro readText = readNode.AddComponent<TextMeshPro>();
            readText.text = "✓";
            readText.fontSize = 3f;
            readText.alignment = TextAlignmentOptions.Center;
            readNode.SetActive(false);

            platform.ConfigureRuntime(
                avatarOnLeft,
                startAnchor.transform,
                endAnchor.transform,
                text,
                typingNode,
                trigger);
            trigger.ConfigureRuntime(unreadNode, readNode);

            return platform;
        }
    }
}
