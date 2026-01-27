using UnityEngine;
using UnityEngine.UI;
using FarmGame.Data;

/// <summary>
/// 材料项 UI 组件
/// 显示单个材料的需求和持有状态
/// 
/// **Feature: ui-system**
/// **Validates: Requirements 2.2, 2.3**
/// </summary>
public class MaterialItemUI : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Text nameText;
    [SerializeField] private Text amountText;  // 格式: "持有/需要"
    [SerializeField] private Image statusIcon; // 绿勾/红叉（可选）
    
    [Header("颜色")]
    [SerializeField] private Color sufficientColor = new Color(0.3f, 0.9f, 0.3f);
    [SerializeField] private Color insufficientColor = new Color(0.9f, 0.3f, 0.3f);
    
    /// <summary>
    /// 设置材料项显示
    /// </summary>
    public void Setup(int itemId, int required, int owned, ItemDatabase database)
    {
        // 获取物品数据
        var itemData = database?.GetItemByID(itemId);
        
        // 设置图标
        if (iconImage != null)
        {
            iconImage.sprite = itemData?.icon;
            iconImage.enabled = itemData?.icon != null;
        }
        
        // 设置名称
        if (nameText != null)
        {
            nameText.text = itemData?.itemName ?? $"物品#{itemId}";
        }
        
        // 设置数量和颜色
        bool sufficient = owned >= required;
        
        if (amountText != null)
        {
            amountText.text = $"{owned}/{required}";
            amountText.color = sufficient ? sufficientColor : insufficientColor;
        }
        
        // 设置状态图标（可选）
        if (statusIcon != null)
        {
            statusIcon.color = sufficient ? sufficientColor : insufficientColor;
        }
    }
    
    /// <summary>
    /// 使用 MaterialStatus 设置
    /// </summary>
    public void Setup(MaterialStatus status)
    {
        if (iconImage != null)
        {
            iconImage.sprite = status.icon;
            iconImage.enabled = status.icon != null;
        }
        
        if (nameText != null)
        {
            nameText.text = status.itemName;
        }
        
        if (amountText != null)
        {
            amountText.text = $"{status.owned}/{status.required}";
            amountText.color = status.sufficient ? sufficientColor : insufficientColor;
        }
        
        if (statusIcon != null)
        {
            statusIcon.color = status.sufficient ? sufficientColor : insufficientColor;
        }
    }
}
