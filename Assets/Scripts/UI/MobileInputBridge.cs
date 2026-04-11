using ChatGo.Player;
using UnityEngine;

namespace ChatGo.UI
{
    public class MobileInputBridge : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;

        public void Bind(PlayerController player)
        {
            playerController = player;
        }

        public void HoldLeft()
        {
            playerController?.SetMoveInputFromUI(-1f);
        }

        public void HoldRight()
        {
            playerController?.SetMoveInputFromUI(1f);
        }

        public void ReleaseHorizontal()
        {
            playerController?.SetMoveInputFromUI(0f);
        }

        public void Jump()
        {
            playerController?.ForceJump();
        }
    }
}
