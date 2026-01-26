using UnityEngine;
using UnityEngine.UI;

public class Treasure : MonoBehaviour, IInteractable
{
    [SerializeField] private string treasureName = "Unknown Treasure";
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private Image treasureUIIcon;
    [SerializeField] private string description = "";
    
    public bool CanInteract => true;

    public void Interact(GameObject player)
    {
        
        Debug.Log($"Player {player.name} collected treasure: {treasureName}");
        
        // 添加到 GameManager 的收集记录
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddTreasure(treasureName);
        }
        
        // 播放收集音效
        if (collectSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(collectSound);
        }
        
        // 激活UI图标
        if (treasureUIIcon != null)
        {
            treasureUIIcon.gameObject.SetActive(true);
        }

        UIManager.Instance?.ShowDialogue(description);
        
        // 销毁宝藏对象
        Destroy(gameObject);
    }
}
