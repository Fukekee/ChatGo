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
        [SerializeField] private DialogueLine[] fallbackLines;
        [Tooltip("HandPlaced 模式下可为空")]
        [SerializeField] private BubblePool bubblePool;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private ReplyPanel replyPanel;
        [SerializeField] private CameraFollow cameraFollow;

        [Header("平台模式")]
        [SerializeField] private ConversationPlatformMode platformMode = ConversationPlatformMode.RuntimeBubblePool;
        [Tooltip("HandPlaced 模式下用于复用显示的 Bubble 平台。建议至少 3 个，按上/中/下顺序放入。")]
        [SerializeField] private BubblePlatform[] handPlacedPlatforms;
        [Tooltip("HandPlaced 模式最多同时显示几条消息。")]
        [SerializeField] private int handPlacedMaxVisible = 3;
        [Tooltip("可选：手动指定上/中/下槽位。为空时使用 handPlacedPlatforms 前 N 个初始位置。")]
        [SerializeField] private Transform[] handPlacedSlots;
        [Tooltip("HandPlaced 模式每次更新时的平台上移动画时长。0 表示瞬移。")]
        [SerializeField] private float handPlacedSlideDuration = 0.2f;
        [Tooltip("是否让相机跟随当前气泡（固定机位请取消勾选）")]
        [SerializeField] private bool followActiveBubbleWithCamera = true;

        [Header("过渡")]
        [Tooltip("触发 ReadReceipt 后，延迟多久再生成下一个平台并传送玩家（秒）。期间玩家无法移动。")]
        [SerializeField] private float nextLineDelay = 0.7f;

        [Header("生成参数（仅 RuntimeBubblePool）")]
        [SerializeField] private Vector3 firstBubblePosition = Vector3.zero;
        [SerializeField] private float verticalSpacing = -3f;
        [Tooltip("对方气泡相对 firstBubblePosition 的 X 偏移（左侧为负）")]
        [SerializeField] private float opponentBubbleX = -4f;
        [Tooltip("我方气泡相对 firstBubblePosition 的 X 偏移（右侧为正）")]
        [SerializeField] private float playerBubbleX = 4f;

        private int currentLineIndex = -1;
        private BubblePlatform currentBubble;
        private BubblePlatform pendingReplyBubble;
        private DialogueLine[] runtimeLines;
        private readonly List<BubblePlatform> activeHandPlacedBubbles = new();
        private readonly Queue<BubblePlatform> idleHandPlacedBubbles = new();
        private readonly List<Vector3> handPlacedSlotPositions = new();
        private Coroutine handPlacedSlideRoutine;
        private Coroutine nextLineDelayRoutine;

        private void Start()
        {
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

            runtimeLines = ResolveRuntimeLines();

            if (platformMode == ConversationPlatformMode.HandPlacedInScene)
            {
                if (!ValidateHandPlacedSetup())
                {
                    enabled = false;
                    return;
                }

                PrepareHandPlacedPlatformsInactive();
            }

            SpawnNextLine();
        }

        private void OnDestroy()
        {
            UnsubscribeCurrentBubble();
        }

        private void SpawnNextLine()
        {
            DialogueLine nextLine = GetNextLine();
            if (nextLine == null)
            {
                return;
            }

            BubblePlatform nextBubble = null;

            if (platformMode == ConversationPlatformMode.HandPlacedInScene)
            {
                nextBubble = SpawnHandPlacedLine(nextLine);
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

                float xOffset = nextLine.speaker == SpeakerSide.Opponent ? opponentBubbleX : playerBubbleX;
                Vector3 spawnPosition = new Vector3(
                    firstBubblePosition.x + xOffset,
                    firstBubblePosition.y + verticalSpacing * currentLineIndex,
                    firstBubblePosition.z);
                nextBubble = bubblePool.SpawnLine(nextLine, spawnPosition);
                if (nextBubble == null)
                {
                    return;
                }
            }

            UnsubscribeCurrentBubble();
            currentBubble = nextBubble;
            // 玩家回复行：须先选 ReplyPanel，再订阅读回执；否则传送后与触发器重迭会立刻推进对话。
            if (nextLine.speaker != SpeakerSide.Player)
            {
                SubscribeReadReceiptForCurrentBubble();
            }

            Vector3 playerSpawnPoint = currentBubble.GetStartWorldPosition();
            playerController?.TeleportTo(playerSpawnPoint);

            if (cameraFollow != null && followActiveBubbleWithCamera)
            {
                cameraFollow.SetFollowTarget(currentBubble.transform);
            }

            if (nextLine.speaker == SpeakerSide.Player)
            {
                pendingReplyBubble = currentBubble;
                if (playerController != null)
                {
                    playerController.CanMove = false;
                }

                if (replyPanel != null)
                {
                    replyPanel.Show(nextLine.replyOption1, nextLine.replyOption2, OnReplySelected);
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

        private DialogueLine GetNextLine()
        {
            if (runtimeLines == null || runtimeLines.Length == 0)
            {
                return null;
            }

            currentLineIndex += 1;
            if (currentLineIndex < 0 || currentLineIndex >= runtimeLines.Length)
            {
                return null;
            }

            return runtimeLines[currentLineIndex];
        }

        private void OnBubbleReadTriggered(ReadReceiptTrigger trigger)
        {
            if (trigger == null || trigger.OwnerPlatform != currentBubble)
            {
                return;
            }

            ScheduleNextLine();
        }

        private void ScheduleNextLine()
        {
            if (nextLineDelayRoutine != null)
            {
                StopCoroutine(nextLineDelayRoutine);
            }

            nextLineDelayRoutine = StartCoroutine(DelayedSpawnNextLine());
        }

        private IEnumerator DelayedSpawnNextLine()
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
            SpawnNextLine();
        }

        private void OnReplySelected(string selectedReply)
        {
            BubblePlatform bubble = pendingReplyBubble;
            pendingReplyBubble = null;

            if (bubble != null)
            {
                bubble.SetReplyText(selectedReply);
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

        private DialogueLine[] ResolveRuntimeLines()
        {
            if (conversationData != null && conversationData.lines != null && conversationData.lines.Length > 0)
            {
                return conversationData.lines;
            }

            if (fallbackLines != null && fallbackLines.Length > 0)
            {
                return fallbackLines;
            }

            return new[]
            {
                new DialogueLine
                {
                    speaker = SpeakerSide.Opponent,
                    text = "现在项目很忙啊"
                },
                new DialogueLine
                {
                    speaker = SpeakerSide.Player,
                    replyOption1 = "但是我的工作很早就完成了",
                    replyOption2 = "我可以远程处理，保证不影响进度"
                },
                new DialogueLine
                {
                    speaker = SpeakerSide.Opponent,
                    text = "你先把交接方案发我"
                }
            };
        }

        private bool ValidateHandPlacedSetup()
        {
            if (runtimeLines == null || runtimeLines.Length == 0)
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
                    BubblePlatform slotBubble = handPlacedPlatforms[i];
                    if (slotBubble == null)
                    {
                        Debug.LogError($"ConversationManager: handPlacedPlatforms[{i}] 为空。");
                        continue;
                    }

                    handPlacedSlotPositions.Add(slotBubble.transform.position);
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

        private BubblePlatform SpawnHandPlacedLine(DialogueLine lineData)
        {
            if (lineData == null || handPlacedSlotPositions.Count == 0)
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
            bubbleToUse.Init(lineData);
            activeHandPlacedBubbles.Add(bubbleToUse);

            // 方案 A：先把最新平台放到最终槽位，再传送玩家；其余平台随后做上移动画。
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
                // 前 3 条按上->中->下依次填充；满屏后每次上移一格，最新落在最下槽位。
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
