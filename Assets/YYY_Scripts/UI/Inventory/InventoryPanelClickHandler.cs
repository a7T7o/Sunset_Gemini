using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 背包面板点击处理器
/// 用于检测在非槽位区域的点击，触发丢弃或返回原位
/// 挂载在 Main、Top 和垃圾桶区域上
/// </summary>
public class InventoryPanelClickHandler : MonoBehaviour, IPointerClickHandler, IDropHandler
{
    [SerializeField] private bool isDropZone = false;  // 是否是丢弃区域（如垃圾桶）
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        
        var manager = InventoryInteractionManager.Instance;
        if (manager == null || !manager.IsHolding) return;
        
        Debug.Log($"<color=cyan>[PanelClickHandler] OnPointerClick - isDropZone={isDropZone}, pos={eventData.position}</color>");
        
        // ★ 使用新的 HandleHeldClickOutside 方法处理 Held 状态下的点击
        manager.HandleHeldClickOutside(eventData.position, isDropZone);
    }
    
    /// <summary>
    /// 处理拖拽放置（拖拽到垃圾桶）
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        if (!isDropZone) return;
        
        var manager = InventoryInteractionManager.Instance;
        if (manager == null || !manager.IsHolding) return;
        
        Debug.Log("<color=yellow>[PanelClickHandler] OnDrop - 拖拽到垃圾桶</color>");
        
        // 通知 Manager 这是垃圾桶区域
        manager.OnSlotDrop(-1, false);  // 使用 -1 表示垃圾桶
    }
}
