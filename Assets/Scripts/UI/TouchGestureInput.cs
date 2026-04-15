using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using ChatGo.Player;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace ChatGo.UI
{
    /// <summary>
    /// 触控手势输入：单指滑动控制左右移动，点击屏幕跳跃。
    /// 支持双指操作——拖动移动的同时点击第二根手指跳跃。
    /// </summary>
    public class TouchGestureInput : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;

        [Header("手势参数")]
        [SerializeField, Tooltip("点击判定最大时长（秒）")]
        private float tapMaxDuration = 0.3f;

        [SerializeField, Tooltip("水平拖动触发移动的最小像素距离")]
        private float swipeMoveThreshold = 30f;

        [SerializeField, Tooltip("是否忽略落在 UI 元素上的触摸")]
        private bool ignoreUITouches = true;

        private int moveFingerId = -1;
        private Vector2 moveStartPos;
        private float moveStartTime;
        private bool hasSwiped;

        public void Bind(PlayerController player)
        {
            playerController = player;
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
            ClearMoveState();
        }

        private void Update()
        {
            var touches = Touch.activeTouches;
            bool moveFingerStillAlive = false;

            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                int id = touch.finger.index;

                if (touch.phase == TouchPhase.Began && ignoreUITouches && IsOverUI(touch))
                    continue;

                // ── 主手指：控制移动 / 短按跳跃 ──
                if (id == moveFingerId)
                {
                    switch (touch.phase)
                    {
                        case TouchPhase.Moved:
                        case TouchPhase.Stationary:
                            ApplyMovement(touch);
                            moveFingerStillAlive = true;
                            break;
                        case TouchPhase.Ended:
                        case TouchPhase.Canceled:
                            OnPrimaryFingerUp();
                            break;
                    }
                    continue;
                }

                // ── 尚无主手指 → 采纳当前手指 ──
                if (moveFingerId < 0 && touch.phase == TouchPhase.Began)
                {
                    moveFingerId = id;
                    moveStartPos = touch.screenPosition;
                    moveStartTime = Time.unscaledTime;
                    hasSwiped = false;
                    moveFingerStillAlive = true;
                    continue;
                }

                // ── 副手指点击 → 跳跃（可在滑动移动时同时跳跃） ──
                if (touch.phase == TouchPhase.Ended)
                {
                    float dur = (float)(touch.time - touch.startTime);
                    if (dur <= tapMaxDuration)
                        playerController?.ForceJump();
                }
            }

            if (!moveFingerStillAlive && moveFingerId >= 0)
                ClearMoveState();
        }

        private void ApplyMovement(Touch touch)
        {
            float deltaX = touch.screenPosition.x - moveStartPos.x;
            if (Mathf.Abs(deltaX) >= swipeMoveThreshold)
            {
                hasSwiped = true;
                playerController?.SetMoveInputFromUI(Mathf.Sign(deltaX));
            }
            else
            {
                playerController?.SetMoveInputFromUI(0f);
            }
        }

        private void OnPrimaryFingerUp()
        {
            if (!hasSwiped)
            {
                float duration = Time.unscaledTime - moveStartTime;
                if (duration <= tapMaxDuration)
                    playerController?.ForceJump();
            }
            ClearMoveState();
        }

        private void ClearMoveState()
        {
            playerController?.SetMoveInputFromUI(0f);
            moveFingerId = -1;
            hasSwiped = false;
        }

        private static bool IsOverUI(Touch touch)
        {
            return EventSystem.current != null
                && EventSystem.current.IsPointerOverGameObject(touch.touchId);
        }
    }
}
