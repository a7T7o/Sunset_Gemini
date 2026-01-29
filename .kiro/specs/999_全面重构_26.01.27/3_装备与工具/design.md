# 装备与工具系统设计文档

**创建日期**: 2026-01-29
**更新日期**: 2026-01-29
**任务代号**: Operation Arsenal (军火库行动) - Execute Phase
**决策状态**: 🔒 文档已锁定 → 🔥 代码执行中

---

## 一、EquipmentData 数据类设计（新建）

### 1.1 继承关系

```csharp
public class EquipmentData : ItemData
```

**设计原则**：继承 ItemData，不修改基类，保护现有 SO 资产。

### 1.2 新增字段

```csharp
[CreateAssetMenu(fileName = "Equipment_", menuName = "Game/Items/EquipmentData")]
public class EquipmentData : ItemData
{
    [Header("=== 装备专属属性 ===")]
    
    [Tooltip("装备类型（覆盖基类的通用定义）")]
    public EquipmentType equipmentType = EquipmentType.None;
    
    [Tooltip("防御力")]
    public int defense = 0;
    
    [Tooltip("属性加成（预留给力量/敏捷等）")]
    public List<StatModifier> attributes = new List<StatModifier>();
    
    [Tooltip("装备模型（预留给纸娃娃系统）")]
    public GameObject equipmentModel;
}
```

### 1.3 StatModifier 结构（预留）

```csharp
[Serializable]
public class StatModifier
{
    public StatType statType;   // 属性类型（力量、敏捷、智力等）
    public float value;         // 加成数值
    public ModifierType type;   // 加成类型（固定值、百分比）
}

public enum StatType
{
    Strength,   // 力量
    Agility,    // 敏捷
    Intelligence, // 智力
    Vitality,   // 体力
    Luck        // 幸运
}

public enum ModifierType
{
    Flat,       // 固定值加成
    Percent     // 百分比加成
}
```

### 1.4 文件位置

```
Assets/YYY_Scripts/Data/Items/EquipmentData.cs
```

---

## 二、EquipmentService 重构设计

### 2.1 当前状态

```csharp
public class EquipmentService : MonoBehaviour
{
    [SerializeField] private ItemStack[] equips = new ItemStack[EquipSlots];
    // 没有实现 IPersistentObject
    // 存档后数据丢失
}
```

### 2.2 目标状态

```csharp
public class EquipmentService : MonoBehaviour, IPersistentObject
{
    public const int EquipSlots = 6;
    
    [SerializeField] private InventoryItem[] equips = new InventoryItem[EquipSlots];
    
    // 实现 IPersistentObject
    public string PersistentId => "EquipmentService";
    
    public object CaptureState()
    {
        var data = new EquipmentSaveData();
        for (int i = 0; i < equips.Length; i++)
        {
            data.slots[i] = equips[i]?.ToSaveData();
        }
        return data;
    }
    
    public void RestoreState(object state)
    {
        if (state is EquipmentSaveData data)
        {
            for (int i = 0; i < equips.Length; i++)
            {
                equips[i] = InventoryItem.FromSaveData(data.slots[i], database);
            }
        }
    }
}
```

### 2.3 存档数据结构

```csharp
[Serializable]
public class EquipmentSaveData
{
    public InventoryItemSaveData[] slots = new InventoryItemSaveData[6];
}
```

### 2.4 槽位限制逻辑（P0 核心）

在 `EquipItem` 方法中必须检查 `item.Data.equipmentType` 是否匹配目标槽位：

| 槽位索引 | 允许的 EquipmentType |
|---------|---------------------|
| 0 | Helmet |
| 1 | Pants |
| 2 | Armor |
| 3 | Shoes |
| 4 | Ring |
| 5 | Ring |

```csharp
public bool CanEquipAt(int slotIndex, ItemData itemData)
{
    if (itemData == null) return false;
    
    // 优先检查 EquipmentData 子类
    EquipmentType eqType = EquipmentType.None;
    if (itemData is EquipmentData eqData)
    {
        eqType = eqData.equipmentType;
    }
    else
    {
        eqType = itemData.equipmentType;
    }
    
    return slotIndex switch
    {
        0 => eqType == EquipmentType.Helmet,
        1 => eqType == EquipmentType.Pants,
        2 => eqType == EquipmentType.Armor,
        3 => eqType == EquipmentType.Shoes,
        4 or 5 => eqType == EquipmentType.Ring,
        _ => false
    };
}

public bool EquipItem(int slotIndex, InventoryItem item)
{
    // 槽位限制检查
    if (!CanEquipAt(slotIndex, item?.Data))
    {
        Debug.LogWarning($"[EquipmentService] 无法装备: {item?.Data?.itemName} 不能放入槽位 {slotIndex}");
        return false;
    }
    
    equips[slotIndex] = item;
    OnEquipmentChanged?.Invoke(slotIndex);
    return true;
}
```

---

## 三、Tool_BatchItemSOGenerator 扩展设计

### 3.1 新增装备小类

在 `ItemSOType` 枚举中新增：

```csharp
public enum ItemSOType
{
    // 现有类型...
    ToolData,
    WeaponData,
    KeyData,
    LockData,
    
    // 新增
    EquipmentData  // 装备
}
```

### 3.2 映射更新

```csharp
// CategoryToSubTypes 映射
{ ItemMainCategory.ToolsAndEquipment, new[] { 
    ItemSOType.ToolData, 
    ItemSOType.WeaponData, 
    ItemSOType.KeyData, 
    ItemSOType.LockData,
    ItemSOType.EquipmentData  // 新增
}}

// SubTypeNames 映射
{ ItemSOType.EquipmentData, "装备" }

// SubTypeStartIDs 映射
{ ItemSOType.EquipmentData, 8000 }

// SubTypeOutputFolders 映射
{ ItemSOType.EquipmentData, "Assets/111_Data/Items/Equipment" }
```

### 3.3 装备专属设置面板

```csharp
private EquipmentType selectedEquipmentType = EquipmentType.Helmet;

private void DrawEquipmentSettings()
{
    EditorGUILayout.LabelField("🛡️ 装备专属设置", EditorStyles.boldLabel);
    
    selectedEquipmentType = (EquipmentType)EditorGUILayout.EnumPopup("装备类型", selectedEquipmentType);
    
    // 根据装备类型显示推荐 ID 范围和输出路径
    string idHint = selectedEquipmentType switch
    {
        EquipmentType.Helmet => "头盔 - 推荐 ID: 8000-8099 - 输出: Equipment/Helmets",
        EquipmentType.Armor => "盔甲 - 推荐 ID: 8100-8199 - 输出: Equipment/Armors",
        EquipmentType.Pants => "裤子 - 推荐 ID: 8200-8299 - 输出: Equipment/Pants",
        EquipmentType.Shoes => "鞋子 - 推荐 ID: 8300-8399 - 输出: Equipment/Shoes",
        EquipmentType.Ring => "戒指 - 推荐 ID: 8400-8499 - 输出: Equipment/Rings",
        EquipmentType.Accessory => "饰品 - 推荐 ID: 8500-8599 - 输出: Equipment/Accessories",
        _ => ""
    };
    EditorGUILayout.HelpBox(idHint, MessageType.Info);
}
```

### 3.4 ID 范围规划

| 装备类型 | ID 范围 | 输出路径 |
|---------|--------|---------|
| Helmet | 8000-8099 | Assets/111_Data/Items/Equipment/Helmets |
| Armor | 8100-8199 | Assets/111_Data/Items/Equipment/Armors |
| Pants | 8200-8299 | Assets/111_Data/Items/Equipment/Pants |
| Shoes | 8300-8399 | Assets/111_Data/Items/Equipment/Shoes |
| Ring | 8400-8499 | Assets/111_Data/Items/Equipment/Rings |
| Accessory | 8500-8599 | Assets/111_Data/Items/Equipment/Accessories |

### 3.5 生成逻辑（自动设置 equipmentType）

```csharp
private ItemData CreateEquipmentData(Sprite sprite, int itemID, string itemName)
{
    var data = ScriptableObject.CreateInstance<EquipmentData>();
    SetCommonProperties(data, sprite, itemID, itemName, ItemCategory.Special);
    
    // 装备专属设置
    data.maxStackSize = 1;  // 装备不可堆叠
    data.equipmentType = selectedEquipmentType;  // 自动设置，策划不需要手动选
    data.defense = 0;  // 默认防御力，策划后续手动调整
    
    return data;
}
```

---

## 四、兼容性考虑

### 4.1 ItemStack → InventoryItem 迁移

`EquipmentService` 从 `ItemStack[]` 迁移到 `InventoryItem[]` 需要：

1. 修改内部数据结构
2. 修改 `GetEquip` / `SetEquip` 方法的返回/参数类型
3. 更新 `EquipmentSlotUI` 的绑定逻辑
4. 更新 `InventoryInteractionManager` 中的装备交互逻辑

### 4.2 向后兼容

- 旧存档没有装备数据，读档时装备栏为空（可接受）
- 新存档包含装备数据，旧版本读取时忽略（需要版本检查）

### 4.3 EquipmentData vs ItemData 兼容

- 现有使用 `ItemData.equipmentType` 的代码继续工作
- 新装备使用 `EquipmentData` 子类，获得额外字段
- `CanEquipAt` 方法同时支持两种类型

---

## 五、风险评估

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| ItemStack → InventoryItem 迁移可能影响现有逻辑 | 中 | 逐步迁移，充分测试 |
| 装备槽位 UI 需要同步更新 | 低 | 复用现有 Refresh 逻辑 |
| 存档格式变化 | 低 | 添加版本检查 |
| EquipmentData 子类与现有代码兼容性 | 低 | 继承 ItemData，多态兼容 |
