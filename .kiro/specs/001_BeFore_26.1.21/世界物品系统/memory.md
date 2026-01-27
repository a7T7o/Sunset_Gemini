# 世界物品掉落系统 - 开发记忆

## 模块概述

世界物品掉落与拾取系统，当玩家砍树、挖矿、收获作物时，物品以弹性动画形式掉落到地面。包括：
- 视觉资源自动生成（从图标生成世界Sprite和Prefab）
- 掉落弹性动画（弹出+弹跳+浮动）
- 阴影呼吸系统
- 拾取飞向玩家动画
- 对象池性能优化
- 砍树/挖矿掉落集成

## 当前状态

- **完成度**: 100%
- **最后更新**: 2025-12-24
- **状态**: ✅ V1 版本已完成

---

## V1 版本记录 (2025-12-24)

### 核心功能

1. **WorldPrefab 生成工具** (`WorldPrefabGeneratorTool.cs`)
   - Sprite 子物体 Z 轴旋转 45°（保持像素完整）
   - 整体缩放可调节（默认 0.75）
   - 阴影自动计算大小和位置
   - 自动添加 CircleCollider2D (Trigger) 和 Tag="Pickup"
   - 自动关联 linkedItemData 确保拾取正确
   - 手动获取选中项交互方式

2. **掉落动画组件** (`WorldItemDrop.cs`)
   - 弹性掉落动画（弹出+弹跳+浮动）
   - 阴影呼吸系统（缩放 0.85~1.0，透明度 0.25~0.4）
   - 距离优化（超出 15 单位暂停动画）

3. **拾取组件** (`WorldItemPickup.cs`)
   - 飞向玩家动画（抛物线弧度）
   - 自动初始化（从 linkedItemData 或预制体名称解析 itemId）
   - 对象池支持

4. **自动拾取服务** (`AutoPickupService.cs`)
   - 基于 Tag 和 Collider 检测
   - 触发飞向玩家动画

5. **对象池** (`WorldItemPool.cs`)
   - 黄金角度螺旋分布算法
   - 数量上限管理

### 预制体结构

```
WorldItem_{itemId}_{itemName}（根物体，整体缩放 0.75）
├── Tag: "Pickup"
├── CircleCollider2D (Trigger)
├── WorldItemPickup (linkedItemData 已关联)
├── WorldItemDrop
├── Sprite（Z 轴旋转 45°，位置略高于阴影）
│   └── SpriteRenderer（原始 Sprite，像素完整）
└── Shadow（位置 Y=0）
    └── SpriteRenderer（阴影，自动计算大小）
```

### 关键参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| worldItemScale | 0.75 | 整体缩放 |
| spriteRotationZ | 45° | Sprite Z 轴旋转 |
| shadowBottomOffset | 0.02 | 底部偏移 |
| bounceHeight | 0.8 | 弹跳高度 |
| bounceDecay | 0.5 | 弹跳衰减 |
| maxBounceCount | 3 | 弹跳次数 |
| idleFloatAmplitude | 0.03 | 浮动幅度 |
| idleFloatSpeed | 2.5 | 浮动速度 |
| shadowMinScaleRatio | 0.85 | 阴影最小缩放 |
| shadowMaxScaleRatio | 1.0 | 阴影最大缩放 |
| shadowMinAlpha | 0.25 | 阴影最小透明度 |
| shadowMaxAlpha | 0.4 | 阴影最大透明度 |
| flyDuration | 0.25s | 飞向玩家时长 |
| flyHeight | 0.3 | 飞向抛物线高度 |

### 已修复的问题

1. **OnValidate SendMessage 错误** - 使用 EditorApplication.delayCall 延迟执行
2. **预制体拖入场景无法拾取** - 添加 linkedItemData 字段和自动初始化逻辑
3. **像素破坏问题** - 改为 Transform 旋转而非图片旋转

---

## 会话记录

### 会话 1 - 2025-12-20

**用户需求**:
> 与石头系统集成，支持矿物和石料的差值掉落

**完成任务**:
1. ✅ 更新 memory.md 添加石头系统关联信息

---

### 会话 2 - 2025-12-21

**用户需求**:
> 更新各工作区的 design.md 和 tasks.md

**完成任务**:
1. ✅ 更新 design.md 添加与石头系统的集成说明
2. ✅ 添加差值掉落机制说明

---

### 会话 3 - 2025-12-22

**用户需求**:
> 完成物品掉落与拾取系统的所有功能

**完成任务**:
1. ✅ WorldItemDrop.cs - 掉落弹性动画
2. ✅ WorldItemPickup.cs - 拾取组件（含飞向玩家动画）
3. ✅ WorldSpawnService.cs - 生成服务
4. ✅ WorldItemPool.cs - 对象池
5. ✅ AutoPickupService.cs - 自动拾取服务
6. ✅ WorldPrefabGeneratorTool.cs - 预制体生成工具

---

### 会话 4 - 2025-12-22

**用户需求**:
> 1. Sprite 子物体 Z 轴旋转（保持像素完整）
> 2. 整体缩放可调节
> 3. 阴影自动计算
> 4. 浮动时阴影呼吸变化

**完成任务**:
1. ✅ 重写 WorldPrefabGeneratorTool
2. ✅ 重写 WorldItemDrop 阴影呼吸系统
3. ✅ 修复 OnValidate SendMessage 错误

---

### 会话 5 - 2025-12-22

**用户需求**:
> 将工具选择方式改为手动点击按钮获取选中项

**完成任务**:
1. ✅ 修改 WorldPrefabGeneratorTool 交互方式
2. ✅ 修改 Tool_BatchItemSOGenerator 交互方式

---

### 会话 6 - 2025-12-24

**用户需求**:
> 预制体拖入场景后飞向玩家但未进入背包

**问题分析**:
- 预制体拖入场景时未调用 Init() 方法
- itemId 保持默认值 -1，导致无法添加到背包

**解决方案**:
1. 添加 linkedItemData 字段，在生成预制体时自动关联
2. 添加 EnsureInitialized() 方法，在 Start() 时自动初始化
3. 支持从预制体名称解析 itemId 作为备份方案

**修改文件**:
- `Assets/YYY_Scripts/World/WorldItemPickup.cs` - 添加自动初始化逻辑
- `Assets/Editor/WorldPrefabGeneratorTool.cs` - 设置 linkedItemData

---

### 会话 7 - 2025-12-24

**用户需求**:
> 我的sprite素材资源可能大小不一，所以生成的每个物体都不一致大小...我希望你能给我一个限定大小的指数在SO内

**完成任务**:
1. ✅ ItemData 添加 useCustomDisplaySize 和 displayPixelSize 字段
2. ✅ WorldPrefabGeneratorTool 读取并应用 displayPixelSize
3. ✅ WorldItemPickup 添加 ApplyDisplaySize() 方法

**修改文件**:
- `Assets/YYY_Scripts/Data/Items/ItemData.cs` - 添加显示尺寸字段
- `Assets/Editor/WorldPrefabGeneratorTool.cs` - 应用显示尺寸
- `Assets/YYY_Scripts/World/WorldItemPickup.cs` - 运行时应用显示尺寸

**解决方案**:
- ItemData 新增 `useCustomDisplaySize` (bool) 和 `displayPixelSize` (int, 8-128)
- `GetWorldDisplayScale()` 方法计算缩放比例
- WorldPrefab 生成时应用缩放到 Sprite 和阴影
- 运行时 Init() 调用 ApplyDisplaySize()

---

## 关键决策

| 决策 | 原因 | 日期 |
|------|------|------|
| Transform 旋转而非图片旋转 | 保持像素完整性 | 2025-12-22 |
| 阴影呼吸系统 | 增强视觉反馈 | 2025-12-22 |
| 手动获取选中项 | 避免误操作 | 2025-12-22 |
| linkedItemData 自动关联 | 确保预制体拖入场景后能正确拾取 | 2025-12-24 |
| 预制体名称解析 itemId | 作为备份初始化方案 | 2025-12-24 |
| displayPixelSize 控制世界物品尺寸 | 统一不同大小素材的显示效果 | 2025-12-24 |

## 相关文件

### 核心脚本
| 文件 | 说明 |
|------|------|
| `Assets/YYY_Scripts/World/WorldItemDrop.cs` | 掉落弹性动画组件 |
| `Assets/YYY_Scripts/World/WorldItemPickup.cs` | 拾取组件（含飞向玩家动画） |
| `Assets/YYY_Scripts/World/WorldSpawnService.cs` | 生成服务 |
| `Assets/YYY_Scripts/World/WorldItemPool.cs` | 对象池 |
| `Assets/YYY_Scripts/Service/Player/AutoPickupService.cs` | 自动拾取服务 |
| `Assets/Editor/WorldPrefabGeneratorTool.cs` | 预制体生成工具 |
| `Assets/Editor/Tool_BatchItemSOGenerator.cs` | 批量生成物品 SO 工具 |

### 文档
| 文件 | 说明 |
|------|------|
| `Docx/分类/世界物品/world prefab生成与功能设计.md` | 详细方案总结文档 |

## 用户操作指南

### 生成 WorldPrefab

1. 打开菜单：`Tools → World Item → 批量生成 World Prefab`
2. 在 Project 窗口选择 ItemData 或文件夹
3. 点击"🔍 获取选中项"按钮
4. 调整参数（可选）
5. 点击"🚀 生成"按钮

### 使用 WorldPrefab

1. 将生成的预制体拖入场景
2. 物品会自动播放浮动动画
3. 玩家靠近时自动飞向玩家并进入背包


---

### 会话 8 - 2026-01-07

**用户反馈**:
> 从背包丢弃的物品没有阴影，与砍树掉落的物品外观不一致

**问题分析**:
1. `WorldItemPool.Spawn` 使用动态创建的 `defaultWorldPrefab`
2. `defaultWorldPrefab` 的阴影是程序生成的简单椭圆
3. 没有查找 `ItemData.worldPrefab` 来使用正确的预制体

**解决方案**:
创建新子工作区 `0_丢弃物预制体统一`，修改 WorldItemPool 以支持：
1. 优先使用 `ItemData.worldPrefab`
2. 按 itemId 分类管理对象池
3. 回退到 `defaultWorldPrefab`（当 worldPrefab 不存在时）

**完成任务**:
1. ✅ 创建子工作区 `.kiro/specs/世界物品系统/0_丢弃物预制体统一/`
2. ✅ 创建需求文档 `requirements.md`
3. ✅ 创建设计文档 `design.md`
4. ✅ 创建任务列表 `tasks.md`
5. ✅ 创建工作区记忆 `memory.md`

**下一步**:
- [ ] 执行 `0_丢弃物预制体统一` 工作区的任务
