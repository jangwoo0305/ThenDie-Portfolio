using Mirror;
using ThenDie.Ingame;
using ThenDie.Player;
using ThenDie.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ThenDie.Interaction
{
    public class PlayerInteractor : NetworkBehaviour
    {
        [SerializeField] private float maxInteractDistance = 2.5f;

        private readonly Dictionary<NetworkIdentity, int> nearbyIdentityOverlapCounts = new();
        private IInteractable nearbyInteractable;
        private NetworkIdentity nearbyIdentity;
        private TaskInteractionPromptUI interactionPromptUI;

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            interactionPromptUI =
                FindFirstObjectByType<TaskInteractionPromptUI>(
                    FindObjectsInactive.Include
                );
        }

        public override void OnStopLocalPlayer()
        {
            ClearNearbyInteractables();
            interactionPromptUI?.Hide();
            base.OnStopLocalPlayer();
        }

        private void Update()
        {
            if (!isLocalPlayer)
                return;

            SelectNearestInteractable();

            if (nearbyInteractable == null || nearbyIdentity == null)
            {
                interactionPromptUI?.Hide();
                return;
            }

            if (!RefreshInteractionPrompt())
                return;

            if (Keyboard.current == null)
                return;

            if (GameplayInputBlocker.ShouldIgnoreGameplayInput())
                return;

            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                CmdInteract(nearbyIdentity);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isLocalPlayer)
                return;

            NetworkIdentity identity = other.GetComponentInParent<NetworkIdentity>();
            IInteractable interactable = identity != null
                ? identity.GetComponent<IInteractable>()
                : null;

            if (interactable == null || identity == null)
                return;

            nearbyIdentityOverlapCounts.TryGetValue(identity, out int overlapCount);
            nearbyIdentityOverlapCounts[identity] = overlapCount + 1;
            SelectNearestInteractable();

            if (nearbyIdentity == identity && RefreshInteractionPrompt())
                Debug.Log("E " + interactable.InteractionText);
        }
        
        
        private void OnTriggerExit2D(Collider2D other)
        {
            if (!isLocalPlayer)
                return;

            NetworkIdentity identity = other.GetComponentInParent<NetworkIdentity>();
            if (identity == null || !nearbyIdentityOverlapCounts.TryGetValue(identity, out int overlapCount))
                return;

            if (overlapCount <= 1)
                nearbyIdentityOverlapCounts.Remove(identity);
            else
                nearbyIdentityOverlapCounts[identity] = overlapCount - 1;

            if (identity == nearbyIdentity)
            {
                nearbyInteractable = null;
                nearbyIdentity = null;
            }

            SelectNearestInteractable();
            if (nearbyInteractable == null)
                interactionPromptUI?.Hide();
        }

        private void SelectNearestInteractable()
        {
            NetworkIdentity nearestIdentity = null;
            IInteractable nearestInteractable = null;
            float nearestDistanceSquared = float.MaxValue;
            List<NetworkIdentity> staleIdentities = null;

            foreach (NetworkIdentity identity in nearbyIdentityOverlapCounts.Keys)
            {
                if (identity == null)
                {
                    staleIdentities ??= new List<NetworkIdentity>();
                    staleIdentities.Add(identity);
                    continue;
                }

                IInteractable interactable = identity.GetComponent<IInteractable>();
                if (interactable == null)
                {
                    staleIdentities ??= new List<NetworkIdentity>();
                    staleIdentities.Add(identity);
                    continue;
                }

                bool shouldConsider = interactable is IInteractionPromptProvider promptProvider
                    ? promptProvider.ShouldShowPrompt(gameObject)
                    : interactable.CanInteract(gameObject);
                if (!shouldConsider)
                    continue;

                float distanceSquared = (identity.transform.position - transform.position).sqrMagnitude;
                if (distanceSquared >= nearestDistanceSquared)
                    continue;

                nearestDistanceSquared = distanceSquared;
                nearestIdentity = identity;
                nearestInteractable = interactable;
            }

            if (staleIdentities != null)
            {
                foreach (NetworkIdentity staleIdentity in staleIdentities)
                    nearbyIdentityOverlapCounts.Remove(staleIdentity);
            }

            nearbyIdentity = nearestIdentity;
            nearbyInteractable = nearestInteractable;
        }

        private void ClearNearbyInteractables()
        {
            nearbyIdentityOverlapCounts.Clear();
            nearbyIdentity = null;
            nearbyInteractable = null;
        }

        private bool RefreshInteractionPrompt()
        {
            if (nearbyInteractable is IInteractionPromptProvider promptProvider)
            {
                if (!promptProvider.ShouldShowPrompt(gameObject))
                {
                    interactionPromptUI?.Hide();
                    return false;
                }

                interactionPromptUI?.Show(
                    promptProvider.GetPromptText(gameObject),
                    promptProvider.ShouldShowInteractionKey(gameObject)
                );
                return true;
            }

            if (!nearbyInteractable.CanInteract(gameObject))
            {
                interactionPromptUI?.Hide();
                return false;
            }

            interactionPromptUI?.Show(nearbyInteractable.InteractionText);
            return true;
        }
        
        [Command]
        private void CmdInteract(NetworkIdentity targetIdentity)
        {
            if (targetIdentity == null)
                return;

            PlayerState playerState = GetComponent<PlayerState>();

            if (playerState == null || !playerState.isAlive)
                return;

            if (GameFlowManager.Instance == null || GameFlowManager.Instance.phase != GamePhase.FreeRoam)
            {
                return;
            }

            float distance = Vector3.Distance(
                transform.position,
                targetIdentity.transform.position
            );

            if (distance > maxInteractDistance)
                return;

            IInteractable interactable = targetIdentity.GetComponent<IInteractable>();
            if (interactable == null)
                return;

            if (!interactable.CanInteract(gameObject))
                return;

            interactable.Interact(gameObject);
        }
    }
}
