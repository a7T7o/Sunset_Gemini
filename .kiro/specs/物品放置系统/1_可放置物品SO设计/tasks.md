# 实现计划 - 可放置物品 SO 设计

## 阶段 1：基础架构

- [x] 1. 创建可放置物品基类




  - [ ] 1.1 创建 PlaceableItemData.cs 继承 ItemData
    - 添加 placementPrefab、placementOffset、canRotate、rotationStep 字段


    - 实现 OnValidate 自动设置 isPlaceable = true




    - _Requirements: 2.1, 2.2, 2.3_
  - [ ] 1.2 创建 PlaceableItemDataEditor.cs 自定义编辑器
    - 隐藏父类的 isPlaceable、placementType、buildingSize 字段




    - _Requirements: 1.5, 2.3_



- [ ] 2. 扩展放置类型枚举
  - [x] 2.1 修改 PlacementEnums.cs 添加新类型


    - 添加 Workstation = 5, Storage = 6, InteractiveDisplay = 7, SimpleEvent = 8
    - _Requirements: 7.1_





## 阶段 2：树苗 SO 优化

- [x] 3. 优化 SaplingData




  - [x] 3.1 修改 SaplingData.cs 继承 PlaceableItemData


    - 移除 plantableSeason 字段
    - 保留 treePrefab、plantingExp、heldSprite 字段
    - _Requirements: 1.1, 1.5_
  - [ ] 3.2 实现季节自动处理逻辑
    - 添加 GetSeasonalStyleIndex() 方法
    - 处理过渡季节随机选择
    - _Requirements: 1.2, 1.3_




  - [ ] 3.3 创建 SaplingDataEditor.cs 自定义编辑器
    - 隐藏通用放置配置字段
    - 只显示 treePrefab、plantingExp、heldSprite
    - _Requirements: 1.5_

- [ ] 4. 实现冬季种植阻止
  - [x] 4.1 修改 PlacementManager.cs 添加冬季检测




    - 在 TryPlace() 中检测当前季节
    - 冬季时输出控制台提示并返回失败
    - _Requirements: 1.4_





## 阶段 3：工作台类型



- [ ] 5. 创建工作台数据类
  - [x] 5.1 创建 WorkstationType 枚举




    - 定义 Heating、Smelting、Pharmacy、Sawmill、Cooking、Crafting
    - _Requirements: 3.2_
  - [ ] 5.2 创建 WorkstationData.cs 继承 PlaceableItemData
    - 添加 workstationType、recipeListRef、craftTimeMultiplier 字段
    - 添加 requiresFuel、fuelSlotCount、acceptedFuelTypes 字段
    - _Requirements: 3.1, 3.2_
  - [x]* 5.3 创建 WorkstationDataEditor.cs 自定义编辑器



    - 根据 requiresFuel 显示/隐藏燃料相关字段
    - _Requirements: 3.2_

## 阶段 4：存储容器类型

- [ ] 6. 创建存储容器数据类
  - [ ] 6.1 创建 StorageData.cs 继承 PlaceableItemData
    - 添加 storageCapacity、storageRows、storageCols 字段
    - 添加 allowedCategories、isLockable、defaultLocked 字段
    - _Requirements: 4.1, 4.2_
  - [ ]* 6.2 创建 StorageDataEditor.cs 自定义编辑器
    - 根据 isLockable 显示/隐藏锁定相关字段
    - _Requirements: 4.2_

## 阶段 5：交互展示类型

- [ ] 7. 创建交互展示数据类
  - [ ] 7.1 创建 InteractiveDisplayData.cs 继承 PlaceableItemData
    - 添加 displayTitle、displayContent、displayImage 字段
    - 添加 displayDuration、closeOnInteract 字段
    - _Requirements: 5.1, 5.2_

## 阶段 6：简单事件类型

- [ ] 8. 创建简单事件数据类
  - [ ] 8.1 创建 SimpleEventType 枚举
    - 定义 PlaySound、PlayAnimation、Teleport、Unlock、GiveItem、TriggerQuest、ShowMessage
    - _Requirements: 6.2_
  - [ ] 8.2 创建 SimpleEventData.cs 继承 PlaceableItemData
    - 添加 eventType、eventParameter、isOneTime、cooldownTime、triggerSound 字段
    - _Requirements: 6.1, 6.2_

## 阶段 7：编辑器菜单组织

- [ ] 9. 更新 Create 菜单
  - [ ] 9.1 更新所有 SO 的 CreateAssetMenu 属性
    - SaplingData: "Farm/Placeable/Sapling"
    - WorkstationData: "Farm/Placeable/Workstation"
    - StorageData: "Farm/Placeable/Storage"
    - InteractiveDisplayData: "Farm/Placeable/Interactive Display"
    - SimpleEventData: "Farm/Placeable/Simple Event"
    - _Requirements: 8.1, 8.2_

## 阶段 8：检查点

- [ ] 10. 最终检查
  - [ ] 确保所有代码编译通过
  - [ ] 确保 Create 菜单正确显示
  - [ ] 确保树苗 SO 只显示必要字段

---

## 优先级说明

**高优先级**（本次实现）：
- 阶段 1-2：基础架构和树苗优化
- 阶段 7：编辑器菜单

**中优先级**（后续实现）：
- 阶段 3-6：各类可放置物品

**标记 * 的任务为可选**：
- 自定义编辑器可后续完善
