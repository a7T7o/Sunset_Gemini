# 预览与放置位置对齐修复 - 开发记忆

## 模块概述

修复放置系统中预览位置与实际放置位置不一致的问题，使预览绿框正确包裹物品 Collider，预览和放置位置完全一致。

## 当前状态

- **完成度**: 90%
- **最后更新**: 2026-01-21
- **状态**: V2 修复完成，待测试

## 会话记录

### 会话 1 - 2026-01-21

**用户需求**:
> 预放置绿框应该包裹物品的 Collider（不是 Sprite），格子应该是一个个 GridCell 拼起来的，预览和放置的中心应该统一在预放置框的几何中心。

**完成任务**:
1. 分析问题根源：格子大小计算错误、预览 Sprite 与实际不同、对齐方式不统一
2. 创建需求文档 requirements.md
3. 创建设计文档 design.md
4. 创建任务列表 tasks.md
5. 修改 PlacementGridCalculator.cs - 添加从 PolygonCollider2D 计算格子大小和中心的方法
6. 修改 PlacementPreviewV3.cs - 使用预制体 Sprite 和 Collider 中心对齐
7. 修改 PlacementManagerV3.cs - 放置位置使用 Collider 中心对齐
8. Unity 编译验证通过

**遗留问题**:
- 树苗预览位置仍有偏差（底部对齐影响）

---

### 会话 2 - 2026-01-21（V2 修复）

**用户需求**:
> 树木还是存在位置偏差。需要更进一步思考是否存在更加合理的方式去处理这个差异，比如直接计算放置物体的 sprite 和 Collider 等信息，然后自动适应不同处理下的资源。

**问题分析**:
TreeControllerV2 和 ChestController 在 Awake/Start 时会执行 `AlignSpriteBottom()`，将 Sprite 底部对齐到原点。这会改变 Sprite 相对于 Collider 的位置关系，导致预览和放置后不一致。

**用户确认的方案**:
> 放置中心与实际中心一致，预览情况应该是预放置方框中心与物品 Collider 中心一致，放置下来后也应该是这样。

**完成任务**:
1. 更新 design.md - V2 版本，考虑底部对齐的影响
2. 添加 `GetColliderCenterAfterBottomAlign()` 方法 - 计算底部对齐后 Collider 中心位置
3. 添加 `GetPreviewSpriteLocalPosition()` 方法 - 计算预览 Sprite 的正确 localPosition
4. 修改 PlacementPreviewV3.Show() - 使用新方法
5. 修改 PlacementManagerV3.InstantiatePlacementPrefab() - 使用新方法
6. Unity 编译验证通过

**修改文件**:
- `Assets/YYY_Scripts/Service/Placement/PlacementGridCalculator.cs` - 新增 GetColliderCenterAfterBottomAlign、GetPreviewSpriteLocalPosition 方法
- `Assets/YYY_Scripts/Service/Placement/PlacementPreviewV3.cs` - 修改 Show() 使用 GetPreviewSpriteLocalPosition
- `Assets/YYY_Scripts/Service/Placement/PlacementManagerV3.cs` - 修改 InstantiatePlacementPrefab() 使用 GetColliderCenterAfterBottomAlign

**解决方案**:

核心原则：**预测底部对齐的影响，使放置后 Collider 中心对齐到格子中心**

```
底部对齐偏移 = -sprite.bounds.min.y
放置后 Collider 中心 = 原始 Collider 中心 + (0, 底部对齐偏移)
放置位置 = 格子中心 - 放置后 Collider 中心
```

**遗留问题**:
- 待用户测试验证

---

### 会话 3 - 2026-01-21（继续）

**用户需求**:
> 继续上次的工作，完成测试验证

**当前状态**:
代码实现已完成，Unity 编译通过。等待用户在 Unity 中测试：
1. 树苗放置 - 验证预览和放置位置一致
2. 箱子放置 - 验证预览和放置位置一致

**核心实现总结**:
- `GetColliderCenterAfterBottomAlign()` - 预测底部对齐后 Collider 中心位置
- `GetPreviewSpriteLocalPosition()` - 计算预览 Sprite 的正确 localPosition
- 预览和放置都使用相同的计算逻辑，确保一致性

---

## 关键决策

| 决策 | 原因 | 日期 |
|------|------|------|
| 使用 Collider 中心对齐而非 Sprite 底部对齐 | 用户明确要求预览框包裹 Collider，中心统一 | 2026-01-21 |
| 保留 ChestController 的底部对齐机制 | 底部对齐用于 Sprite 状态切换，不影响初始放置 | 2026-01-21 |
| 预测底部对齐影响 | 放置系统需要预测物品 Awake 时的底部对齐，才能正确计算放置位置 | 2026-01-21 |

## 相关文件

| 文件 | 说明 |
|------|------|
| `PlacementGridCalculator.cs` | 格子计算器，新增底部对齐后 Collider 中心计算方法 |
| `PlacementPreviewV3.cs` | 预览组件，修改 Show() 使用预制体 Sprite |
| `PlacementManagerV3.cs` | 放置管理器，修改放置位置计算 |


---

### 会话 4 - 2026-01-21（ChestController 底部对齐修复）

**背景**:
在会话 3 的测试中发现：
- 树苗放置：预览位置 = 放置位置 ✅
- 箱子放置：放置位置比预览位置**低** ❌

**问题分析**:
详见 `问题分析报告-V2.md`。核心问题是 TreeControllerV2 和 ChestController 的底部对齐机制不一致：

| 特性 | TreeControllerV2 | ChestController |
|------|-----------------|-----------------|
| 修改目标 | 子物体 localPosition | 根物体 position |
| 计算方式 | 绝对值：`-sprite.bounds.min.y` | 相对值：锚点差值 |
| **初始放置时** | **✅ 会执行** | **❌ 不会执行** |

**用户决策**:
> 通过规范开发来简化代码逻辑，而不是在放置系统中做特殊处理

**完成任务**:
1. 创建子工作区 `7_可放置物品开发规范`
2. 创建 steering 规则文件 `.kiro/steering/placeable-items.md`
3. 修改 ChestController 使其符合规范

**ChestController 修改内容**:
1. 新增 `AlignSpriteBottom()` 方法 - 与 TreeControllerV2 完全一致
   - 修改 `transform.localPosition.y = -sprite.bounds.min.y`
2. 修改 `UpdateSpriteForState()` - 使用新的 `AlignSpriteBottom()` 方法
3. 标记旧的 `ApplySpriteWithBottomAlign()` 为 `[Obsolete]`

**修改文件**:
- `Assets/YYY_Scripts/World/Placeable/ChestController.cs` - 底部对齐机制修改

**验证**:
- Unity 编译通过 ✅
- 待用户测试验证

---

## 关键决策（更新）

| 决策 | 原因 | 日期 |
|------|------|------|
| 统一底部对齐机制 | 简化放置系统位置计算，避免特殊处理 | 2026-01-21 |
| 使用 localPosition.y 而非 position | 与 TreeControllerV2 保持一致，符合规范 | 2026-01-21 |
