# 实现计划

- [x] 1. 创建枚举定义




  - [x] 1.1 创建 ChestMaterial 枚举（Wood/Iron/Special）

    - 在 PlacementEnums.cs 中添加
    - _Requirements: 2.1_





  - [x] 1.2 创建 ChestOwnership 枚举（Player/World）

    - 在 PlacementEnums.cs 中添加
    - _Requirements: 7.4_

- [x] 2. 扩展 StorageData SO






  - [x] 2.1 修改 storageCols 范围为 [1,15]


    - _Requirements: 1.1_



  - [x] 2.2 添加 chestMaterial 字段

    - _Requirements: 2.1_
  - [x] 2.3 添加 maxHealth 字段

    - _Requirements: 2.2_



  - [x]* 2.4 编写属性测试：容量计算一致性


    - **Property 1: 容量计算一致性**
    - **Validates: Requirements 1.2**

- [x] 3. 创建 ChestController 核心逻辑

  - [x] 3.1 创建 ChestController.cs 基础结构

    - 字段：storageData, currentHealth, ownership, isLocked, contents
    - 初始化逻辑
    - _Requirements: 4.1, 4.2_
  - [x] 3.2 实现 OnHit 受击处理



    - 检查工具类型是否为镐子
    - 检查箱子是否为空
    - 空箱子：造成伤害，血量归零则销毁
    - 有物品：触发推动逻辑
    - _Requirements: 4.1, 4.2, 4.3, 4.4_
  - [ ]* 3.3 编写属性测试：镐子伤害传递
    - **Property 2: 镐子伤害传递**
    - **Validates: Requirements 4.1**



  - [ ]* 3.4 编写属性测试：非镐子工具无伤害
    - **Property 3: 非镐子工具无伤害**
    - **Validates: Requirements 4.3**


  - [x]* 3.5 编写属性测试：有物品箱子不受伤害

    - **Property 4: 有物品箱子不受伤害**
    - **Validates: Requirements 4.4**

- [x] 4. 实现箱子推动逻辑



  - [x] 4.1 实现 TryPush 方法



    - 计算目标位置（当前位置 + 玩家朝向 × 1单位）


    - 碰撞检测（Physics2D.OverlapCircle）
    - 有碰撞体则取消推动
    - 无碰撞体则播放跳动动画并移动
    - _Requirements: 5.1, 5.2, 5.3, 5.4_
  - [ ]* 4.2 编写属性测试：推动碰撞阻挡
    - **Property 5: 推动碰撞阻挡**
    - **Validates: Requirements 5.3**

- [x] 5. 实现上锁/解锁系统


  - [x] 5.1 实现 TryLock 方法

    - 检查箱子是否已上锁

    - 检查材质匹配
    - 成功则标记上锁并设置归属为玩家








    - _Requirements: 6.1, 6.2, 6.3, 6.4_
  - [x] 5.2 实现 TryUnlock 方法


    - 检查箱子是否上锁且归属为世界
    - 检查材质匹配

    - 成功则设置归属为玩家

    - _Requirements: 7.1, 7.2_
  - [x] 5.3 实现 TryOpen 方法







    - 检查归属和上锁状态
    - 玩家归属直接打开







    - 世界归属+上锁则提示需要钥匙
    - _Requirements: 7.3, 7.4_

  - [ ]* 5.4 编写属性测试：材质匹配上锁
    - **Property 6: 材质匹配上锁**



    - **Validates: Requirements 6.1, 6.2**
  - [ ]* 5.5 编写属性测试：材质匹配解锁
    - **Property 7: 材质匹配解锁**
    - **Validates: Requirements 7.1, 7.2**
  - [ ]* 5.6 编写属性测试：玩家箱子直接打开
    - **Property 8: 玩家箱子直接打开**
    - **Validates: Requirements 7.4**

- [x] 6. Checkpoint - 确保所有测试通过

  - 确保所有测试通过，如有问题请询问用户。

- [x] 7. 实现箱子掉落自动放置


  - [x] 7.1 创建 ChestDropHandler.cs


    - 静态方法 HandleChestDrop
    - 螺旋搜索算法 FindEmptyPosition
    - _Requirements: 3.1, 3.2, 3.5_
  - [x] 7.2 修改 WorldItemPickup/WorldSpawnService 支持自动放置类型


    - 检测物品是否为 Storage 类型
    - 掉落动画结束后调用 ChestDropHandler
    - _Requirements: 3.3, 3.4_
  - [ ]* 7.3 编写属性测试：掉落过程不可拾取
    - **Property 10: 掉落过程不可拾取**
    - **Validates: Requirements 3.4**

- [x] 8. 实现箱子UI面板


  - [x] 8.1 创建 BoxPanelUI.cs 基础结构


    - 引用 Up/Down 区域
    - Open/Close 方法
    - _Requirements: 10.1, 10.2_
  - [x] 8.2 实现 UI 预制体加载逻辑


    - 根据箱子容量加载对应预制体
    - _Requirements: 10.3_
  - [x] 8.3 实现内容物保存逻辑

    - 关闭时同步数据到 ChestController
    - _Requirements: 10.4_

- [x] 9. 实现面板互斥切换


  - [x] 9.1 扩展 GameInputManager 支持 Box 面板


    - 添加 Box 面板引用
    - 修改快捷键处理逻辑
    - _Requirements: 9.3, 9.4_
  - [x] 9.2 实现 PackagePanel 和 Box 互斥显示

    - 打开一个时关闭另一个
    - _Requirements: 9.1, 9.2_
  - [ ]* 9.3 编写属性测试：UI面板互斥
    - **Property 9: UI面板互斥**
    - **Validates: Requirements 9.1, 9.2**

- [x] 10. 实现箱子右键交互


  - [x] 10.1 扩展 GameInputManager 处理箱子右键


    - 检测点击目标是否为箱子
    - 检查手持物品类型（锁/钥匙/其他）
    - 调用对应的 ChestController 方法
    - _Requirements: 6.1, 7.1, 8.1, 8.3_
  - [x] 10.2 实现自动导航到箱子

    - 距离大于交互距离时触发导航
    - 到达后自动打开
    - _Requirements: 8.1, 8.2, 8.4_

- [x] 11. Final Checkpoint - 确保所有测试通过


  - 确保所有测试通过，如有问题请询问用户。
