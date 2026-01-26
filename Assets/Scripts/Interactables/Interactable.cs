using UnityEngine;

// Interface for interactable objects
public interface IInteractable
{
    bool CanInteract { get; }
    
    void Interact(GameObject player);
}