using Mirror;
using ThenDie.Interaction;
using UnityEngine;

namespace ThenDie.Ingame
{
    public class OwlMailMailbox
        : NetworkBehaviour, IInteractable, IInteractionPromptProvider
    {
        public string InteractionText => "편지 전달";

        public bool CanInteract(GameObject player)
        {
            if (SabotageManager.Instance == null ||
                !SabotageManager.Instance.IsOwlMailRunning)
                return false;

            OwlMailPlayerCarry carry = player.GetComponent<OwlMailPlayerCarry>();
            return carry != null && carry.CarryLetterCount > 0;
        }

        public bool ShouldShowPrompt(GameObject player)
        {
            return SabotageManager.Instance != null &&
                   SabotageManager.Instance.IsOwlMailRunning;
        }

        public string GetPromptText(GameObject player)
        {
            return HasLetters(player)
                ? InteractionText
                : "전달할 편지가 없습니다.";
        }

        public bool ShouldShowInteractionKey(GameObject player)
        {
            return HasLetters(player);
        }

        [Server]
        public void Interact(GameObject player)
        {
            if (SabotageManager.Instance == null || !SabotageManager.Instance.CanPlayerInteractWithOwlMail(player))
            {
                return;
            }
            if (!CanInteract(player))
                return;

            OwlMailPlayerCarry carry = player.GetComponent<OwlMailPlayerCarry>();
            int letterCount = carry.TakeAllLetters();

            SabotageManager.Instance.ServerDeliverLetters(letterCount);
        }

        private static bool HasLetters(GameObject player)
        {
            OwlMailPlayerCarry carry = player.GetComponent<OwlMailPlayerCarry>();
            return carry != null && carry.CarryLetterCount > 0;
        }
    }
}
