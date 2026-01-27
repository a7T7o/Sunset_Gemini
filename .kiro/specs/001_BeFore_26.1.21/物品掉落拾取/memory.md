# 物品掉落与拾取系统 - 开发记忆

## 模块概述
优化物品掉落与拾取系统，包括简化掉落配置、优化掉落物分散显示、添加拾取飞向玩家动画、修复物品无法被拾取的问题。

## 当前状态
- **完成度**: 100%
- **最后更新**: 2025-12-24
- **状态**: 已完成

## 会话记录

### 会话 7 - 2025-12-24

**用户需求**:
> 当背包满了就不会再吸引周边的物体了，你没做好这个逻辑。现在要的应该是吸引动画触发也就是进入距离的时候，就该计数并检测背包是否满了，如果满了就不触发后续逻辑，物品就会待在原地不会被吸引

**问题分析**:
- `AutoPickupService.Update()` 检测到物品进入拾取范围后，直接调用 `FlyToPlayer()` 触发飞向动画
- 飞向动画完成后才调用 `TryPickup()` 尝试添加到背包
- **没有在触发吸引动画前检查背包是否有空间**
- 导致背包满时，物品仍然会被吸引飞向玩家，然后因为背包满而无法拾取

**解决方案**:
1. 在 `InventoryService` 中添加 `CanAddItem()` 方法
   - 检查背包是否有空间容纳指定物品（不实际添加）
   - 按照与 `AddItem()` 相同的优先级检查：第一行堆叠 → 第一行空位 → 其他行堆叠 → 其他行空位
2. 在 `AutoPickupService` 中，触发 `FlyToPlayer()` 前先调用 `CanAddItem()` 检查
   - 背包满时跳过该物品，物品保持原位不被吸引
   - 背包有空间时正常触发飞向动画

**修改文件**:
- `Assets/YYY_Scripts/Service/Inventory/InventoryService.cs` - 添加 CanAddItem 方法
- `Assets/YYY_Scripts/Service/Player/AutoPickupService.cs` - 添加背包空间检查

**遗留问题**:
- [x] 所有功能已完成

---

### 会话 6 - 2025-12-24

**用户需求**:
> 1、简化掉落配置，去掉 DropTable，直接给一个获取 SO 的地方，根据设计好的掉落数量直接生成
> 2、掉落物从树根 collider 范围内平均生成，每个掉落物都选一个坐标，微微分散的轻微堆叠
> 3、砍掉时掉落物轻微从生成位置弹起来
> 4、拾取物品时飘向玩家而不是直接消失
> 5、通过 Ctrl+左键 生成的物品无法被拾取，需要排查修复

**完成任务**:
1. 修复 WorldPrefab 拾取问题
   - WorldPrefabGeneratorTool 自动添加 CircleCollider2D (Trigger)
   - WorldPrefabGeneratorTool 自动设置 Tag 为 "Pickup"
   - WorldItemPool 运行时创建的物体也添加 Collider 和 Tag
   - WorldSpawnService 运行时创建的物体也添加 Collider 和 Tag
2. 简化 TreeControllerV2 掉落配置
   - 添加 dropItemData (ItemData) 字段
   - 添加 stageDropAmounts[6] 数组
   - 添加 stumpDropAmounts[6] 数组
   - 添加 dropSpreadRadius 字段
   - 修改 SpawnDrops 和 SpawnStumpDrops 方法使用新配置
   - 更新 TreeControllerV2Editor 添加掉落配置 UI
3. 优化掉落物分散生成
   - 使用黄金角度螺旋分布算法
   - 每个物品单独生成（不堆叠）
   - 添加轻微随机偏移让分布更自然
4. 添加拾取飞向玩家动画
   - WorldItemPickup 添加 FlyToPlayer 方法
   - 使用抛物线弧度动画
   - AutoPickupService 触发飞向动画

**修改文件**:
- `Assets/Editor/WorldPrefabGeneratorTool.cs` - 添加 Collider 和 Tag
- `Assets/YYY_Scripts/World/WorldItemPool.cs` - 添加 Collider 和 Tag，优化分散算法
- `Assets/YYY_Scripts/World/WorldSpawnService.cs` - 添加 Collider 和 Tag，优化分散算法
- `Assets/YYY_Scripts/World/WorldItemPickup.cs` - 添加飞向玩家动画
- `Assets/YYY_Scripts/Service/Player/AutoPickupService.cs` - 触发飞向动画
- `Assets/YYY_Scripts/Controller/TreeControllerV2.cs` - 简化掉落配置
- `Assets/Editor/TreeControllerV2Editor.cs` - 添加掉落配置 UI

**遗留问题**:
- [x] 所有代码实现已完成
- [ ] 用户需要使用 WorldPrefabGeneratorTool 重新生成 WorldPrefab

---

### 会话 2 - 2025-12-22

**用户需求**:
> 1、旋转不是对 sprite 图片本身，而是预制体的 Sprite 子物体 Z 轴旋转 45 度（保持像素完整）
> 2、整体大小缩小 0.75 左右，参数可调节
> 3、阴影自动计算，不需要复杂参数
> 4、旋转后物体最低点在阴影圆心水平线上方一点
> 5、浮动时阴影呼吸变化

**完成任务**:
1. 重写 WorldPrefabGeneratorTool
   - 移除生成旋转 Sprite 图片的功能
   - 改为在 Sprite 子物体上设置 Z 轴旋转（保持像素完整）
   - 添加整体缩放参数（默认 0.75）
   - 简化阴影配置，自动计算大小和位置
   - 计算旋转后物体底部位置，确保略高于阴影中心
2. 重写 WorldItemDrop 阴影呼吸系统
   - 阴影随浮动缩放变化（物品高时阴影小）
   - 阴影随浮动透明度变化（物品高时阴影淡）
   - 记录初始 Sprite 位置，正确处理浮动偏移
3. 修复 OnValidate 中的 SendMessage 错误
   - 使用 EditorApplication.delayCall 延迟执行

**修改文件**:
- `Assets/Editor/WorldPrefabGeneratorTool.cs` - 完全重写
- `Assets/YYY_Scripts/World/WorldItemDrop.cs` - 重写阴影呼吸系统

**解决方案**:

### 预制体结构
```
WorldItem_XXX（根物体，整体缩放 0.75）
├── Sprite（Z 轴旋转 45°，位置略高于阴影）
│   └── SpriteRenderer（原始 Sprite，像素完整）
└── Shadow（位置 Y=0）
    └── SpriteRenderer（阴影，自动计算大小）
```

### 位置计算
1. 计算旋转后的边界框尺寸
2. 计算旋转后物体底部到中心的距离
3. Sprite Y 位置 = -底部距离 + 底部偏移（确保底部略高于阴影中心）

### 阴影呼吸
- 缩放比例：0.85 ~ 1.0（物品高时小）
- 透明度：0.25 ~ 0.4（物品高时淡）

**遗留问题**:
- [x] 用户测试新的预制体生成效果

---

### 会话 3 - 2025-12-22

**用户需求**:
> 将工具的选择方式从"即时跟随 Project 选择"改为"手动点击按钮获取选中项"，类似 001批量工具 的交互方式

**完成任务**:
1. 修改 WorldPrefabGeneratorTool 交互方式
   - 移除 `Selection.selectionChanged` 事件监听
   - 添加"🔍 获取选中项"按钮
   - 设置保存到 EditorPrefs，下次打开时恢复
2. 修改 Tool_BatchItemSOGenerator 交互方式
   - 移除 `Selection.selectionChanged` 事件监听
   - 将 `RefreshSelectedSprites()` 改为 `GetSelectedSprites()`
   - 添加"🔍 获取选中项"按钮
   - 设置保存到 EditorPrefs，下次打开时恢复

**修改文件**:
- `Assets/Editor/WorldPrefabGeneratorTool.cs` - 修改交互方式
- `Assets/Editor/Tool_BatchItemSOGenerator.cs` - 修改交互方式

**解决方案**:
参考 `Assets/Editor/Tool_001_BatchProject.cs` 的交互模式，用户需要手动点击按钮确认选择，而不是自动跟随 Project 窗口的选择变化。这样更符合用户的操作习惯，避免误操作。

**遗留问题**:
- [x] 所有功能已完成

---

### 会话 4 - 2025-12-24

**用户需求**:
> 预制体拖入场景后飞向玩家但未进入背包，需要排查修复

**问题分析**:
- 预制体拖入场景时未调用 `Init()` 方法
- `itemId` 保持默认值 `-1`，导致 `inventory.AddItem()` 失败

**解决方案**:
1. 添加 `linkedItemData` 字段，在生成预制体时自动关联
2. 添加 `EnsureInitialized()` 方法，在 `Start()` 时自动初始化
3. 支持从预制体名称解析 `itemId` 作为备份方案

**修改文件**:
- `Assets/YYY_Scripts/World/WorldItemPickup.cs` - 添加自动初始化逻辑
- `Assets/Editor/WorldPrefabGeneratorTool.cs` - 设置 linkedItemData

**遗留问题**:
- [x] 所有功能已完成
- [ ] 用户需要重新生成 WorldPrefab 以应用修复

---

### 会话 5 - 2025-12-24

**用户需求**:
> 给工具加一个可选项，更新所有 database 内的 items，填写文件夹路径即可

**完成任务**:
1. 添加"从文件夹批量生成"模式
   - 添加 `useBatchMode` 开关
   - 添加 `batchFolderPath` 文件夹路径输入
   - 添加"预览文件夹内容"按钮
   - 递归搜索所有子文件夹的 ItemData

**修改文件**:
- `Assets/Editor/WorldPrefabGeneratorTool.cs` - 添加批量模式

**使用方法**:
1. 勾选"📂 从文件夹批量生成"
2. 输入或选择 ItemData 所在的文件夹路径
3. 点击"🔍 预览文件夹内容"查看将要处理的物品
4. 点击"🚀 生成"按钮

**遗留问题**:
- [x] 所有功能已完成

---

### 会话 6 - 2025-12-24

**用户需求**:
> 背包的SO的展示形式其实是用item sprite旋转45度，也是z轴旋转，你没有做这个，我不会生成bagsprite给你，是你自己转换而来的

**完成任务**:
1. 修改 UIItemIconScaler.SetIconWithAutoScale() 添加 45 度旋转
   - 添加 ICON_ROTATION_Z = 45f 常量
   - 计算旋转后边界框尺寸
   - 应用旋转到 RectTransform
2. 验证所有 UI 组件自动获得旋转效果
   - InventorySlotUI ✅
   - ToolbarSlotUI ✅
   - EquipmentSlotUI ✅
3. 在 Tool_BatchItemSOModifier 添加清除 bagSprite 选项

**修改文件**:
- `Assets/YYY_Scripts/UI/Utility/UIItemIconScaler.cs` - 添加旋转功能
- `Assets/Editor/Tool_BatchItemSOModifier.cs` - 添加清除 bagSprite 选项

**遗留问题**:
- [x] 所有功能已完成

---

## 关键决策

| 决策 | 原因 | 日期 |
|------|------|------|
| 移除 DropTable | 用户已统一设计好掉落数量，不需要复杂的概率配置 | 2025-12-22 |
| 使用黄金角度螺旋分布 | 比纯随机分布更均匀，视觉效果更好 | 2025-12-22 |
| 每个物品单独生成 | 让玩家能直观看到掉落数量 | 2025-12-22 |
| 添加飞向玩家动画 | 提升拾取体验 | 2025-12-22 |
| 自动添加 Collider 和 Tag | 确保物品可被拾取 | 2025-12-22 |
| 手动获取选中项交互 | 避免自动跟随选择导致误操作 | 2025-12-22 |
| linkedItemData 自动关联 | 确保预制体拖入场景后能正确拾取 | 2025-12-24 |
| 预制体名称解析 itemId | 作为备份初始化方案 | 2025-12-24 |
| 从文件夹批量生成模式 | 方便一次性更新所有物品的 WorldPrefab | 2025-12-24 |
| 吸引前检查背包空间 | 背包满时物品不被吸引，保持原位 | 2025-12-24 |

## 相关文件

| 文件 | 说明 |
|------|------|
| `Assets/YYY_Scripts/Controller/TreeControllerV2.cs` | 树木控制器 |
| `Assets/YYY_Scripts/World/WorldSpawnService.cs` | 物品生成服务 |
| `Assets/YYY_Scripts/World/WorldItemPickup.cs` | 物品拾取组件 |
| `Assets/YYY_Scripts/World/WorldItemDrop.cs` | 掉落动画组件 |
| `Assets/YYY_Scripts/World/WorldItemPool.cs` | 对象池 |
| `Assets/YYY_Scripts/Service/Player/AutoPickupService.cs` | 自动拾取服务 |
| `Assets/YYY_Scripts/Service/Inventory/InventoryService.cs` | 背包服务 |
| `Assets/Editor/WorldPrefabGeneratorTool.cs` | WorldPrefab 生成工具 |
| `Assets/Editor/TreeControllerV2Editor.cs` | 树木控制器编辑器 |
| `Assets/Editor/Tool_BatchItemSOGenerator.cs` | 批量生成物品 SO 工具 |

## 用户操作指南

### 生成 WorldPrefab

**方式一：手动选择**
1. 打开菜单：`Tools → World Item → 批量生成 World Prefab`
2. 在 Project 窗口选择 ItemData 或文件夹
3. 点击"🔍 获取选中项"按钮
4. 点击"🚀 生成"按钮

**方式二：从文件夹批量生成**
1. 打开菜单：`Tools → World Item → 批量生成 World Prefab`
2. 勾选"📂 从文件夹批量生成"
3. 输入或选择 ItemData 所在的文件夹路径（如 `Assets/111_Data/Items`）
4. 点击"🔍 预览文件夹内容"查看将要处理的物品
5. 勾选"覆盖已存在文件"（如果需要更新）
6. 点击"🚀 生成"按钮

工具会自动：
- 生成 45度旋转的世界 Sprite
- 创建包含 WorldItemPickup、WorldItemDrop、SpriteRenderer、Shadow、CircleCollider2D 的 Prefab
- 设置 Tag 为 "Pickup"
- 设置 linkedItemData 确保拾取正确
- 将生成的 Prefab 自动关联到 ItemData.worldPrefab 字段


---

### 会话 8 - 2026-01-04

**用户需求**:
> 背包交互系统升级中涉及的掉落物冷却机制

**完成任务**:
1. ✅ 扩展 WorldItemPickup 添加拾取冷却支持
   - 添加 `SetSpawnCooldown(float)` - 资源节点掉落物使用
   - 添加 `SetDropCooldown(float)` - 玩家丢弃物品使用
   - 添加 `CanBePickedUp()` - 检查是否可拾取
   - 添加 `OnPlayerExitRange()` - 玩家离开范围时调用
2. ✅ 修改 WorldItemPool.Spawn 设置生成冷却
   - 新增 `setSpawnCooldown` 参数（默认 true）
   - 生成时自动设置 1 秒冷却（硬编码）
3. ✅ 修改 AutoPickupService 检查冷却
   - 拾取前调用 `CanBePickedUp()` 检查
   - 添加离开范围检测，调用 `OnPlayerExitRange()`

**修改文件**:
- `Assets/YYY_Scripts/World/WorldItemPickup.cs` - 添加冷却字段和方法
- `Assets/YYY_Scripts/World/WorldItemPool.cs` - 添加 setSpawnCooldown 参数
- `Assets/YYY_Scripts/Service/Player/AutoPickupService.cs` - 添加冷却检查和离开范围检测

**冷却机制说明**:

| 类型 | 冷却时间 | 重置条件 |
|------|---------|---------|
| 资源节点掉落 | 1 秒 | 时间结束 |
| 玩家丢弃 | 5 秒 | 时间结束 或 离开范围后重新进入 |

**关键决策**:

| 决策 | 原因 | 日期 |
|------|------|------|
| 生成冷却 1 秒硬编码 | 简化配置，统一行为 | 2026-01-04 |
| 丢弃冷却支持离开范围重置 | 给玩家反悔机会 | 2026-01-04 |
