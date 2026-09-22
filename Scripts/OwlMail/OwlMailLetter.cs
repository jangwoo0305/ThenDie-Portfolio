using Mirror;
using UnityEngine;
using ThenDie.Interaction;

namespace ThenDie.Ingame
{
    public class OwlMailLetter : NetworkBehaviour, IInteractable
    {
        [SyncVar]
        private bool isCollected;

        public string InteractionText => "편지 줍기";

        public bool CanInteract(GameObject player)
        {
            if (isCollected)
                return false;

            if (SabotageManager.Instance == null ||
                !SabotageManager.Instance.IsOwlMailRunning)
                return false;

            OwlMailPlayerCarry carry = player.GetComponent<OwlMailPlayerCarry>();
            return carry != null &&
                   carry.CanAddLetter(SabotageManager.Instance.MaxCarryCount);
        }

        [Server]
        public void Interact(GameObject player)
        {
            if (SabotageManager.Instance == null || !SabotageManager.Instance.CanPlayerInteractWithOwlMail(player))
            {
                return;
            }

            OwlMailPlayerCarry carry = player.GetComponent<OwlMailPlayerCarry>();
            if (carry == null)
                return;

            if (!carry.CanAddLetter(SabotageManager.Instance.MaxCarryCount))
                return;

            if (isCollected)
                return;

            isCollected = true;
            carry.AddLetter();
            NetworkServer.Destroy(gameObject);
        }
    }
}
