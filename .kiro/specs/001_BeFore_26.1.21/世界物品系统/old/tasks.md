# 世界物品掉落系统 - 实现任务列表

**版本**: v1.0  
**日期**: 2025-12-17  
**状态**: ✅ 已完成

---

## Implementation Plan

- [x] 1. 更新 ItemData SO 结构


  - [x] 1.1 修改 ItemData.cs，重命名 worldSprite 为 bagSprite，添加 GetBagSprite() 方法


    - 添加 bagSprite 字段（背包/工具栏显示）
    - 保留 worldPrefab 字段
    - 移除旧的 worldSprite 字段
    - 实现 GetBagSprite() 回退逻辑
    - _Requirements: 2.1, 2.2_
  - [x] 1.2 编写属性测试：bagSprite回退逻辑


    - **Property 2: bagSprite回退逻辑**
    - **Validates: Requirements 2.2**

- [x] 2. 创建 WorldItemDrop 弹性动画组件


  - [x] 2.1 创建 WorldItemDrop.cs 核心逻辑


    - 实现 DropState 状态机（Bouncing, Idle, Paused）
    - 实现 StartDrop() 弹出动画
    - 实现 UpdateBouncing() 弹跳逻辑
    - 实现 UpdateIdle() 浮动逻辑
    - _Requirements: 3.1, 3.2, 3.3_
  - [x] 2.2 实现阴影同步逻辑

    - 阴影保持在地面位置
    - 阴影大小随物品高度变化
    - 浮动时阴影同步缩放
    - _Requirements: 4.2, 4.3_
  - [x] 2.3 编写属性测试：阴影高度同步

    - **Property 5: 阴影高度同步**
    - **Validates: Requirements 4.2**
  - [x] 2.4 实现距离检测暂停动画

    - 每0.5秒检测玩家距离
    - 超过阈值暂停动画
    - 进入范围恢复动画
    - _Requirements: 5.1, 5.2_
  - [x] 2.5 编写属性测试：距离动画暂停

    - **Property 6: 距离动画暂停**
    - **Validates: Requirements 5.1, 5.2**

- [x] 3. 创建 WorldItemPool 对象池


  - [x] 3.1 创建 WorldItemPool.cs 单例


    - 实现对象池初始化
    - 实现 Spawn() 和 Despawn() 方法
    - 实现池扩展逻辑
    - _Requirements: 5.3_
  - [x] 3.2 实现物品数量上限管理

    - 跟踪活跃物品数量
    - 超过上限时清理最旧物品
    - _Requirements: 5.4_
  - [x] 3.3 编写属性测试：物品数量上限

    - **Property 7: 物品数量上限**
    - **Validates: Requirements 5.4**

- [x] 4. Checkpoint - 确保所有测试通过


  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. 更新 WorldSpawnService


  - [x] 5.1 添加 SpawnWithAnimation() 方法

    - 使用对象池获取实例
    - 调用 WorldItemDrop.StartDrop()
    - 设置物品数据
    - _Requirements: 3.1, 6.1, 7.1_
  - [x] 5.2 添加 SpawnMultiple() 方法

    - 支持批量生成多个物品
    - 随机位置偏移
    - _Requirements: 3.4, 6.2_
  - [x] 5.3 编写属性测试：掉落位置随机分布

    - **Property 4: 掉落位置随机分布**
    - **Validates: Requirements 3.4**
  - [x] 5.4 实现 worldPrefab 回退逻辑

    - worldPrefab为空时使用默认模板
    - _Requirements: 2.3_
  - [x] 5.5 编写属性测试：worldPrefab回退逻辑

    - **Property 3: worldPrefab回退逻辑**
    - **Validates: Requirements 2.3**

- [x] 6. 更新 WorldItemPickup 组件


  - [x] 6.1 移除碰撞体依赖

    - 移除 Collider2D 相关代码
    - 使用距离检测替代碰撞检测拾取
    - _Requirements: 2.1_
  - [x] 6.2 集成 WorldItemDrop 组件

    - 添加对 WorldItemDrop 的引用
    - 拾取时停止动画
    - _Requirements: 3.1_

- [x] 7. 创建编辑器工具：World Prefab 生成器

  - [x] 7.1 创建 WorldPrefabGeneratorTool.cs 编辑器窗口

    - 菜单入口：Tools/World Item/批量生成 World Prefab
    - 支持选择多个 ItemData SO
    - _Requirements: 1.4_

  - [x] 7.2 实现 45度旋转 Sprite 生成算法
    - 读取 icon Sprite
    - 旋转45度并缩放到边界内
    - 保存为新的 Sprite 资源
    - _Requirements: 1.1, 1.2_

  - [x] 7.3 编写属性测试：Sprite旋转边界约束
    - **Property 1: Sprite旋转边界约束**
    - **Validates: Requirements 1.2**
  - [x] 7.4 实现 World Prefab 自动生成

    - 创建 Prefab 结构（Root + Sprite + Shadow）
    - 添加 WorldItemPickup 和 WorldItemDrop 组件
    - 自动赋值到 SO 的 worldPrefab 字段
    - _Requirements: 1.3_

  - [x] 7.5 创建默认阴影 Sprite 资源
    - 椭圆形阴影
    - 半透明黑色
    - _Requirements: 4.1_

- [x] 8. Checkpoint - 确保所有测试通过


  - Ensure all tests pass, ask the user if questions arise.

- [x] 9. 创建 DropTable 掉落配置系统



  - [x] 9.1 创建 DropConfig 和 DropTable 数据结构


    - 物品、数量范围、掉落概率
    - _Requirements: 7.2_
  - [x] 9.2 创建 ResourceNode 基类


    - 通用资源点逻辑（树木、矿石共用）
    - 掉落表配置
    - 生成掉落物品
    - _Requirements: 6.1, 7.1_

  - [x] 9.3 编写属性测试：掉落表物品生成
    - **Property 8: 掉落表物品生成**
    - **Validates: Requirements 7.2**
    - 已添加 DropTable_GenerateDrops_RespectsDropChance 等测试

- [x] 10. 集成砍树掉落


  - [x] 10.1 更新 TreeController 添加掉落逻辑

    - 配置 DropTable（木材、树枝等）
    - 砍倒时调用 WorldSpawnService.SpawnMultiple()
    - _Requirements: 6.1, 6.2, 6.3_

- [x] 11. 集成挖矿掉落

  - [x] 11.1 创建 RockController 组件

    - 矿石生命值
    - 挖掘进度
    - _Requirements: 7.1_
  - [x] 11.2 添加矿石掉落逻辑

    - 配置 DropTable（矿石、宝石等）
    - 挖掘完毕时调用 WorldSpawnService.SpawnMultiple()
    - _Requirements: 7.2, 7.3_

- [x] 11.5 集成 StoneController 差值掉落（与 stone-system 关联）
  - 实现差值掉落计算（当前阶段总量 - 下一阶段总量）
  - 支持材料等级限制（低等级镐子只能获得石料）
  - 调用 WorldSpawnService.SpawnMultiple() 生成掉落物
  - _Requirements: stone-system 4.1-4.5, 6.1-6.4, 8.1-8.10_

- [ ]* 11.6 编写属性测试：石头差值掉落正确性
  - **Property 9: 石头差值掉落正确性**
  - **Validates: stone-system Requirements 4.1-4.3**

- [ ]* 11.7 编写属性测试：材料等级限制掉落
  - **Property 10: 材料等级限制掉落**
  - **Validates: stone-system Requirements 8.1-8.10**

- [x] 12. 更新调试工具


  - [x] 12.1 更新 WorldSpawnDebug 支持动画生成

    - 添加 playAnimation 选项
    - 使用 SpawnWithAnimation() 方法
    - _Requirements: 8.1, 8.2, 8.3_

- [x] 13. 更新 UI 系统使用 bagSprite


  - [x] 13.1 更新 InventorySlotUI 使用 GetBagSprite()

    - 替换直接使用 icon 的代码
    - _Requirements: 2.2_

  - [x] 13.2 更新 ToolbarSlotUI 使用 GetBagSprite()
    - 已使用 `data.GetBagSprite()` 
    - _Requirements: 2.2_

  - [x] 13.3 更新 EquipmentSlotUI 使用 GetBagSprite()
    - 已使用 `data.GetBagSprite()`
    - UIItemIconScaler 是静态工具类，不需要修改
    - _Requirements: 2.2_

- [x] 14. Final Checkpoint - 确保所有测试通过

  - ✅ 所有 15 个测试通过
  - ✅ Property 1-8 全部验证完成
