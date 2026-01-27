# Implementation Plan

## P0 优先级 - 用户操作指南

- [x] 0. 生成 WorldPrefab（用户手动操作）
  - **这是物品可被拾取的前提条件！**
  - 打开菜单：`Tools → World Item → 批量生成 World Prefab`
  - 在 Project 窗口选择需要生成 WorldPrefab 的 ItemData 文件夹（如 `Assets/Data/Items/Materials`）
  - 点击"生成"按钮
  - 工具会自动：
    1. 生成 45度旋转的世界 Sprite
    2. 创建包含 WorldItemPickup、WorldItemDrop、SpriteRenderer、Shadow 的 Prefab
    3. 将生成的 Prefab 自动关联到 ItemData.worldPrefab 字段
  - **重要**：生成后需要手动为 Prefab 添加 Collider2D 和设置 Tag（见任务 1）

- [x] 1. 修复 WorldPrefab 拾取问题
  - [x] 1.1 修改 WorldPrefabGeneratorTool 自动添加 Collider2D
    - 在 GeneratePrefab 方法中添加 CircleCollider2D (Trigger)
    - 设置合适的半径（基于 Sprite 大小）
    - _Requirements: 5.2_
  - [x] 1.2 修改 WorldPrefabGeneratorTool 自动设置 Tag
    - 在 GeneratePrefab 方法中设置 root.tag = "Pickup"
    - _Requirements: 5.1_
  - [x] 1.3 修改 WorldItemPool 运行时创建的物体
    - 在 CreateDefaultPrefab 方法中添加 CircleCollider2D (Trigger)
    - 设置 Tag 为 "Pickup"
    - _Requirements: 5.1, 5.2_


- [x] 2. 简化 TreeControllerV2 掉落配置
  - [x] 2.1 添加新的掉落配置字段
    - 添加 dropItemData (ItemData) 字段
    - 添加 stageDropAmounts[6] 数组
    - 添加 stumpDropAmounts[6] 数组
    - _Requirements: 1.1, 1.2_
  - [x] 2.2 修改 SpawnDrops 方法
    - 使用新的配置生成掉落物
    - 移除对 DropTable 的依赖
    - _Requirements: 1.3_
  - [x] 2.3 修改 SpawnStumpDrops 方法
    - 使用新的配置生成树桩掉落物
    - _Requirements: 1.3_
  - [x] 2.4 更新 TreeControllerV2Editor
    - 添加新掉落配置的 UI 绘制
    - 隐藏旧的 DropTable 字段
    - _Requirements: 1.1, 1.2_

- [x] 3. 优化掉落物分散生成
  - [x] 3.1 修改 WorldSpawnService.SpawnMultiple 方法
    - 改进位置分散算法，确保物品不完全重叠
    - 使用树根 Collider 范围作为分散区域
    - _Requirements: 2.1, 2.2, 2.3_
  - [x] 3.2 修改 TreeControllerV2 传递 Collider 范围
    - 获取树根 Collider 的 bounds
    - 传递给 SpawnMultiple 方法
    - _Requirements: 2.1_

- [x] 4. 添加拾取飞向玩家动画
  - [x] 4.1 在 WorldItemPickup 中添加飞向动画
    - 添加 FlyToPlayer 方法
    - 添加 IsFlying 状态标记
    - 实现平滑移动协程
    - _Requirements: 4.2, 4.3_
  - [x] 4.2 修改 AutoPickupService 触发飞向动画
    - 检测到物品时触发 FlyToPlayer
    - 动画完成后执行 TryPickup
    - _Requirements: 4.1_

- [x] 5. 编译验证
  - 确保所有代码编译通过
  - 在 Unity 中测试功能

- [x] 6. 修复背包满时物品仍被吸引的问题





  - [ ] 6.1 在 InventoryService 中添加 CanAddItem 方法
    - 添加检查背包是否有空间的方法


    - 不实际添加物品，只检查空间
    - _Requirements: 6.1_
  - [ ] 6.2 修改 AutoPickupService 在吸引前检查背包空间
    - 在触发 FlyToPlayer 前调用 CanAddItem 检查
    - 背包满时跳过该物品，物品保持原位
    - _Requirements: 6.1, 6.2, 6.3, 6.4_
