using System;
using System.Collections;
using ChatGo.Player;
using UnityEngine;

namespace ChatGo.Bubble
{
    [RequireComponent(typeof(Collider2D))]
    public class ReadReceiptTrigger : MonoBehaviour
    {
        [SerializeField] private GameObject unreadVisual;
        [SerializeField] private GameObject readVisual;
        [SerializeField] private float completeDelay = 0.3f;

        private BubblePlatform ownerPlatform;
        private bool consumed;

        public event Action<ReadReceiptTrigger> Triggered;
        public BubblePlatform OwnerPlatform => ownerPlatform;

        public void ConfigureRuntime(GameObject unreadVisualObject, GameObject readVisualObject)
        {
            unreadVisual = unreadVisualObject;
            readVisual = readVisualObject;
        }

        public void BindPlatform(BubblePlatform platform)
        {
            ownerPlatform = platform;
        }

        public void ResetTriggerState()
        {
            consumed = false;

            if (unreadVisual != null)
            {
                unreadVisual.SetActive(true);
            }

            if (readVisual != null)
            {
                readVisual.SetActive(false);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (consumed)
            {
                return;
            }

            PlayerController player = other.GetComponent<PlayerController>();
            if (player == null)
            {
                return;
            }

            consumed = true;
            if (unreadVisual != null)
            {
                unreadVisual.SetActive(false);
            }

            if (readVisual != null)
            {
                readVisual.SetActive(true);
            }

            StartCoroutine(NotifyAfterDelay());
        }

        private IEnumerator NotifyAfterDelay()
        {
            if (completeDelay > 0f)
            {
                yield return new WaitForSeconds(completeDelay);
            }

            Triggered?.Invoke(this);
        }
    }
}
