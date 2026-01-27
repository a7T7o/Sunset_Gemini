using UnityEngine;
using UnityEngine.UI;
using FarmGame.Data;
using System;

/// <summary>
/// 物品使用确认弹窗
/// 右键点击消耗品时显示确认对话框
/// </summary>
public class ItemUseConfirmDialog : MonoBehaviour
{
    #region 单例
    
    public static ItemUseConfirmDialog Instance { get; private set; }
    
    #endregion
    
    #region 序列化字段
    
    [Header("UI 引用")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private Text messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Text confirmButtonText;
    
    #endregion
    
    #region 私有字段
    
    private ItemStack _pendingItem;
    private int _pendingSlotIndex;
    private Action<bool> _callback;
    private ItemDatabase _database;
    private InventoryService _inventory;
    
    #endregion
    
    #region Unity 生命周期
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // 绑定按钮事件
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);
        
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);
        
        // 初始隐藏
        if (dialogPanel != null)
            dialogPanel.SetActive(false);
    }
    
    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
    
    void Update()
    {
        // ESC 键关闭弹窗
        if (dialogPanel != null && dialogPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnCancelClicked();
            }
            // Enter 键确认
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                OnConfirmClicked();
            }
        }
    }
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 设置引用
    /// </summary>
    public void SetReferences(ItemDatabase database, InventoryService inventory)
    {
        _database = database;
        _inventory = inventory;
    }
    
    /// <summary>
    /// 显示使用确认弹窗
    /// </summary>
    /// <param name="item">要使用的物品</param>
    /// <param name="slotIndex">槽位索引</param>
    /// <param name="callback">确认回调（true=确认使用，false=取消）</param>
    public void Show(ItemStack item, int slotIndex, Action<bool> callback = null)
    {
        if (item.IsEmpty || _database == null)
        {
            callback?.Invoke(false);
            return;
        }
        
        var itemData = _database.GetItemByID(item.itemId);
        if (itemData == null)
        {
            callback?.Invoke(false);
            return;
        }
        
        _pendingItem = item;
        _pendingSlotIndex = slotIndex;
        _callback = callback;
        
        // 设置消息文本
        if (messageText != null)
        {
            string actionWord = GetActionWord(itemData);
            messageText.text = $"是否确定要{actionWord} {itemData.itemName}？";
        }
        
        // 设置确认按钮文本
        if (confirmButtonText != null)
        {
            confirmButtonText.text = GetActionWord(itemData);
        }
        
        // 显示弹窗
        if (dialogPanel != null)
            dialogPanel.SetActive(true);
    }
    
    /// <summary>
    /// 隐藏弹窗
    /// </summary>
    public void Hide()
    {
        if (dialogPanel != null)
            dialogPanel.SetActive(false);
        
        _pendingItem = ItemStack.Empty;
        _pendingSlotIndex = -1;
        _callback = null;
    }
    
    /// <summary>
    /// 检查物品是否可以使用（消耗品）
    /// </summary>
    public bool CanUseItem(ItemStack item)
    {
        if (item.IsEmpty || _database == null) return false;
        
        var itemData = _database.GetItemByID(item.itemId);
        if (itemData == null) return false;
        
        // 检查是否为消耗品
        return itemData.consumableType != ConsumableType.None;
    }
    
    /// <summary>
    /// 尝试使用物品（显示确认弹窗）
    /// </summary>
    public void TryUseItem(ItemStack item, int slotIndex)
    {
        if (!CanUseItem(item)) return;
        
        Show(item, slotIndex, (confirmed) =>
        {
            if (confirmed)
            {
                UseItem(item, slotIndex);
            }
        });
    }
    
    #endregion
    
    #region 私有方法
    
    /// <summary>
    /// 获取动作词（使用/食用）
    /// </summary>
    private string GetActionWord(ItemData itemData)
    {
        if (itemData == null) return "使用";
        
        return itemData.consumableType switch
        {
            ConsumableType.Food => "食用",
            ConsumableType.Potion => "使用",
            ConsumableType.Buff => "使用",
            _ => "使用"
        };
    }
    
    /// <summary>
    /// 实际使用物品
    /// </summary>
    private void UseItem(ItemStack item, int slotIndex)
    {
        if (_inventory == null || _database == null) return;
        
        var itemData = _database.GetItemByID(item.itemId);
        if (itemData == null) return;
        
        // 执行使用效果
        ApplyItemEffect(itemData, item.quality);
        
        // 减少物品数量
        _inventory.RemoveFromSlot(slotIndex, 1);
        
        Debug.Log($"[ItemUseConfirmDialog] 使用物品: {itemData.itemName}");
    }
    
    /// <summary>
    /// 应用物品效果
    /// </summary>
    private void ApplyItemEffect(ItemData itemData, int quality)
    {
        // 根据物品类型应用效果
        if (itemData is FoodData foodData)
        {
            // 恢复精力和生命值
            // TODO: 连接到玩家状态系统
            Debug.Log($"[ItemUseConfirmDialog] 食物效果: 精力+{foodData.energyRestore}, HP+{foodData.healthRestore}");
        }
        else if (itemData is PotionData potionData)
        {
            // 恢复生命值和精力
            // TODO: 连接到玩家状态系统
            Debug.Log($"[ItemUseConfirmDialog] 药水效果: HP+{potionData.healthRestore}, 精力+{potionData.energyRestore}");
        }
        
        // 触发使用事件（供外部系统订阅）
        OnItemUsed?.Invoke(itemData, quality);
    }
    
    /// <summary>
    /// 确认按钮点击
    /// </summary>
    private void OnConfirmClicked()
    {
        var callback = _callback;
        var item = _pendingItem;
        var slotIndex = _pendingSlotIndex;
        
        Hide();
        
        callback?.Invoke(true);
    }
    
    /// <summary>
    /// 取消按钮点击
    /// </summary>
    private void OnCancelClicked()
    {
        var callback = _callback;
        
        Hide();
        
        callback?.Invoke(false);
    }
    
    #endregion
    
    #region 事件
    
    /// <summary>
    /// 物品使用事件
    /// </summary>
    public static event Action<ItemData, int> OnItemUsed;
    
    #endregion
}
