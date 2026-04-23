using System;
using ChatGo.Bubble;
using ChatGo.Data;
using ChatGo.Opponent;
using ChatGo.Player;
using ChatGo.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChatGo.Conversation
{
    public enum ConversationPlatformMode
    {
        /// <summary>由 BubblePool 按对话行在世界中生成气泡（原逻辑）。</summary>
        RuntimeBubblePool = 0,
        /// <summary>在场景中按顺序摆放 BubblePlatform，与对话行一一对应，不改动其 Transform。</summary>
        HandPlacedInScene = 1
    }

    public class ConversationManager : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private ConversationData conversationData;
        [SerializeField] private DialogueNode[] fallbackNodes;
        [Tooltip("HandPlaced 模式下可为空")]
        [SerializeField] private BubblePool bubblePool;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private ReplyPanel replyPanel;
        [SerializeField] private CameraFollow cameraFollow;

        [Header("平台模式")]
        [SerializeField] private ConversationPlatformMode platformMode = ConversationPlatformMode.RuntimeBubblePool;
        [Tooltip("HandPlaced 模式下复用显示的 Bubble 平台对象池。运行时位置由槽位决定，所以这些物体在场景里摆在哪都行。")]
        [SerializeField] private BubblePlatform[] handPlacedPlatforms;
        [Tooltip("HandPlaced 模式最多同时显示几条消息。")]
        [SerializeField] private int handPlacedMaxVisible = 3;
        [Tooltip("可选：手动指定 N 个槽位 Transform。为空时使用 firstBubblePosition + verticalSpacing 自动生成等距单列槽位。")]
        [SerializeField] private Transform[] handPlacedSlots;
        [Tooltip("HandPlaced 模式每次更新时的平台上移动画时长。0 表示瞬移。")]
        [SerializeField] private float handPlacedSlideDuration = 0.2f;
        [Tooltip("是否让相机跟随当前气泡（固定机位请取消勾选）")]
        [SerializeField] private bool followActiveBubbleWithCamera = true;

        [Header("过渡")]
        [Tooltip("触发 ReadReceipt 后，延迟多久再生成下一个平台并传送玩家（秒）。期间玩家无法移动。")]
        [SerializeField] private float nextLineDelay = 0.7f;

        [Header("默认锚点 / 行间距")]
        [Tooltip("RuntimeBubblePool 的基准位置；HandPlaced 模式下若 handPlacedSlots 为空，也作为 fallback 槽位的列锚点。")]
        [SerializeField] private Vector3 firstBubblePosition = Vector3.zero;
        [Tooltip("相邻槽位 / 气泡的 Y 间距。负值表示往下排列。")]
        [SerializeField] private float verticalSpacing = -3f;
        [Header("生成参数（仅 RuntimeBubblePool）")]
        [Tooltip("对方气泡相对 firstBubblePosition 的 X 偏移（左侧为负）")]
        [SerializeField] private float opponentBubbleX = -4f;
        [Tooltip("我方气泡相对 firstBubblePosition 的 X 偏移（右侧为正）")]
        [SerializeField] private float playerBubbleX = 4f;

        public event Action ConversationCompleted;

        private Dictionary<string, DialogueNode> nodeMap;
        private string currentNodeId;
        private string pendingTargetNodeId;
        private int spawnCount;
        private BubblePlatform currentBubble;
        private BubblePlatform pendingReplyBubble;
        private readonly List<BubblePlatform> activeHandPlacedBubbles = new();
        private readonly Queue<BubblePlatform> idleHandPlacedBubbles = new();
        private readonly List<Vector3> handPlacedSlotPositions = new();
        private Coroutine handPlacedSlideRoutine;
        private Coroutine nextLineDelayRoutine;

        private void Start()
        {
            ResolveSceneReferences();

            OpponentHealth.Instance?.ResetToMax();
            PlayerHealth.Instance?.ResetToMax();

            if (replyPanel != null)
            {
                replyPanel.Hide();
            }

            if (cameraFollow != null)
            {
                cameraFollow.SetFixedWorldX(firstBubblePosition.x);
                if (!followActiveBubbleWithCamera)
                {
                    cameraFollow.SetFollowTarget(null);
                }
            }

            BuildNodeMap();

            if (nodeMap.Count == 0)
            {
                Debug.LogError("ConversationManager: 没有对话节点。");
                enabled = false;
                return;
            }

            if (platformMode == ConversationPlatformMode.HandPlacedInScene)
            {
                if (!ValidateHandPlacedSetup())
                {
                    enabled = false;
                    return;
                }

                PrepareHandPlacedPlatformsInactive();
            }

            SpawnNode(ResolveStartNodeId());
        }

        private void OnDestroy()
        {
            UnsubscribeCurrentBubble();
        }

        private void ResolveSceneReferences()
        {
            if (bubblePool == null)
            {
                bubblePool = FindFirstObjectByType<BubblePool>();
            }

            if (playerController == null)
            {
                playerController = FindFirstObjectByType<PlayerController>();
            }

            if (replyPanel == null)
            {
                replyPanel = FindFirstObjectByType<ReplyPanel>();
            }

            if (cameraFollow == null)
            {
                cameraFollow = FindFirstObjectByType<CameraFollow>();
            }
        }

        private void BuildNodeMap()
        {
            nodeMap = new Dictionary<string, DialogueNode>();
            DialogueNode[] nodes = ResolveNodes();
            foreach (DialogueNode node in nodes)
            {
                if (string.IsNullOrEmpty(node.nodeId))
                {
                    continue;
                }

                if (nodeMap.ContainsKey(node.nodeId))
                {
                    Debug.LogWarning($"ConversationManager: 重复的节点 ID \"{node.nodeId}\"，后者会覆盖前者。");
                }

                nodeMap[node.nodeId] = node;
            }
        }

        private DialogueNode[] ResolveNodes()
        {
            if (conversationData != null && conversationData.nodes != null && conversationData.nodes.Length > 0)
            {
                return conversationData.nodes;
            }

            if (fallbackNodes != null && fallbackNodes.Length > 0)
            {
                return fallbackNodes;
            }

            return new[]
            {
                new DialogueNode
                {
                    nodeId = "1",
                    speaker = SpeakerSide.Opponent,
                    text = "现在项目很忙啊",
                    nextNodeId = "2"
                },
                new DialogueNode
                {
                    nodeId = "2",
                    speaker = SpeakerSide.Player,
                    choices = new[]
                    {
                        new DialogueChoice { choiceText = "但是我的工作很早就完成了", targetNodeId = "3a" },
                        new DialogueChoice { choiceText = "我可以远程处理，保证不影响进度", targetNodeId = "3b" }
                    }
                },
                new DialogueNode
                {
                    nodeId = "3a",
                    speaker = SpeakerSide.Opponent,
                    text = "那你把交接方案发我"
                },
                new DialogueNode
                {
                    nodeId = "3b",
                    speaker = SpeakerSide.Opponent,
                    text = "行，那你保持在线随时沟通"
                }
            };
        }

        private string ResolveStartNodeId()
        {
            if (conversationData != null && !string.IsNullOrEmpty(conversationData.startNodeId))
            {
                return conversationData.startNodeId;
            }

            DialogueNode[] nodes = ResolveNodes();
            return nodes.Length > 0 ? nodes[0].nodeId : null;
        }

        private DialogueNode GetNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                return null;
            }

            return nodeMap.TryGetValue(nodeId, out DialogueNode node) ? node : null;
        }

        private void SpawnNode(string nodeId)
        {
            DialogueNode node = GetNode(nodeId);
            if (node == null)
            {
                return;
            }

            currentNodeId = nodeId;
            pendingTargetNodeId = null;

            BubblePlatform nextBubble = null;

            if (platformMode == ConversationPlatformMode.HandPlacedInScene)
            {
                nextBubble = SpawnHandPlacedLine(node);
                if (nextBubble == null)
                {
                    return;
                }
            }
            else
            {
                if (bubblePool == null)
                {
                    return;
                }

                float xOffset = node.speaker == SpeakerSide.Opponent ? opponentBubbleX : playerBubbleX;
                Vector3 spawnPosition = new Vector3(
                    firstBubblePosition.x + xOffset,
                    firstBubblePosition.y + verticalSpacing * spawnCount,
                    firstBubblePosition.z);
                nextBubble = bubblePool.SpawnLine(node, spawnPosition);
                if (nextBubble == null)
                {
                    return;
                }
            }

            spawnCount++;

            UnsubscribeCurrentBubble();
            currentBubble = nextBubble;

            if (node.speaker != SpeakerSide.Player)
            {
                SubscribeReadReceiptForCurrentBubble();
            }

            Vector3 playerSpawnPoint = currentBubble.GetStartWorldPosition();
            playerController?.TeleportTo(playerSpawnPoint);

            if (cameraFollow != null && followActiveBubbleWithCamera)
            {
                cameraFollow.SetFollowTarget(currentBubble.transform);
            }

            if (node.speaker == SpeakerSide.Player)
            {
                pendingReplyBubble = currentBubble;
                if (playerController != null)
                {
                    playerController.CanMove = false;
                }

                if (replyPanel != null)
                {
                    replyPanel.Show(node.choices, OnChoiceSelected);
                }
            }
            else
            {
                pendingReplyBubble = null;
                if (replyPanel != null)
                {
                    replyPanel.Hide();
                }

                if (playerController != null)
                {
                    PlayerHealth health = playerController.GetComponent<PlayerHealth>();
                    playerController.CanMove = health == null || !health.IsDead;
                }
            }
        }

        private void OnBubbleReadTriggered(ReadReceiptTrigger trigger)
        {
            if (trigger == null || trigger.OwnerPlatform != currentBubble)
            {
                return;
            }

            string nextId;
            if (!string.IsNullOrEmpty(pendingTargetNodeId))
            {
                nextId = pendingTargetNodeId;
                pendingTargetNodeId = null;
            }
            else
            {
                DialogueNode current = GetNode(currentNodeId);
                nextId = current?.nextNodeId;
            }

            if (string.IsNullOrEmpty(nextId))
            {
                ConversationCompleted?.Invoke();
                return;
            }

            ScheduleNextNode(nextId);
        }

        private void ScheduleNextNode(string nextNodeId)
        {
            if (nextLineDelayRoutine != null)
            {
                StopCoroutine(nextLineDelayRoutine);
            }

            nextLineDelayRoutine = StartCoroutine(DelayedSpawnNode(nextNodeId));
        }

        private IEnumerator DelayedSpawnNode(string nextNodeId)
        {
            if (playerController != null)
            {
                playerController.CanMove = false;
            }

            float delay = Mathf.Max(0f, nextLineDelay);
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            nextLineDelayRoutine = null;
            SpawnNode(nextNodeId);
        }

        private void OnChoiceSelected(DialogueChoice choice)
        {
            BubblePlatform bubble = pendingReplyBubble;
            pendingReplyBubble = null;

            pendingTargetNodeId = choice.targetNodeId;

            if (bubble != null)
            {
                bubble.SetReplyText(choice.choiceText);
                bubble.ApplyHazardFromChoice(choice);
                SubscribeReadReceiptForBubble(bubble);
            }

            if (playerController != null)
            {
                PlayerHealth health = playerController.GetComponent<PlayerHealth>();
                playerController.CanMove = health == null || !health.IsDead;
            }
        }

        private void SubscribeReadReceiptForCurrentBubble()
        {
            SubscribeReadReceiptForBubble(currentBubble);
        }

        private void SubscribeReadReceiptForBubble(BubblePlatform bubble)
        {
            if (bubble == null || bubble.ReadReceiptTrigger == null)
            {
                return;
            }

            bubble.ReadReceiptTrigger.Triggered -= OnBubbleReadTriggered;
            bubble.ReadReceiptTrigger.Triggered += OnBubbleReadTriggered;
        }

        private void UnsubscribeCurrentBubble()
        {
            if (currentBubble != null && currentBubble.ReadReceiptTrigger != null)
            {
                currentBubble.ReadReceiptTrigger.Triggered -= OnBubbleReadTriggered;
            }
        }

        private bool ValidateHandPlacedSetup()
        {
            if (nodeMap == null || nodeMap.Count == 0)
            {
                Debug.LogError("ConversationManager: 没有对话数据。");
                return false;
            }

            if (handPlacedPlatforms == null || handPlacedPlatforms.Length == 0)
            {
                Debug.LogError("ConversationManager: HandPlaced 模式需要配置 handPlacedPlatforms。");
                return false;
            }

            int maxVisible = Mathf.Max(1, handPlacedMaxVisible);
            if (handPlacedPlatforms.Length < maxVisible)
            {
                Debug.LogError(
                    $"ConversationManager: handPlacedPlatforms 数量 ({handPlacedPlatforms.Length}) 少于最大可见条数 ({maxVisible})。");
                return false;
            }

            return true;
        }

        private void PrepareHandPlacedPlatformsInactive()
        {
            activeHandPlacedBubbles.Clear();
            idleHandPlacedBubbles.Clear();
            handPlacedSlotPositions.Clear();

            int slotCount = Mathf.Min(Mathf.Max(1, handPlacedMaxVisible), handPlacedPlatforms.Length);
            for (int i = 0; i < slotCount; i++)
            {
                if (handPlacedSlots != null && i < handPlacedSlots.Length && handPlacedSlots[i] != null)
                {
                    handPlacedSlotPositions.Add(handPlacedSlots[i].position);
                }
                else
                {
                    // Fallback：用 firstBubblePosition 作列锚点 + verticalSpacing 等距叠出 N 个槽位。
                    // 不再读 handPlacedPlatforms[i] 的初始位置，避免它们的 X / Y 被无意中固化进槽位。
                    Vector3 fallback = new Vector3(
                        firstBubblePosition.x,
                        firstBubblePosition.y + verticalSpacing * i,
                        firstBubblePosition.z);
                    handPlacedSlotPositions.Add(fallback);
                }
            }

            for (int i = 0; i < handPlacedPlatforms.Length; i++)
            {
                if (handPlacedPlatforms[i] != null)
                {
                    handPlacedPlatforms[i].gameObject.SetActive(false);
                    idleHandPlacedBubbles.Enqueue(handPlacedPlatforms[i]);
                }
            }
        }

        private BubblePlatform SpawnHandPlacedLine(DialogueNode nodeData)
        {
            if (nodeData == null || handPlacedSlotPositions.Count == 0)
            {
                return null;
            }

            int maxVisible = Mathf.Min(Mathf.Max(1, handPlacedMaxVisible), handPlacedSlotPositions.Count);

            BubblePlatform bubbleToUse = null;
            if (idleHandPlacedBubbles.Count > 0)
            {
                bubbleToUse = idleHandPlacedBubbles.Dequeue();
            }
            else if (activeHandPlacedBubbles.Count >= maxVisible)
            {
                bubbleToUse = activeHandPlacedBubbles[0];
                activeHandPlacedBubbles.RemoveAt(0);
            }

            if (activeHandPlacedBubbles.Count >= maxVisible)
            {
                BubblePlatform oldest = activeHandPlacedBubbles[0];
                activeHandPlacedBubbles.RemoveAt(0);
                if (oldest != null && oldest != bubbleToUse)
                {
                    oldest.gameObject.SetActive(false);
                    idleHandPlacedBubbles.Enqueue(oldest);
                }
            }

            if (bubbleToUse == null)
            {
                Debug.LogError("ConversationManager: 没有可用的 HandPlaced 平台实例。");
                return null;
            }

            bubbleToUse.gameObject.SetActive(true);
            bubbleToUse.Init(nodeData);
            activeHandPlacedBubbles.Add(bubbleToUse);

            int targetSlotForLatest = Mathf.Clamp(activeHandPlacedBubbles.Count - 1, 0, handPlacedSlotPositions.Count - 1);
            bubbleToUse.transform.position = handPlacedSlotPositions[targetSlotForLatest];

            if (handPlacedSlideRoutine != null)
            {
                StopCoroutine(handPlacedSlideRoutine);
            }

            handPlacedSlideRoutine = StartCoroutine(AnimateHandPlacedLayout());
            return bubbleToUse;
        }

        private IEnumerator AnimateHandPlacedLayout()
        {
            int count = activeHandPlacedBubbles.Count;
            if (count <= 0)
            {
                yield break;
            }

            int slotCount = handPlacedSlotPositions.Count;
            var from = new Vector3[count];
            var to = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                BubblePlatform bubble = activeHandPlacedBubbles[i];
                from[i] = bubble.transform.position;
                int targetSlot = Mathf.Clamp(i, 0, slotCount - 1);
                to[i] = handPlacedSlotPositions[targetSlot];
            }

            float duration = Mathf.Max(0f, handPlacedSlideDuration);
            if (duration <= 0f)
            {
                for (int i = 0; i < count; i++)
                {
                    activeHandPlacedBubbles[i].transform.position = to[i];
                }

                handPlacedSlideRoutine = null;
                yield break;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float normalized = Mathf.Clamp01(t / duration);
                float eased = Mathf.SmoothStep(0f, 1f, normalized);
                for (int i = 0; i < count; i++)
                {
                    activeHandPlacedBubbles[i].transform.position = Vector3.LerpUnclamped(from[i], to[i], eased);
                }

                yield return null;
            }

            for (int i = 0; i < count; i++)
            {
                activeHandPlacedBubbles[i].transform.position = to[i];
            }

            handPlacedSlideRoutine = null;
        }
    }
}
