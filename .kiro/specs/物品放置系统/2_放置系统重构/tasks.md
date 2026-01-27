# 实现计划 - 物品放置系统重构

- [x] 1. 创建基础工具类

  - [x] 1.1 创建 PlacementGridCalculator.cs - 方块中心和格子计算
    - ✅ 实现 GetCellCenter() 方法：Floor(pos) + 0.5
    - ✅ 实现 GetRequiredGridSize() 方法：基于 Collider 向上取整
    - ✅ 实现 GetOccupiedCellCenters() 方法：获取所有占用格子
    - ✅ 实现 GetOccupiedCellIndices() 方法：获取所有格子索引
    - _需求: 3.1, 3.2, 7.3_

  - [x] 1.2 创建 PlacementLayerDetector.cs - Layer 检测
    - ✅ 实现 GetLayerAtPosition() 方法：Raycast 检测地面 Layer
    - ✅ 实现 GetPlayerLayer() 方法：获取玩家 Layer
    - ✅ 实现 IsLayerMatch() 方法：比较两个 Layer
    - ✅ 实现 SyncLayerToAllChildren() 方法：同步 Layer 到所有子物体
    - ✅ 实现 SyncSortingLayerToAllRenderers() 方法：同步 Sorting Layer
    - _需求: 1.1, 5.1, 5.2_

- [x] 2. 重构预览 UI 系统

  - [x] 2.1 创建 PlacementGridCell.cs - 单个格子渲染组件
    - ✅ 创建格子 Sprite（1x1 单位大小，动态生成）
    - ✅ 实现 SetValid() 方法：设置绿色/红色
    - ✅ 配置 Sorting Layer 为 Effects
    - ✅ 实现对象池支持（Hide/Show）
    - _需求: 7.4, 7.5, 7.7_

  - [x] 2.2 创建 PlacementPreviewV2.cs - 预览 UI 系统
    - ✅ 添加格子池管理（对象池模式）
    - ✅ 实现 CreateGridCells() 方法：显示格子方框
    - ✅ 实现 UpdateCellStates() 方法：更新格子颜色
    - ✅ 实现物品半透明预览显示
    - ✅ 实现 UpdatePosition() 使用方块中心
    - _需求: 7.1, 7.2, 7.6_

- [x] 3. 重构验证器

  - [x] 3.1 扩展 PlacementValidationResult - 添加新字段
    - ✅ 添加 CellStates 列表
    - ✅ 添加 DetectedLayer 字段
    - ✅ 添加 WithCellStates() 静态方法
    - _需求: 4.3, 8.2_

  - [x] 3.2 创建 PlacementValidatorV2.cs - 完整验证逻辑
    - ✅ 实现 ValidateFull() 方法：完整验证
    - ✅ 实现 DetectCellCollisions() 方法：格子碰撞检测
    - ✅ 实现 Layer 一致性检测（IsPlacementLayerValid）
    - ✅ 实现 AlignToGrid() 使用方块中心
    - _需求: 4.1, 4.2, 5.2, 8.1, 8.3_

- [x] 4. 重构放置管理器

  - [x] 4.1 创建 PlacementManagerV2.cs - 预览更新逻辑
    - ✅ 实现 UpdatePreview() 使用方块中心
    - ✅ 集成 PlacementGridCalculator
    - ✅ 集成格子状态更新
    - _需求: 3.3_

  - [x] 4.2 PlacementManagerV2.cs - Layer 同步逻辑
    - ✅ 在 ExecutePlacement() 中获取点击位置 Layer
    - ✅ 实现 SyncLayerToPlacedObject() 方法
    - ✅ 同步父物体、Tree、Shadow 的 Layer
    - ✅ 同步 Sorting Layer 到所有 SpriteRenderer
    - _需求: 1.2, 1.3, 1.4_

  - [x] 4.3 PlacementManagerV2.cs - Order 计算逻辑
    - ✅ 实现 SetSortingOrder() 方法
    - ✅ 公式：Order = -Round(Y × multiplier) + offset
    - ✅ Shadow 的 Order = Tree.Order - 1
    - _需求: 2.1, 2.2, 2.3, 2.4_

  - [x] 4.4 PlacementManagerV2.cs - 碰撞阻止逻辑
    - ✅ 在 TryPlace() 中检查验证结果
    - ✅ 存在红色格子时阻止放置
    - _需求: 8.4, 8.5_

- [x] 5. 实现导航放置功能

  - [x] 5.1 添加导航放置逻辑
    - ✅ 检测放置位置是否超出范围
    - ✅ 调用 PlayerAutoNavigator 导航到目标附近
    - ✅ 实现 WaitForNavigationComplete 协程
    - ✅ 到达后自动执行放置
    - ✅ 支持取消导航（CancelNavigationPlacement）
    - _需求: 6.1, 6.2, 6.3_

- [x] 6. 创建格子预制体和资源

  - [x] 6.1 创建格子 Sprite 资源
    - ✅ 动态生成 1x1 单位的方框 Sprite（CreateGridSprite）
    - ✅ 配置为可着色（白色基础）
    - ✅ 边框 + 半透明填充
    - _需求: 7.7_

- [x] 7. 最终检查点
  - ✅ 所有代码编译通过（0 errors, 0 warnings）
  - ⏳ 等待用户测试验证

