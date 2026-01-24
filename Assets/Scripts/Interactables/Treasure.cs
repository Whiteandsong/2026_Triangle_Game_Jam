using UnityEngine;

public class Treasure : MonoBehaviour, IInteractable
{
    [SerializeField] private string treasureName = "Unknown Treasure";

    public void Interact(GameObject player)
    {
        Debug.Log($"Player {player.name} collected treasure: {treasureName}");
        //TODO: Add treasure to player's inventory or increase score
        Destroy(gameObject);
    }
}
