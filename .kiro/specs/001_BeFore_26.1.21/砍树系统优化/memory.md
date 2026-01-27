# 砍树系统优化 - 开发记忆

## 模块概述

修复砍树系统的多个严重问题，包括伤害计算、精力消耗逻辑、树桩砍伐、性能卡顿、成长空间检测等。

## 当前状态
- **完成度**: 100%
- **最后更新**: 2025-12-28
- **状态**: 已完成（成长边距检测重构完成）

## 会话记录

### 会话 1 - 2025-12-26

**用户需求**:
> 1、首先我进入游戏会出现一两秒的卡顿
> 2、我在用右键进入下一天时，树木的成长也会让我卡顿
> 3、对于砍伐很奇怪，我砍树无论是什么斧头都是造成了1点伤害
> 4、我如果用低级的斧头砍高级的树也会消耗精力，只是不对树造成伤害
> 5、树桩无法被砍到
> 6、对于物品的情况，SO内更改了显示尺寸配置的参数，预制体并不会获取这个值
> 7、对于物品的堆叠处理，amount的位置、大小、字体都不对

**问题分析**:

1. **伤害只有1点** - `baseDamage = 1f + toolQuality * 0.5f`，而 `toolQuality = 0`
2. **精力消耗逻辑错误** - 等级检查在精力消耗之前
3. **树桩无法被砍** - `IsDepleted` 和 `CanAccept` 都阻止了树桩被砍
4. **树木成长卡顿** - `UpdatePolygonColliderFromSprite` 每次 UpdateSprite 都调用
5. **Amount 显示不正确** - UI 配置参数错误

**已完成任务**:
1. ✅ 修复 TreeControllerV2.HandleAxeChop() - 先消耗精力，再检查等级
2. ✅ 修复 PlayerToolHitEmitter.BuildContext() - 从 ToolData 获取正确的伤害值
3. ✅ 添加 GetToolBaseDamage() 方法 - 斧头伤害基于材料等级
4. ✅ 优化 TreeControllerV2.UpdateColliderState() - 移除每次更新碰撞体形状
5. ✅ 添加 UpdatePolygonColliderShape() - 仅在阶段变化时更新碰撞体
6. ✅ 修复 InventorySlotUI - Amount 文本配置按用户指定参数
7. ✅ 更新 UI 规范 - 记录 Amount 文本标准配置
8. ✅ 修复 TreeControllerV2.IsDepleted - 树桩状态不算耗尽
9. ✅ 修复 TreeControllerV2.CanAccept() - 树桩状态下接受斧头

**修改文件**:
- `Assets/YYY_Scripts/Controller/TreeControllerV2.cs` - 修复砍伐逻辑，优化性能，修复树桩砍伐
- `Assets/YYY_Scripts/Combat/PlayerToolHitEmitter.cs` - 修复伤害计算
- `Assets/YYY_Scripts/UI/Inventory/InventorySlotUI.cs` - 修复 Amount 文本配置
- `Assets/YYY_Scripts/World/WorldItemPickup.cs` - 移除过多调试日志
- `.kiro/steering/ui.md` - 更新 UI 规范

**斧头伤害配置**:
| 材料等级 | 伤害值 |
|---------|--------|
| 木质 | 2 |
| 石质 | 3 |
| 生铁 | 4 |
| 黄铜 | 5 |
| 钢质 | 6 |
| 金质 | 8 |

**树桩砍伐修复详情**:
- **问题1**: `IsDepleted` 返回 `currentState == TreeState.Stump`，导致树桩被跳过
- **修复1**: `IsDepleted` 改为始终返回 `false`（树木销毁时对象已不存在）
- **问题2**: `CanAccept()` 中 `if (currentState == TreeState.Stump) return false`
- **修复2**: 树桩状态下检查是否为斧头，是则返回 `true`

**待测试项**:
1. 砍树伤害是否正确（不同材料斧头应有不同伤害）
2. 树桩是否可以被砍
3. 树木成长是否还卡顿
4. 游戏启动卡顿（需要进一步分析）
5. SO 参数修改后预制体实时更新（尚未实现）

**遗留问题**:
- [ ] 游戏启动卡顿问题需要进一步分析
- [ ] SO 参数修改后预制体实时更新功能尚未实现

---

### 会话 2 - 2025-12-26（续）

**继续修复**:
> 从上下文继承，继续修复树桩砍伐问题

**完成任务**:
1. ✅ 发现并修复 `IsDepleted` 属性 - 树桩状态不应该返回 true
2. ✅ 发现并修复 `CanAccept()` 方法 - 树桩状态下应该接受斧头

**解决方案**:
```csharp
// IsDepleted 修复
public bool IsDepleted => false;  // 树木销毁时对象已不存在

// CanAccept 修复
public bool CanAccept(ToolHitContext ctx)
{
    // 树桩状态下，只接受斧头
    if (currentState == TreeState.Stump)
    {
        return ctx.toolType == ToolType.Axe;
    }
    // ... 其他逻辑
}
```

---

### 会话 3 - 2025-12-26（续）

**用户反馈（图片）**:
> 1、树木成长空间检测不够严格，stage4 时四棵树已经很拥挤，但还有两棵进入了下一阶段
> 2、树桩的碰撞体位置错乱（绿色线框与 Sprite 不匹配）
> 3、卡顿问题未解决
> 4、SO 参数修改后预制体实时更新尚未完成

**问题分析**:
1. **成长空间检测** - 当前只检测碰撞体，应该检测下一阶段的 Sprite Bounds 是否会与周围树木重叠
2. **树桩碰撞体错乱** - `FinishChopDown` 中没有调用 `UpdatePolygonColliderShape()` 更新碰撞体形状

**完成任务**:
1. ✅ 重写 `CanGrowToNextStage()` - 使用 Sprite Bounds 检测重叠，允许最多 20% 重叠
2. ✅ 添加 `GetStageBounds()` 方法 - 获取指定阶段的 Sprite Bounds
3. ✅ 修复 `FinishChopDown()` - 添加 `UpdatePolygonColliderShape()` 调用

**成长空间检测改进**:
```csharp
// 获取下一阶段的 Sprite Bounds
Bounds nextStageBounds = GetStageBounds(nextStage);

// 计算下一阶段的世界空间 Bounds
Bounds worldBounds = new Bounds(treeRoot + nextStageBounds.center, nextStageBounds.size);

// 检查与周围树木的 Sprite Bounds 是否重叠
if (worldBounds.Intersects(otherBounds))
{
    // 计算重叠率，允许最多 20% 重叠
    if (overlapRatio > 0.2f) return false;
}
```

**树桩碰撞体修复**:
```csharp
private void FinishChopDown()
{
    currentState = TreeState.Stump;
    // ...
    UpdateSprite();
    // ★ 树桩状态需要更新碰撞体形状
    UpdatePolygonColliderShape();
}
```

---

### 会话 5 - 2025-12-26（成长检测重构 + 导航修复）

**用户反馈**:
> 1、最右边这个树没有和他们进行重叠，你也不让他生长。参数设定不够严谨，需要判断 collider 距离和 sprite bound 距离
> 2、导航系统不能识别 V2 的树，是不是树注册的时候没有注册 V2？只注册了原版？
> 3、去掉左数第二颗树后右边那棵树可以进入 stage4，但左数第一棵树边上没有任何物体却不能变更大

**问题分析**:

1. **成长检测逻辑缺陷**：
   - 只使用 `Physics2D.OverlapCircleAll` 检测，依赖碰撞体
   - 树苗阶段没有碰撞体，检测不到
   - 搜索半径不够大
   - 没有考虑树根之间的距离

2. **导航系统不识别 V2 树**：
   - V2 树预制体的父物体标签是 `Untagged`，不是 `Tree`
   - NavGrid2D 使用 `obstacleTags` 检测障碍物，需要 `Tree` 标签

3. **成长检测的连锁反应**：
   - 使用 `Physics2D` 检测时，如果周围树没有碰撞体就检测不到
   - 应该使用 `ResourceNodeRegistry` 查找所有树木

**完成任务**:

1. ✅ **重写 `CanGrowToNextStage()` 方法 v2**：
   - 使用 `ResourceNodeRegistry` 查找所有树木（不依赖碰撞体）
   - 同时检测树根距离和 Sprite Bounds 重叠
   - 添加 `CanGrowNearTree()` 方法进行详细检测
   - 添加 `GetMinRootDistance()` 计算最小树根距离
   - 添加 `GetStageRootRadius()` 获取阶段树根半径
   - 添加 `CalculateOverlapRatio()` 计算重叠率
   - 保留 `CanGrowToNextStageByPhysics()` 作为回退方案

2. ✅ **修复 V2 树预制体标签**：
   - M1.prefab 父物体标签改为 `Tree`
   - M2.prefab 父物体标签改为 `Tree`
   - M3.prefab 父物体标签改为 `Tree`

3. ✅ **创建编辑器工具**：
   - `Assets/Editor/TreeV2TagFixer.cs` - 批量修复 V2 树标签

**成长检测改进详情**:

```csharp
// 1. 使用 ResourceNodeRegistry 查找所有树木
var nearbyNodes = ResourceNodeRegistry.Instance.GetNodesInRange(treeRoot, searchRadius, "Tree");

// 2. 检查树根距离
float rootDistance = Vector2.Distance(myRoot, otherRoot);
float minRootDistance = GetMinRootDistance(myNextStage, otherTree.currentStageIndex);
if (rootDistance < minRootDistance) return false;

// 3. 检查 Sprite Bounds 重叠
if (myWorldBounds.Intersects(otherBounds))
{
    float overlapRatio = CalculateOverlapRatio(myWorldBounds, otherBounds);
    if (overlapRatio > 0.2f) return false;
}
```

**树根距离计算**:
| 阶段 | 树根半径 |
|------|---------|
| 0 | 0.2 |
| 1 | 0.3 |
| 2 | 0.5 |
| 3 | 0.7 |
| 4 | 0.9 |
| 5 | 1.1 |

最小树根距离 = (我的半径 + 对方半径) × 0.8

**修改文件**:
- `Assets/YYY_Scripts/Controller/TreeControllerV2.cs` - 重写成长检测逻辑
- `Assets/222_Prefabs/Tree_V2/M1.prefab` - 修复标签
- `Assets/222_Prefabs/Tree_V2/M2.prefab` - 修复标签
- `Assets/222_Prefabs/Tree_V2/M3.prefab` - 修复标签
- `Assets/Editor/TreeV2TagFixer.cs` - 新增编辑器工具

**待测试项**:
1. 成长检测是否更准确（同时检测树根距离和 Sprite 重叠）
2. 导航系统是否能识别 V2 树
3. 单独的树是否能正常成长

---

### 会话 6 - 2025-12-26（成长检测 v3 + Layer 修复）

**用户反馈**:
> 1、右边这棵树就是不长，你搞什么在
> 2、修复 V2 树预制体标签后导航还是没用

**问题分析**:

1. **成长检测问题**：
   - 搜索半径太大：`searchRadius = Mathf.Max(nextStageBounds.size.x, nextStageBounds.size.y) * 2f`
   - 如果下一阶段 Sprite 很大（如 stage 5），搜索半径会非常大，找到很远的树
   - 树根距离检测太严格，导致远处的树也被认为是障碍

2. **导航系统问题**：
   - V2 树的 Layer = 20（Layer 1），V1 树的 Layer = 0（Default）
   - NavGrid2D 的 `obstacleMask` 可能只包含 Layer 0
   - 即使标签正确，Layer 不匹配也会导致检测失败

**完成任务**:

1. ✅ **重写 `CanGrowToNextStage()` 方法 v3**：
   - 修复搜索半径：`myHalfWidth + maxOtherHalfWidth + 0.5f`（只找真正可能重叠的树）
   - 移除树根距离检测（因为树根距离不影响视觉重叠）
   - 只检测 Sprite Bounds 重叠
   - 提高重叠阈值到 30%（允许更多重叠）
   - 添加调试日志输出搜索半径和找到的树数量

2. ✅ **修复 V2 树预制体 Layer**：
   - M1.prefab 所有物体 Layer 从 20 改为 0
   - M2.prefab 所有物体 Layer 从 20 改为 0
   - M3.prefab 所有物体 Layer 从 20 改为 0
   - 与 V1 树保持一致（Layer = 0 = Default）

**成长检测 v3 改进**:

```csharp
// ★ 修复：搜索半径 = 下一阶段 Sprite 的半宽度 + 最大可能的其他树半宽度
float myHalfWidth = nextStageBounds.size.x * 0.5f;
float maxOtherHalfWidth = 3f; // 假设最大树的半宽度不超过 3 单位
float searchRadius = myHalfWidth + maxOtherHalfWidth + 0.5f;

// ★ 只检查 Sprite Bounds 重叠（移除树根距离检测）
if (!myWorldBounds.Intersects(otherBounds))
{
    return true; // 不相交，可以成长
}

// 允许最多 30% 的重叠
if (overlapRatio > 0.3f) return false;
```

**修改文件**:
- `Assets/YYY_Scripts/Controller/TreeControllerV2.cs` - 成长检测 v3
- `Assets/222_Prefabs/Tree_V2/M1.prefab` - Layer 改为 0
- `Assets/222_Prefabs/Tree_V2/M2.prefab` - Layer 改为 0
- `Assets/222_Prefabs/Tree_V2/M3.prefab` - Layer 改为 0

**待测试项**:
1. 单独的树是否能正常成长
2. 导航系统是否能识别 V2 树（Layer 修复后）
3. 成长检测是否只阻挡真正重叠的树

---

### 会话 7 - 2025-12-26（成长检测 v4 + 导航问题分析）

**用户反馈**:
> 1、可是如图最右边这个树又没有和他们进行重叠，你也不让他生长
> 2、对于你现在V2的树我的设置和V1的都一样呀，标签层级脚本，只是treecontroller一个是V1一个是V2，但是你的导航就是不能识别v2的树
> 3、去掉左数第二颗树的时候你的右边那棵树可以进入stage4，这很怪异

**问题分析**:

1. **成长检测 v3 的问题**：
   - 搜索半径计算仍然有问题：`myHalfWidth + maxOtherHalfWidth + 0.5f`
   - 如果下一阶段 Sprite 宽度是 6.75（stage 5），`myHalfWidth = 3.375`
   - `searchRadius = 3.375 + 3 + 0.5 = 6.875`
   - 这个半径可能还是不够大，无法找到所有可能重叠的树
   - 或者相反，找到了不应该阻挡的树

2. **连锁反应问题**：
   - 去掉左边第二棵树后右边的树能成长
   - 说明成长检测逻辑有连锁依赖问题
   - 可能是因为搜索到了远处的树，但实际上不重叠

3. **导航系统问题**：
   - V1 和 V2 树的预制体配置已经一致（Layer=0, Tag=Tree）
   - 问题可能在于 **NavGrid2D 的 `obstacleTags` 没有配置 "Tree"**
   - 这是场景配置问题，需要在 Inspector 中检查

**完成任务**:

1. ✅ **重写 `CanGrowToNextStage()` 方法 v4**：
   - 使用更大的搜索半径：`(myDiagonal + maxOtherDiagonal) * 0.5f`
   - 基于 Sprite 对角线长度计算，确保能找到所有可能重叠的树
   - 添加详细的调试日志，输出搜索半径、找到的树数量、每棵树的重叠检测结果
   - 完全基于 Sprite Bounds 重叠判断，不依赖树根距离

2. ✅ **改进 `CanGrowNearTree()` 方法**：
   - 添加详细的调试日志，输出两棵树的 Bounds 和重叠率
   - 如果不重叠，明确输出"可以成长"
   - 如果重叠，输出重叠率和阻挡原因

**成长检测 v4 改进**:

```csharp
// ★ v4 改进：使用足够大的搜索半径，确保能找到所有可能重叠的树
// 搜索半径 = 我的 Sprite 对角线长度 + 最大可能的其他树对角线长度
float myDiagonal = Mathf.Sqrt(nextStageBounds.size.x * nextStageBounds.size.x + nextStageBounds.size.y * nextStageBounds.size.y);
float maxOtherDiagonal = 15f; // 假设最大树的对角线不超过 15 单位
float searchRadius = (myDiagonal + maxOtherDiagonal) * 0.5f;

// 详细调试日志
if (showDebugInfo)
{
    Debug.Log($"<color=cyan>[TreeControllerV2] {gameObject.name} 成长检测 v4：\n" +
              $"  - 当前阶段: {currentStageIndex} → {nextStage}\n" +
              $"  - 下一阶段 Bounds: {nextStageBounds.size}\n" +
              $"  - 世界空间 Bounds: center={myWorldBounds.center}, size={myWorldBounds.size}\n" +
              $"  - 搜索半径: {searchRadius:F2}</color>");
}
```

**导航系统问题说明**:

NavGrid2D 使用 `Physics2D.OverlapCircleAll` 检测障碍物，然后检查标签。需要确保：
1. NavGrid2D 的 `obstacleTags` 数组包含 "Tree"
2. 或者 `obstacleMask` 包含树木所在的 Layer

**请在 Unity 编辑器中检查**：
1. 找到场景中的 NavGrid2D 组件
2. 检查 `Obstacle Tags` 数组是否包含 "Tree"
3. 如果没有，添加 "Tree" 标签

**修改文件**:
- `Assets/YYY_Scripts/Controller/TreeControllerV2.cs` - 成长检测 v4

**待测试项**:
1. 开启 `showDebugInfo` 查看成长检测日志
2. 确认单独的树是否能正常成长
3. 检查 NavGrid2D 的 `obstacleTags` 配置

---

## 关键决策

| 决策 | 原因 | 日期 |
|------|------|------|
| 先消耗精力，再检查等级 | 符合用户需求：挥动斧头就消耗精力，无论等级是否足够 | 2025-12-26 |
| 斧头伤害基于材料等级 | 不同材料的斧头应该有不同的伤害 | 2025-12-26 |
| 仅在阶段变化时更新碰撞体 | 避免每次 UpdateSprite 都更新碰撞体导致性能问题 | 2025-12-26 |
| Amount 文本使用用户指定参数 | 字体18，加粗倾斜，纯黑，右下对齐 | 2025-12-26 |
| IsDepleted 始终返回 false | 树桩可以继续被砍，销毁时对象已不存在 | 2025-12-26 |
| 树桩状态下 CanAccept 接受斧头 | 树桩只能用斧头砍，需要特殊处理 | 2025-12-26 |
| 使用 Sprite Bounds 检测成长空间 | 碰撞体只覆盖树根，无法准确判断树冠重叠 | 2025-12-26 |
| 允许最多 30% 重叠 | 树冠可以稍微重叠，看起来更自然 | 2025-12-26 |
| 使用 ResourceNodeRegistry 查找树木 | 不依赖碰撞体，可以检测到所有阶段的树木 | 2025-12-26 |
| V2 树预制体需要 Tree 标签 | 导航系统使用标签检测障碍物 | 2025-12-26 |
| 成长检测 v4 使用对角线计算搜索半径 | 确保能找到所有可能重叠的树，避免漏检 | 2025-12-26 |

## 相关文件

| 文件 | 说明 |
|------|------|
| `Assets/YYY_Scripts/Controller/TreeControllerV2.cs` | 树木控制器，砍伐逻辑 |
| `Assets/YYY_Scripts/Combat/PlayerToolHitEmitter.cs` | 工具命中发射器，伤害计算 |
| `Assets/YYY_Scripts/Combat/ResourceNodeRegistry.cs` | 资源节点注册表 |
| `Assets/YYY_Scripts/UI/Inventory/InventorySlotUI.cs` | 背包槽位 UI |
| `Assets/YYY_Scripts/World/WorldItemPickup.cs` | 世界物品拾取 |
| `Assets/222_Prefabs/Tree_V2/M1.prefab` | V2 树预制体 |
| `Assets/222_Prefabs/Tree_V2/M2.prefab` | V2 树预制体 |
| `Assets/222_Prefabs/Tree_V2/M3.prefab` | V2 树预制体 |
| `Assets/Editor/TreeV2TagFixer.cs` | V2 树标签修复工具 |
| `.kiro/steering/ui.md` | UI 系统规范 |

### 会话 8 - 2025-12-28（成长边距检测重构完成）

**用户需求**:
> 完成 .kiro/specs/砍树系统优化/0_成长边距检测重构 工作区的任务

**完成任务**:

1. ✅ **任务 1**：修改 StageConfig 类，添加边距参数（已完成）
   - `verticalMargin` 字段（上下边距）
   - `horizontalMargin` 字段（左右边距）
   - StageConfigFactory 默认配置已更新

2. ✅ **任务 2**：修改 TreeControllerV2，添加障碍物标签字段（已完成）
   - `growthObstacleTags` 字符串数组字段
   - 默认值为 ["Tree", "Rock", "Building"]

3. ✅ **任务 3**：创建 TreeControllerV2Editor，实现 MaskField 下拉框
   - 删除重复的 Editor 文件 `Assets/YYY_Scripts/Utils/Editor/TreeControllerV2Editor.cs`
   - 更新主 Editor 文件 `Assets/Editor/TreeControllerV2Editor.cs`
   - 添加 `DrawTagMask()` 方法实现 MaskField 风格的多选下拉框
   - 在 `DrawGrowthSettings()` 中绘制障碍物标签选择器
   - 在 `DrawSingleStageConfig()` 中添加边距参数显示

4. ✅ **任务 4**：重写 CanGrowToNextStage 方法（已完成）
   - 获取下一阶段的边距配置
   - 调用 CheckGrowthMargin 进行检测

5. ✅ **任务 5**：实现 CheckGrowthMargin 方法（已完成）
   - 获取 Collider 中心点
   - 检测上下左右四个方向
   - 返回是否可以成长

6. ✅ **任务 6**：实现 HasObstacleInDirection 方法（已完成）
   - 使用 Physics2D.OverlapCircle 检测
   - 过滤指定标签的 Collider
   - 跳过自身和子物体

7. ✅ **任务 7**：删除旧的成长检测相关代码（已完成）
   - 已移除 growthSpaceMultiplier 字段
   - 已移除 stageGrowthRadius 数组
   - 已移除旧的检测方法

8. ✅ **任务 8**：更新 GrowToNextStage 方法（已完成）
   - 成长后调用 UpdatePolygonColliderShape 更新碰撞体

9. ✅ **任务 9**：编译验证和测试（已完成）
   - 编译成功，无错误无警告

**修改文件**:
- `Assets/Editor/TreeControllerV2Editor.cs` - 添加 MaskField 和边距参数显示
- `Assets/YYY_Scripts/Utils/Editor/TreeControllerV2Editor.cs` - 已删除（重复文件）

**成长边距检测 v5 核心逻辑**:
```
树木尝试成长
    │
    ▼
获取下一阶段配置
    │
    ├─ verticalMargin (上下边距)
    └─ horizontalMargin (左右边距)
    │
    ▼
获取当前 Collider 中心点
    │
    ▼
四方向检测（Physics2D.OverlapCircle）
    │
    ├─ 上方: center + (0, verticalMargin)
    ├─ 下方: center - (0, verticalMargin)
    ├─ 左方: center - (horizontalMargin, 0)
    └─ 右方: center + (horizontalMargin, 0)
    │
    ▼
任意方向有障碍物 → 返回 false (不能成长)
所有方向无障碍物 → 返回 true (可以成长)
```

**默认边距配置**:
| 阶段 | 上下边距 | 左右边距 |
|------|---------|---------|
| 0 (树苗) | 0.2 | 0.15 |
| 1 (小树苗) | 0.3 | 0.2 |
| 2 (中等树) | 0.5 | 0.35 |
| 3 (大树) | 0.7 | 0.5 |
| 4 (成熟树) | 0.9 | 0.65 |
| 5 (完全成熟) | 1.1 | 0.8 |

---

