using System.Collections;
using UnityEngine;

namespace ChatGo.Hazards
{
    [RequireComponent(typeof(Collider2D))]
    public class PiranhaPlantHazard : MonoBehaviour
    {
        [Header("位移（相对父物体 local Y）")]
        [SerializeField] private float hiddenY = -1.1f;
        [SerializeField] private float shownY = 0f;
        [SerializeField] private float moveSpeed = 2.4f;

        [Header("节奏")]
        [SerializeField] private float hideDuration = 1.0f;
        [SerializeField] private float showDuration = 1.2f;
        [Tooltip("确保“顶部停留时间 > 升降时间”的最小冗余秒数")]
        [SerializeField] private float showExtraMargin = 0.1f;

        [Header("伤害")]
        [SerializeField] private int damage = 10;
        [SerializeField] private float damageCooldown = 0.3f;

        private float nextHitTime;
        private Coroutine loopRoutine;

        private void Awake()
        {
            Collider2D col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        private void OnEnable()
        {
            SetLocalY(hiddenY);

            if (loopRoutine != null)
            {
                StopCoroutine(loopRoutine);
            }

            loopRoutine = StartCoroutine(LoopRoutine());
        }

        private void OnDisable()
        {
            if (loopRoutine != null)
            {
                StopCoroutine(loopRoutine);
                loopRoutine = null;
            }
        }

        private IEnumerator LoopRoutine()
        {
            while (true)
            {
                if (hideDuration > 0f)
                {
                    yield return new WaitForSeconds(hideDuration);
                }

                yield return MoveToLocalY(shownY);

                float oneWayMoveTime = Mathf.Abs(shownY - hiddenY) / Mathf.Max(0.01f, moveSpeed);
                float effectiveShowDuration = Mathf.Max(showDuration, oneWayMoveTime + Mathf.Max(0f, showExtraMargin));
                if (effectiveShowDuration > 0f)
                {
                    yield return new WaitForSeconds(effectiveShowDuration);
                }

                yield return MoveToLocalY(hiddenY);
            }
        }

        private IEnumerator MoveToLocalY(float targetY)
        {
            while (!Mathf.Approximately(transform.localPosition.y, targetY))
            {
                float newY = Mathf.MoveTowards(transform.localPosition.y, targetY, moveSpeed * Time.deltaTime);
                SetLocalY(newY);
                yield return null;
            }

            SetLocalY(targetY);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryDamage(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryDamage(other);
        }

        private void TryDamage(Collider2D other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            if (Time.time < nextHitTime)
            {
                return;
            }

            other.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            nextHitTime = Time.time + Mathf.Max(0.01f, damageCooldown);
        }

        private void SetLocalY(float y)
        {
            Vector3 p = transform.localPosition;
            p.y = y;
            transform.localPosition = p;
        }
    }
}
