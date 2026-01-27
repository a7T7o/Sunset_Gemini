# Implementation Plan

## 制作台 UI 系统 - 任务列表

- [x] 1. 创建制作服务核心类

  - [x] 1.1 创建 CraftingService.cs 服务类 ✅
    - 创建 `Assets/Scripts/Service/Crafting/` 目录
    - 定义 ItemDatabase 和 InventoryService 依赖字段
    - 定义 currentStation 字段存储当前设施类型
    - 定义事件：OnCraftSuccess、OnCraftFailed、OnRecipeListChanged
    - 实现 SetStation(CraftingStation station) 方法
    - _Requirements: 1.1, 4.1, 4.2, 4.3_
  - [x] 1.2 实现配方获取功能 ✅
    - 实现 GetAvailableRecipes() 方法
    - 从 ItemDatabase.allRecipes 过滤当前设施的配方
    - 过滤已解锁的配方
    - _Requirements: 1.1, 5.1, 5.2_
  - [ ] 1.3 Write property test for station filtering *
    - **Property 1: 设施配方过滤正确性**
    - **Validates: Requirements 1.1, 4.1, 4.2, 4.3**
  - [x] 1.4 实现材料检查功能 ✅
    - 实现 GetMaterialCount(int itemId) 方法
    - 遍历 InventoryService 所有槽位统计材料数量
    - 实现 CanCraft(RecipeData recipe) 方法
    - _Requirements: 2.1, 2.4_
  - [ ] 1.5 Write property test for material checking *
    - **Property 2: 材料检查一致性**
    - **Validates: Requirements 2.1, 2.4**
  - [x] 1.6 实现解锁检查功能 ✅
    - 实现 IsRecipeUnlocked(RecipeData recipe) 方法
    - 检查 unlockedByDefault 字段
    - 检查 requiredLevel 与玩家等级
    - _Requirements: 5.1, 5.2_
  - [x] 1.7 实现等级不足提示 ✅
    - ✅ 在 RecipeSlotUI 添加 levelRequirementText 字段
    - ✅ 当配方需要更高等级时显示"需要等级 X"提示
    - _Requirements: 5.4_
  - [ ] 1.8 Write property test for unlock checking *
    - **Property 5: 解锁状态过滤正确性**
    - **Property 6: 等级检查正确性**
    - **Validates: Requirements 5.1, 5.2, 5.4**

- [x] 2. Checkpoint - 确保制作服务核心功能正常 ✅
  - 代码编译通过，无错误

- [x] 3. 实现制作执行功能

  - [x] 3.1 实现 TryCraft(RecipeData recipe) 方法 ✅
    - 检查 CanCraft() 返回值
    - 检查背包空间是否足够
    - 扣除所需材料
    - 添加产物到背包
    - 触发 OnCraftSuccess 或 OnCraftFailed 事件
    - _Requirements: 3.1, 3.2, 3.4_
  - [ ] 3.2 Write property test for crafting execution *
    - **Property 3: 制作材料扣除正确性**
    - **Property 4: 制作产物添加正确性**
    - **Validates: Requirements 3.1, 3.2**
  - [x] 3.3 实现 CraftResult 结构体 ✅
    - 定义 success、message、resultItemId、resultAmount 字段
    - 定义 FailReason 枚举
    - _Requirements: 3.4_

- [x] 4. 创建 UI 组件

  - [x] 4.1 创建 MaterialItemUI.cs 组件 ✅
    - 创建 `Assets/Scripts/UI/Crafting/` 目录
    - 定义图标、名称、数量、状态图标引用
    - 实现 Setup(int itemId, int required, int owned, ItemDatabase database) 方法
    - 根据 owned >= required 设置颜色
    - _Requirements: 2.2, 2.3_
  - [x] 4.2 创建 RecipeSlotUI.cs 组件 ✅
    - 定义图标、名称、锁定遮罩引用
    - 实现 Setup(RecipeData recipe, CraftingPanel panel, bool unlocked) 方法
    - 实现点击事件处理
    - _Requirements: 1.3, 5.3_
  - [x] 4.3 创建 RecipeDetailPanel.cs 组件 ✅
    - 定义产物图标、名称、数量、材料列表容器引用
    - 实现 ShowRecipe(RecipeData recipe, CraftingService service) 方法
    - 动态生成 MaterialItemUI 列表
    - _Requirements: 1.4, 2.1_

- [x] 5. 创建制作面板

  - [x] 5.1 创建 CraftingPanel.cs 主面板 ✅
    - 定义配方列表容器、详情面板、制作按钮引用
    - 实现 Open(CraftingStation station) 方法
    - 实现 Close() 方法
    - _Requirements: 1.1_
  - [x] 5.2 实现配方列表刷新 ✅
    - 实现 RefreshRecipeList() 方法
    - 从 CraftingService 获取配方列表
    - 动态生成 RecipeSlotUI
    - _Requirements: 1.2_
  - [x] 5.3 实现配方选择功能 ✅
    - 实现 SelectRecipe(RecipeData recipe) 方法
    - 更新详情面板显示
    - 刷新材料状态
    - _Requirements: 1.3, 1.4_
  - [x] 5.4 实现制作按钮功能 ✅
    - 实现 OnCraftButtonClick() 方法
    - 调用 CraftingService.TryCraft()
    - 根据结果显示反馈
    - 刷新 UI 状态
    - _Requirements: 3.1, 3.2, 3.5_
  - [x] 5.5 实现制作成功反馈 ✅
    - ✅ 播放制作成功音效
    - ✅ 显示制作成功视觉效果（闪光淡出）
    - _Requirements: 3.3_
  - [x] 5.6 实现材料状态刷新 ✅
    - 实现 RefreshMaterialStatus() 方法
    - 更新制作按钮启用/禁用状态
    - _Requirements: 2.4, 2.5_

- [ ] 6. 创建 UI 预制体（需在 Unity 编辑器中手动创建）
  - [ ] 6.1 创建 MaterialItem.prefab
    - 创建 `Assets/Prefabs/UI/Crafting/` 目录
    - 设置图标、名称、数量文本布局
    - 挂载 MaterialItemUI 组件
    - _Requirements: 2.2, 2.3_
  - [ ] 6.2 创建 RecipeSlot.prefab
    - 设置图标、名称、锁定遮罩布局
    - 挂载 RecipeSlotUI 组件
    - _Requirements: 1.2_
  - [ ] 6.3 创建 CraftingPanel.prefab
    - 设置左侧配方列表、右侧详情面板布局
    - 挂载 CraftingPanel 和 RecipeDetailPanel 组件
    - 配置 CraftingService 引用
    - _Requirements: 1.2, 1.4_

- [ ] 7. Checkpoint - 确保制作台 UI 系统正常工作
  - 等待用户在 Unity 中创建预制体并测试
  - Ensure all tests pass, ask the user if questions arise.

## 8. 音效与视觉反馈

- [x] 8.1 添加制作音效支持 ✅
  - ✅ 在 CraftingPanel 添加 craftSuccessSound 和 craftFailSound 字段
  - ✅ 制作成功时播放成功音效
  - ✅ 制作失败时播放失败音效
  - _Requirements: 3.3_

- [x] 8.2 添加视觉反馈效果 ✅
  - ✅ 制作成功时播放闪光淡出效果
  - ✅ 添加 craftSuccessFlash、flashDuration、flashColor 配置
  - _Requirements: 3.3_

## 9. Final Checkpoint

- [ ] 9. Final Checkpoint - 完整功能验证
  - 所有核心代码已实现
  - 编译通过（0 错误）
  - 配方列表正确显示
  - 材料检查功能正常
  - 制作执行功能正常
  - 音效和视觉反馈正常
  - 等级提示正确显示
  - Ensure all tests pass, ask the user if questions arise.

---

**注**: 带 * 的任务为可选属性测试任务

