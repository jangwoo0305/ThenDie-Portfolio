using UnityEngine;

namespace ThenDie.Interaction
{
    public interface IInteractable
    {
        string InteractionText { get; }

        bool CanInteract(GameObject player);

        void Interact(GameObject player);
    }

    public interface IInteractionPromptProvider
    {
        bool ShouldShowPrompt(GameObject player);

        string GetPromptText(GameObject player);

        bool ShouldShowInteractionKey(GameObject player);
    }
}

