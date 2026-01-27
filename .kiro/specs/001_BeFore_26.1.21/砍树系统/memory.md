# 砍树系统 - 开发记忆

## 模块概述

工具-资源交互系统，以砍树功能为核心，建立可扩展的工具命中检测和资源交互框架。

## 当前状态
- **完成度**: 98%
- **最后更新**: 2025-12-24
- **状态**: 已修复锄头挖树苗问题，待用户测试验证

## 会话记录

### 会话 3 - 2025-12-24（锄头挖树苗修复）

**用户需求**:
> 树木系统内设定stage0只能被hoe挖走，不能被砍伐也没有碰撞体，但是动画控制器内你真的让hoe能够挖走种子了吗，我在游戏内使用稿子去挖是挖不动的，所以你没有做相关逻辑

**问题分析**:
1. `PlayerToolHitEmitter.BuildContext()` 根据动画状态推断工具类型
2. `STATE_CRUSH = 8` 被映射到 `ToolType.Pickaxe`
3. 但锄头（Hoe）也使用 `Crush` 动画状态（AnimActionType.Crush）
4. 所以当使用锄头时，系统错误地认为是镐子，导致无法挖出树苗

**解决方案**:
1. 在 `PlayerToolHitEmitter` 中添加对 `PlayerToolController` 的引用
2. 从 `PlayerToolController.CurrentToolData.toolType` 获取实际的工具类型
3. 如果没有装备工具，则回退到根据动画状态推断

**修改文件**:
- `Assets/YYY_Scripts/Combat/PlayerToolHitEmitter.cs`
  - 添加 `playerToolController` 字段引用
  - 修改 `Start()` 自动获取 `PlayerToolController`
  - 修改 `BuildContext()` 从工具数据获取实际工具类型

**核心代码变更**:
```csharp
// BuildContext() 修改
ToolType toolType = ToolType.None;

if (playerToolController != null && playerToolController.CurrentToolData != null)
{
    // 优先从当前装备的工具数据获取工具类型
    toolType = playerToolController.CurrentToolData.toolType;
}
else
{
    // 回退：根据动画状态推断工具类型
    toolType = state switch
    {
        STATE_SLICE => ToolType.Axe,
        STATE_CRUSH => ToolType.Pickaxe,
        _ => ToolType.None
    };
}
```

**遗留问题**:
- [ ] 需要用户测试验证锄头挖树苗功能

---

### 会话 1 - 2025-12-22

**用户需求**:
> 首先是如图的砍树问题，这个情况下树木被砍到了，可是树木的collider并未进入扇形区域也被判定了，所以我需要你更加精确的判断机制，消除这个bug，并且我还希望你不仅要判断被砍的树木是否在扇形内，还要判断人物朝向以及人物与树木相对位置的判断，判断是否符合正常逻辑，你看如图人物朝下但是树木在人的右边，人再怎么说若是朝下也必须要大于树木的collider最上部，也就是走到了树的上面并且靠近树一些才能砍到吧，并且我现在需要限制玩家砍树判定范围内若有多颗树木只能击中其中一颗

**完成任务**:
1. 更新需求文档，新增需求 13-15
2. 更新设计文档，添加精确命中检测流程和正确性属性
3. 扩展 IResourceNode 接口，添加 GetColliderBounds() 方法
4. 在 TreeControllerV2、TreeController、StoneController 中实现 GetColliderBounds()
5. 重写 PlayerToolHitEmitter.TryHit() 实现三重验证
6. 实现 ValidatePositionRelation() 位置验证方法

**修改文件**:
- `Assets/Scripts/Interfaces/IResourceNode.cs` - 添加 GetColliderBounds() 接口方法
- `Assets/Scripts/Controller/TreeControllerV2.cs` - 实现 GetColliderBounds()
- `Assets/Scripts/Controller/TreeController.cs` - 实现 GetColliderBounds()
- `Assets/Scripts/Controller/StoneController.cs` - 实现 GetColliderBounds()
- `Assets/Scripts/Combat/PlayerToolHitEmitter.cs` - 重写 TryHit()，添加 ValidatePositionRelation()
- `.kiro/specs/tree-chopping-system/requirements.md` - 新增需求 13-15
- `.kiro/specs/tree-chopping-system/design.md` - 更新命中检测设计
- `.kiro/specs/tree-chopping-system/tasks.md` - 新增任务 16-17

**解决方案**:

### 三重验证机制

1. **Collider Bounds 扇形检测**
   - 使用 `GetColliderBounds()` 获取树木 Collider 的 bounds
   - 计算 Collider 最近点到玩家的距离和角度
   - 无 Collider 时回退到 Sprite bounds

2. **玩家朝向与相对位置验证**
   - 朝下 (Direction=0): playerY > collider.max.y - 0.1
   - 朝上 (Direction=1): playerY < collider.min.y + 0.1
   - 朝右 (Direction=2, FlipX=false): playerX < collider.min.x + 0.1
   - 朝左 (Direction=2, FlipX=true): playerX > collider.max.x - 0.1

3. **单目标选择**
   - 从通过验证的节点中选择 Collider 中心距离最近的一个
   - 使用玩家 Collider 中心到树木 Collider 中心的距离

**遗留问题**:
- [ ] 需要用户手动测试验证命中检测功能
- [ ] TreeControllerV2 可能需要额外测试（本次只测试了 TreeController）

---

### 会话 2 - 2025-12-22（文档整理）

**用户需求**:
> 现在这个任务做的很不错，你完成的很好，但是我仅仅只是测试的treecontroller而非v2版本，所以后续可能会在v2出问题，你现在需要把当前方案和细节同步在memory内和Docx\分类\砍树文件夹内新建一个"砍树"的md文档，这个文档要写出所有有关砍树的核心逻辑和接口设计以及各种细节，这个文档我需要它能达到交接文档的详尽水平

**完成任务**:
1. 创建详尽的砍树系统交接文档 `Docx/分类/砍树/砍树系统完整文档.md`
2. 更新 memory.md 添加本次会话记录

**创建文件**:
- `Docx/分类/砍树/砍树系统完整文档.md` - 完整交接文档，包含：
  - 系统概述和架构设计
  - 核心组件说明
  - 三重验证机制详解
  - 接口设计和数据结构
  - 关键算法（倒下方向判定、Direction 参数映射）
  - 配置参数表
  - 调试工具说明
  - 常见问题解答
  - 相关文件清单

**注意事项**:
- 用户只测试了 TreeController，TreeControllerV2 可能需要额外验证
- 两个控制器都实现了相同的 `GetColliderBounds()` 方法
- 核心逻辑在 PlayerToolHitEmitter 中，与具体控制器无关

---

## 关键决策

| 决策 | 原因 | 日期 |
|------|------|------|
| 使用 Collider bounds 而非 Sprite bounds | 树木 Sprite 很大但 Collider 只覆盖树根，使用 Collider 更精确 | 2025-12-22 |
| 使用 Collider 中心距离而非最近点距离 | 用户明确要求使用 Collider 中心距离 | 2025-12-22 |
| 位置验证容差值 0.1 | 避免边界情况导致的误判 | 2025-12-22 |
| Direction 参数映射 0=Down, 1=Up, 2=Side | 来自 PlayerAnimController.ConvertToAnimatorDirection() | 2025-12-22 |
| 从 PlayerToolController 获取工具类型 | 动画状态无法区分 Hoe 和 Pickaxe（都用 Crush） | 2025-12-24 |

## 相关文件

| 文件 | 说明 |
|------|------|
| `Assets/YYY_Scripts/Interfaces/IResourceNode.cs` | 资源节点接口 |
| `Assets/YYY_Scripts/Combat/PlayerToolHitEmitter.cs` | 命中发射器（核心修改） |
| `Assets/YYY_Scripts/Combat/ResourceNodeRegistry.cs` | 资源节点注册表 |
| `Assets/YYY_Scripts/Controller/TreeControllerV2.cs` | 树木控制器 V2 |
| `Assets/YYY_Scripts/Controller/TreeController.cs` | 树木控制器 |
| `Assets/YYY_Scripts/Controller/StoneController.cs` | 矿石控制器 |
| `Assets/YYY_Scripts/Anim/Player/PlayerToolController.cs` | 工具控制器（提供 CurrentToolData） |
| `Docx/分类/砍树/砍树系统完整文档.md` | 完整交接文档 |
