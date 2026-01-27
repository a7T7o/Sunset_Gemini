# World Prefab 生成与功能设计

版本: V1.1
日期: 2025-12-24
状态: **已完成**

---

## 1. 需求概述

### 1.1 原始需求

用户需求（2025-12-22）：
> 1、简化掉落配置，去掉 DropTable，直接给一个获取 SO 的地方，根据设计好的掉落数量直接生成
> 2、掉落物从树根 collider 范围内平均生成，每个掉落物都选一个坐标，微微分散的轻微堆叠
> 3、砍掉时掉落物轻微从生成位置弹起来
> 4、拾取物品时飘向玩家而不是直接消失
> 5、通过 Ctrl+左键 生成的物品无法被拾取，需要排查修复

后续需求（2025-12-22）：
> 1、旋转不是对 sprite 图片本身，而是预制体的 Sprite 子物体 Z 轴旋转 45 度（保持像素完整）
> 2、整体大小缩小 0.75 左右，参数可调节
> 3、阴影自动计算，不需要复杂参数
> 4、旋转后物体最低点在阴影圆心水平线上方一点
> 5、浮动时阴影呼吸变化

交互需求（2025-12-22）：
> 将工具的选择方式从"即时跟随 Project 选择"改为"手动点击按钮获取选中项"

### 1.2 核心目标

1. **自动化生成** - 从 ItemData 的 icon 自动生成世界物品预制体
2. **视觉效果** - 45度旋转、阴影、浮动动画、阴影呼吸
3. **拾取体验** - 飞向玩家动画、自动进入背包
4. **性能优化** - 对象池、距离优化

---

## 2. 系统架构

### 2.1 整体架构

```
┌─────────────────────────────────────────────────────────────┐
│                    World Item System                         │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────────┐    ┌──────────────────┐               │
│  │ WorldPrefab      │    │ WorldSpawnService │               │
│  │ GeneratorTool    │───▶│ (生成服务)        │               │
│  │ (编辑器工具)     │    └────────┬─────────┘               │
│  └──────────────────┘             │                          │
│                                   ▼                          │
│                         ┌──────────────────┐                 │
│                         │ WorldItemPool    │                 │
│                         │ (对象池)         │                 │
│                         └────────┬─────────┘                 │
│                                  │                           │
│                                  ▼                           │
│  ┌──────────────────┐   ┌──────────────────┐                │
│  │ WorldItemPickup  │◀──│ WorldItemDrop    │                │
│  │ (拾取组件)       │   │ (动画组件)       │                │
│  └────────┬─────────┘   └──────────────────┘                │
│           │                                                  │
│           ▼                                                  │
│  ┌──────────────────┐                                       │
│  │ AutoPickupService│                                       │
│  │ (自动拾取)       │                                       │
│  └──────────────────┘                                       │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 预制体结构

```
WorldItem_{itemId}_{itemName}（根物体）
├── Transform
│   └── localScale = (0.75, 0.75, 0.75)  // 整体缩放
├── Tag = "Pickup"
├── CircleCollider2D
│   ├── isTrigger = true
│   └── radius = 自动计算
├── WorldItemPickup
│   ├── itemId = {itemId}
│   ├── linkedItemData = {ItemData}  // ★ 关键：确保拾取正确
│   ├── quality = 0
│   └── amount = 1
├── WorldItemDrop
│   └── (动画参数)
│
├── Sprite（子物体）
│   ├── Transform
│   │   ├── localPosition = (0, Y, 0)  // Y = 底部偏移
│   │   └── localRotation = (0, 0, 45)  // Z轴旋转45度
│   └── SpriteRenderer
│       ├── sprite = {ItemData.icon}
│       ├── sortingLayerName = "Layer 1"
│       └── sortingOrder = 0
│
└── Shadow（子物体）
    ├── Transform
    │   ├── localPosition = (0, 0, 0)
    │   └── localScale = 自动计算
    └── SpriteRenderer
        ├── sprite = Shadow_Ellipse
        ├── color = (0, 0, 0, 0.35)
        ├── sortingLayerName = "Layer 1"
        └── sortingOrder = -1
```

---

## 3. 核心组件详解

### 3.1 WorldPrefabGeneratorTool（编辑器工具）

**文件路径**: `Assets/Editor/WorldPrefabGeneratorTool.cs`

**功能**:
- 从 ItemData 的 icon 生成世界物品预制体
- Sprite 子物体 Z 轴旋转（保持像素完整）
- 自动计算阴影大小和位置
- 自动添加 Collider 和 Tag
- 自动关联 linkedItemData

**菜单位置**: `Tools → World Item → 批量生成 World Prefab`

**核心参数**:

| 参数 | 默认值 | 说明 |
|------|--------|------|
| prefabsOutputPath | Assets/Prefabs/WorldItems | 输出路径 |
| worldItemScale | 0.75 | 整体缩放 |
| spriteRotationZ | 45° | Sprite Z 轴旋转 |
| shadowBottomOffset | 0.02 | 底部偏移 |
| shadowColor | (0,0,0,0.35) | 阴影颜色 |
| overwriteExisting | false | 是否覆盖已存在文件 |

**位置计算逻辑**:

```csharp
// 1. 计算 Sprite 在世界单位中的尺寸
float spriteWidth = itemSprite.rect.width / itemSprite.pixelsPerUnit;
float spriteHeight = itemSprite.rect.height / itemSprite.pixelsPerUnit;

// 2. 计算旋转后的边界框
float rotRad = spriteRotationZ * Mathf.Deg2Rad;
float cos = Mathf.Abs(Mathf.Cos(rotRad));
float sin = Mathf.Abs(Mathf.Sin(rotRad));
float rotatedWidth = spriteWidth * cos + spriteHeight * sin;
float rotatedHeight = spriteWidth * sin + spriteHeight * cos;

// 3. 计算旋转后物体底部到中心的距离
float bottomY = -rotatedHeight * 0.5f;

// 4. Sprite Y 位置 = -底部距离 + 底部偏移
float spriteY = -bottomY + shadowBottomOffset;
```

**使用流程**:

1. 打开工具窗口
2. 在 Project 窗口选择 ItemData 或文件夹
3. 点击"🔍 获取选中项"按钮
4. 调整参数（可选）
5. 点击"🚀 生成"按钮

---

### 3.2 WorldItemDrop（掉落动画组件）

**文件路径**: `Assets/YYY_Scripts/World/WorldItemDrop.cs`

**功能**:
- 弹性掉落动画（弹出+弹跳）
- 浮动待拾取动画
- 阴影呼吸系统
- 距离优化

**状态机**:

```
┌─────────┐    StartDrop()    ┌──────────┐
│  Idle   │ ─────────────────▶│ Bouncing │
│ (浮动)  │                   │ (弹跳中) │
└────┬────┘                   └────┬─────┘
     │                              │
     │  距离 > 15                   │ 弹跳完成
     ▼                              ▼
┌─────────┐                   ┌──────────┐
│ Paused  │◀──────────────────│  Idle    │
│ (暂停)  │   距离 < 15       │ (浮动)   │
└─────────┘                   └──────────┘
```

**弹跳参数**:

| 参数 | 默认值 | 说明 |
|------|--------|------|
| bounceHeight | 0.8 | 初始弹出高度 |
| bounceDecay | 0.5 | 弹跳衰减系数 |
| maxBounceCount | 3 | 弹跳次数 |
| gravity | 15 | 重力加速度 |

**浮动参数**:

| 参数 | 默认值 | 说明 |
|------|--------|------|
| idleFloatAmplitude | 0.03 | 浮动幅度 |
| idleFloatSpeed | 2.5 | 浮动速度 |

**阴影呼吸参数**:

| 参数 | 默认值 | 说明 |
|------|--------|------|
| shadowMinScaleRatio | 0.85 | 阴影最小缩放（物品最高时） |
| shadowMaxScaleRatio | 1.0 | 阴影最大缩放（物品最低时） |
| shadowMinAlpha | 0.25 | 阴影最小透明度（物品最高时） |
| shadowMaxAlpha | 0.4 | 阴影最大透明度（物品最低时） |

**阴影呼吸逻辑**:

```csharp
// 计算高度比例
float heightRatio = Mathf.Clamp01(Mathf.Abs(_currentHeight) / maxHeight);

// 阴影缩放：物品越高，阴影越小
float scaleRatio = Mathf.Lerp(shadowMaxScaleRatio, shadowMinScaleRatio, heightRatio);
shadowTransform.localScale = _shadowInitialScale * scaleRatio;

// 阴影透明度：物品越高，阴影越淡
float alpha = Mathf.Lerp(shadowMaxAlpha, shadowMinAlpha, heightRatio);
```

---

### 3.3 WorldItemPickup（拾取组件）

**文件路径**: `Assets/YYY_Scripts/World/WorldItemPickup.cs`

**功能**:
- 存储物品数据（itemId, quality, amount）
- 飞向玩家动画
- 自动初始化（从 linkedItemData 或预制体名称）
- 对象池支持

**核心字段**:

| 字段 | 类型 | 说明 |
|------|------|------|
| itemId | int | 物品ID（-1表示未初始化） |
| quality | int | 品质（0-4） |
| amount | int | 数量 |
| linkedItemData | ItemData | 关联的 ItemData（用于自动初始化） |

**飞向玩家动画参数**:

| 参数 | 默认值 | 说明 |
|------|--------|------|
| flyDuration | 0.25s | 飞行时长 |
| flyHeight | 0.3 | 抛物线高度 |

**自动初始化逻辑**:

```csharp
private void EnsureInitialized()
{
    if (_initialized) return;
    
    // 1. 优先使用关联的 ItemData
    if (linkedItemData != null)
    {
        itemId = linkedItemData.itemID;
        _initialized = true;
        return;
    }
    
    // 2. 尝试从预制体名称解析 itemId
    // 预制体命名格式：WorldItem_{itemId}_{itemName}
    if (itemId < 0)
    {
        string objName = gameObject.name;
        if (objName.StartsWith("WorldItem_"))
        {
            string[] parts = objName.Split('_');
            if (parts.Length >= 2 && int.TryParse(parts[1], out int parsedId))
            {
                itemId = parsedId;
                _initialized = true;
            }
        }
    }
}
```

**飞向玩家动画**:

```csharp
private IEnumerator FlyToPlayerCoroutine(Transform player, InventoryService inventory)
{
    Vector3 startPos = transform.position;
    float elapsed = 0f;
    
    while (elapsed < flyDuration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / flyDuration;
        
        // 使用缓动曲线（ease out cubic）
        float easedT = 1f - Mathf.Pow(1f - t, 3f);
        
        // 获取当前目标位置（玩家可能在移动）
        Vector3 targetPos = playerCollider.bounds.center;
        
        // 计算当前位置（带抛物线弧度）
        Vector3 currentPos = Vector3.Lerp(startPos, targetPos, easedT);
        
        // 添加抛物线高度
        float heightT = 4f * t * (1f - t); // 抛物线：0 -> 1 -> 0
        currentPos.y += flyHeight * heightT;
        
        transform.position = currentPos;
        yield return null;
    }
    
    // 动画完成，执行拾取
    TryPickup(inventory);
}
```

---

### 3.4 AutoPickupService（自动拾取服务）

**文件路径**: `Assets/YYY_Scripts/Service/Player/AutoPickupService.cs`

**功能**:
- 检测玩家周围的可拾取物品
- 触发飞向玩家动画
- 基于 Tag 和 Collider 检测

**核心参数**:

| 参数 | 默认值 | 说明 |
|------|--------|------|
| pickupRadius | 1.2 | 拾取半径 |
| pickupTags | ["Pickup"] | 拾取标签 |
| maxPerFrame | 6 | 每帧最大拾取数 |
| enableFlyAnimation | true | 是否启用飞向动画 |

**检测逻辑**:

```csharp
void Update()
{
    // 使用 Player Collider 的中心作为拾取半径的中心点
    Vector2 center = playerCollider.bounds.center;
    var hits = Physics2D.OverlapCircleAll(center, pickupRadius);
    
    foreach (var h in hits)
    {
        // 按标签筛选
        if (!AutoPickupUtil.HasAnyTag(h.transform, pickupTags)) continue;
        
        var pickup = h.GetComponentInParent<WorldItemPickup>();
        if (pickup == null || pickup.IsFlying) continue;
        
        if (enableFlyAnimation)
        {
            pickup.FlyToPlayer(transform, inventory);
        }
        else
        {
            pickup.TryPickup(inventory);
        }
    }
}
```

---

### 3.5 WorldItemPool（对象池）

**文件路径**: `Assets/YYY_Scripts/World/WorldItemPool.cs`

**功能**:
- 对象池管理
- 黄金角度螺旋分布算法
- 数量上限管理

**核心参数**:

| 参数 | 默认值 | 说明 |
|------|--------|------|
| initialPoolSize | 20 | 初始池大小 |
| maxPoolSize | 50 | 最大池大小 |
| maxActiveItems | 100 | 场景中最大活跃物品数量 |
| cleanupBatchSize | 10 | 超出上限时每次清理的数量 |

**黄金角度螺旋分布算法**:

```csharp
private List<Vector3> CalculateScatteredPositions(Vector3 origin, int count, float radius)
{
    var positions = new List<Vector3>();
    
    if (count == 1)
    {
        // 单个物品：中心位置 + 轻微随机偏移
        float offsetX = Random.Range(-radius * 0.3f, radius * 0.3f);
        float offsetY = Random.Range(-radius * 0.3f, radius * 0.3f);
        positions.Add(origin + new Vector3(offsetX, offsetY, 0f));
        return positions;
    }
    
    // 多个物品：使用黄金角度螺旋分布 + 随机偏移
    float goldenAngle = 137.5f * Mathf.Deg2Rad;
    
    for (int i = 0; i < count; i++)
    {
        float t = (float)i / (count - 1);
        float r = radius * Mathf.Sqrt(t) * 0.8f;
        float angle = i * goldenAngle;
        
        float x = r * Mathf.Cos(angle);
        float y = r * Mathf.Sin(angle);
        
        // 添加轻微随机偏移
        float jitter = radius * 0.15f;
        x += Random.Range(-jitter, jitter);
        y += Random.Range(-jitter, jitter);
        
        positions.Add(origin + new Vector3(x, y, 0f));
    }
    
    return positions;
}
```

---

### 3.6 WorldSpawnService（生成服务）

**文件路径**: `Assets/YYY_Scripts/World/WorldSpawnService.cs`

**功能**:
- 统一的物品生成接口
- 支持动画和对象池
- 批量生成

**核心方法**:

| 方法 | 说明 |
|------|------|
| Spawn(ItemStack, Vector3) | 生成物品（无动画） |
| SpawnById(int, int, int, Vector3, bool) | 通过ID生成物品 |
| SpawnFromItem(ItemData, int, int, Vector3, bool) | 通过ItemData生成物品 |
| SpawnWithAnimation(ItemData, int, int, Vector3, Vector3) | 生成物品并播放弹出动画 |
| SpawnMultiple(ItemData, int, int, Vector3, float) | 批量生成多个物品 |

---

## 4. 数据流

### 4.1 预制体生成流程

```
ItemData (icon)
    │
    ▼
WorldPrefabGeneratorTool
    │
    ├── 计算旋转后边界框
    ├── 计算 Sprite Y 位置
    ├── 创建根物体（Tag="Pickup"）
    ├── 添加 CircleCollider2D (Trigger)
    ├── 添加 WorldItemPickup（设置 linkedItemData）
    ├── 添加 WorldItemDrop
    ├── 创建 Sprite 子物体（Z轴旋转45°）
    ├── 创建 Shadow 子物体
    │
    ▼
WorldItem_{itemId}_{itemName}.prefab
    │
    ▼
ItemData.worldPrefab = prefab
```

### 4.2 物品生成流程

```
TreeController.SpawnDrops()
    │
    ▼
WorldSpawnService.SpawnMultiple()
    │
    ▼
WorldItemPool.Spawn()
    │
    ├── 从池中获取实例
    ├── 设置位置
    ├── 调用 Init(data, quality, amount)
    ├── 播放弹出动画
    │
    ▼
WorldItemPickup (活跃状态)
    │
    ▼
WorldItemDrop.StartDrop()
    │
    ├── 弹跳动画
    ├── 浮动动画
    └── 阴影呼吸
```

### 4.3 物品拾取流程

```
AutoPickupService.Update()
    │
    ├── Physics2D.OverlapCircleAll()
    ├── 检测 Tag="Pickup"
    ├── 获取 WorldItemPickup
    │
    ▼
WorldItemPickup.FlyToPlayer()
    │
    ├── 停止掉落动画
    ├── 播放飞向动画（抛物线）
    │
    ▼
WorldItemPickup.TryPickup()
    │
    ├── inventory.AddItem(itemId, quality, amount)
    ├── 停止动画
    ├── WorldItemPool.Despawn()
    │
    ▼
物品进入背包
```

---

## 5. 问题与解决方案

### 5.1 OnValidate SendMessage 错误

**问题**: 在 OnValidate 中调用某些方法会导致 Unity 报错

**解决方案**: 使用 EditorApplication.delayCall 延迟执行

```csharp
#if UNITY_EDITOR
void OnValidate()
{
    UnityEditor.EditorApplication.delayCall += () =>
    {
        if (this == null) return;
        // 延迟执行的代码
    };
}
#endif
```

### 5.2 预制体拖入场景无法拾取

**问题**: 预制体拖入场景后，飞向玩家但未进入背包

**原因**: 预制体拖入场景时未调用 Init() 方法，itemId 保持默认值 -1

**解决方案**:
1. 添加 linkedItemData 字段，在生成预制体时自动关联
2. 添加 EnsureInitialized() 方法，在 Start() 时自动初始化
3. 支持从预制体名称解析 itemId 作为备份方案

### 5.3 像素破坏问题

**问题**: 直接旋转 Sprite 图片会破坏像素

**解决方案**: 改为在 Sprite 子物体的 Transform 上设置 Z 轴旋转，保持原始 Sprite 像素完整

---

## 6. 相关文件清单

### 6.1 核心脚本

| 文件 | 说明 |
|------|------|
| `Assets/Editor/WorldPrefabGeneratorTool.cs` | 预制体生成工具 |
| `Assets/YYY_Scripts/World/WorldItemDrop.cs` | 掉落动画组件 |
| `Assets/YYY_Scripts/World/WorldItemPickup.cs` | 拾取组件 |
| `Assets/YYY_Scripts/World/WorldSpawnService.cs` | 生成服务 |
| `Assets/YYY_Scripts/World/WorldItemPool.cs` | 对象池 |
| `Assets/YYY_Scripts/Service/Player/AutoPickupService.cs` | 自动拾取服务 |
| `Assets/YYY_Scripts/World/WorldSpawnDebug.cs` | 调试脚本 |
| `Assets/Editor/Tool_BatchItemSOGenerator.cs` | 批量生成物品 SO 工具 |

### 6.2 规划文档

| 文件 | 说明 |
|------|------|
| `.kiro/specs/world-item-drop-system/memory.md` | 开发记忆 |
| `.kiro/specs/world-item-drop-system/requirements.md` | 需求文档 |
| `.kiro/specs/world-item-drop-system/design.md` | 设计文档 |
| `.kiro/specs/world-item-drop-system/tasks.md` | 任务清单 |
| `.kiro/specs/item-drop-pickup-system/memory.md` | 关联工作区记忆 |

### 6.3 生成资源

| 文件 | 说明 |
|------|------|
| `Assets/Prefabs/WorldItems/` | 生成的预制体目录 |
| `Assets/Sprites/Generated/Shadow_Ellipse.png` | 默认阴影 Sprite |

---

## 7. 使用指南

### 7.1 生成 WorldPrefab

1. 打开菜单：`Tools → World Item → 批量生成 World Prefab`
2. 在 Project 窗口选择 ItemData 或文件夹
3. 点击"🔍 获取选中项"按钮
4. 调整参数（可选）：
   - 整体缩放（默认 0.75）
   - Sprite Z 轴旋转（默认 45°）
   - 底部偏移（默认 0.02）
5. 点击"🚀 生成"按钮
6. 生成的预制体会自动关联到 ItemData.worldPrefab

### 7.2 使用 WorldPrefab

**方式一：拖入场景**
1. 将生成的预制体拖入场景
2. 物品会自动播放浮动动画
3. 玩家靠近时自动飞向玩家并进入背包

**方式二：代码生成**
```csharp
// 单个物品
var item = WorldSpawnService.Instance.SpawnFromItem(itemData, quality, amount, position, true);

// 批量物品
var items = WorldSpawnService.Instance.SpawnMultiple(itemData, quality, totalAmount, origin, spreadRadius);
```

**方式三：调试工具**
1. 在场景中找到 WorldSpawnDebug 组件
2. 设置要生成的 ItemData
3. Ctrl+左键点击场景生成物品

---

## 8. 背包图标旋转显示（2025-12-24 新增）

### 8.1 概述

背包/工具栏/装备栏中的物品图标现在也使用 45 度旋转显示，与世界物品的视觉风格保持一致。

### 8.2 实现方式

通过 `UIItemIconScaler.SetIconWithAutoScale()` 方法统一处理：
- 添加 `ICON_ROTATION_Z = 45f` 常量
- 计算旋转后边界框尺寸
- 根据旋转后边界框计算缩放比例
- 应用旋转到 RectTransform

### 8.3 核心代码

```csharp
// 计算旋转后的边界框尺寸
float rotRad = ICON_ROTATION_Z * Mathf.Deg2Rad;
float cos = Mathf.Abs(Mathf.Cos(rotRad));
float sin = Mathf.Abs(Mathf.Sin(rotRad));
float rotatedWidthInUnits = spriteWidthInUnits * cos + spriteHeightInUnits * sin;
float rotatedHeightInUnits = spriteWidthInUnits * sin + spriteHeightInUnits * cos;

// 使用旋转后边界框计算缩放比例
float scaleX = displayAreaInUnits / rotatedWidthInUnits;
float scaleY = displayAreaInUnits / rotatedHeightInUnits;
float scale = Mathf.Min(scaleX, scaleY);

// 应用 45 度旋转
rt.localRotation = Quaternion.Euler(0f, 0f, ICON_ROTATION_Z);
```

### 8.4 影响范围

所有使用 `UIItemIconScaler.SetIconWithAutoScale()` 的 UI 组件自动获得旋转效果：
- `InventorySlotUI` - 背包槽位
- `ToolbarSlotUI` - 工具栏槽位
- `EquipmentSlotUI` - 装备栏槽位

### 8.5 bagSprite 字段废弃

- `ItemData.bagSprite` 字段不再使用
- 背包图标直接使用 `icon` + 45° 旋转
- `Tool_BatchItemSOModifier` 新增"清除 bagSprite"选项

---

## 9. 版本历史

| 版本 | 日期 | 说明 |
|------|------|------|
| V1.0 | 2025-12-24 | 初始版本，完成所有核心功能 |
| V1.1 | 2025-12-24 | 新增背包图标 45° 旋转显示功能 |
