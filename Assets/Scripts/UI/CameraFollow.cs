using UnityEngine;

namespace ChatGo.UI
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform followTarget;
        [SerializeField] private Vector3 offset = new(0f, 0f, -10f);
        [SerializeField] private float smoothTime = 0.2f;

        [Header("聊天左右分栏：不跟 X，避免气泡被拉到屏幕正中")]
        [Tooltip("勾选则相机 X 跟随目标；不勾选则只用 Fixed World X，仅纵向跟随气泡")]
        [SerializeField] private bool followTargetWorldX = false;
        [Tooltip("不跟随目标 X 时，相机使用的世界坐标 X（可与 ConversationManager 的 firstBubblePosition.x 对齐）")]
        [SerializeField] private float fixedWorldX = 0f;

        private Vector3 velocity;

        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
            velocity = Vector3.zero;
        }

        /// <summary>运行时设置固定相机 X（例如与对话区域中心一致）。</summary>
        public void SetFixedWorldX(float worldX)
        {
            fixedWorldX = worldX;
        }

        private void LateUpdate()
        {
            if (followTarget == null)
            {
                return;
            }

            Vector3 target = followTarget.position + offset;
            Vector3 desired = followTargetWorldX
                ? target
                : new Vector3(fixedWorldX, target.y, target.z);

            Vector3 smoothed = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
            transform.position = smoothed;
        }
    }
}
