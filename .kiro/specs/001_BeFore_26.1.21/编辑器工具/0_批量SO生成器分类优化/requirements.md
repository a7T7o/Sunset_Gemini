# 需求文档 - 批量 SO 生成器分类优化

## 介绍

本文档定义批量物品 SO 生成器的分类系统优化需求，将现有的扁平按钮布局改为大类+小类的层级结构，以支持更多物品类型并提升用户体验。

## 术语表

- **Tool_BatchItemSOGenerator**: 批量生成物品 SO 的编辑器工具
- **大类（MainCategory）**: 物品的顶级分类，如工具装备、种植类、可放置等
- **小类（SubType）**: 大类下的具体 SO 类型，如工具、武器、树苗、工作台等
- **ItemSOType**: 物品 SO 类型枚举，对应实际的数据类
- **PlaceableItemData**: 可放置物品数据基类

---

## 需求 1：大类分类系统

**用户故事**: 作为策划，我希望物品类型按大类组织，以便快速定位到需要的物品类别。

### 验收标准

1. WHEN 打开批量生成工具 THEN 系统 SHALL 显示 6 个大类按钮：工具装备、种植类、可放置、消耗品、材料、其他
2. WHEN 用户点击大类按钮 THEN 系统 SHALL 高亮选中的大类并更新小类选项
3. WHEN 工具启动时 THEN 系统 SHALL 默认选中"其他"大类和"基础物品"小类

---

## 需求 2：小类动态显示

**用户故事**: 作为策划，我希望根据选中的大类显示对应的小类选项，以便精确选择物品类型。

### 验收标准

1. WHEN 选中"工具装备"大类 THEN 系统 SHALL 显示小类：工具、武器
2. WHEN 选中"种植类"大类 THEN 系统 SHALL 显示小类：种子、作物
3. WHEN 选中"可放置"大类 THEN 系统 SHALL 显示小类：树苗、工作台、存储、交互展示、简单事件
4. WHEN 选中"消耗品"大类 THEN 系统 SHALL 显示小类：食物、药水
5. WHEN 选中"材料"大类 THEN 系统 SHALL 显示小类：矿石、锭、自然材料、怪物掉落
6. WHEN 选中"其他"大类 THEN 系统 SHALL 显示小类：基础物品、家具、特殊物品

---

## 需求 3：ID 范围自动设置

**用户故事**: 作为策划，我希望选择物品类型后自动设置推荐的 ID 范围，减少手动配置。

### 验收标准

1. WHEN 选择工具小类 THEN 系统 SHALL 设置起始 ID 为 0（农具 00XX）
2. WHEN 选择武器小类 THEN 系统 SHALL 设置起始 ID 为 200
3. WHEN 选择种子小类 THEN 系统 SHALL 设置起始 ID 为 1000
4. WHEN 选择作物小类 THEN 系统 SHALL 设置起始 ID 为 1100
5. WHEN 选择树苗小类 THEN 系统 SHALL 设置起始 ID 为 1200
6. WHEN 选择工作台小类 THEN 系统 SHALL 设置起始 ID 为 1300
7. WHEN 选择存储小类 THEN 系统 SHALL 设置起始 ID 为 1400
8. WHEN 选择交互展示小类 THEN 系统 SHALL 设置起始 ID 为 1500
9. WHEN 选择简单事件小类 THEN 系统 SHALL 设置起始 ID 为 1600
10. WHEN 选择食物小类 THEN 系统 SHALL 设置起始 ID 为 5000
11. WHEN 选择药水小类 THEN 系统 SHALL 设置起始 ID 为 4000
12. WHEN 选择材料子类 THEN 系统 SHALL 根据子类设置对应 ID（矿石 3000、锭 3100、自然 3200、怪物 3300）

---

## 需求 4：输出路径自动设置

**用户故事**: 作为策划，我希望选择物品类型后自动设置推荐的输出路径，保持文件组织一致性。

### 验收标准

1. WHEN 选择可放置大类下的任意小类 THEN 系统 SHALL 设置输出路径为 Assets/111_Data/Items/Placeable/{小类名}
2. WHEN 选择工具装备大类下的小类 THEN 系统 SHALL 设置输出路径为 Assets/111_Data/Items/{Tools|Weapons}
3. WHEN 选择种植类大类下的小类 THEN 系统 SHALL 设置输出路径为 Assets/111_Data/Items/{Seeds|Crops}
4. WHEN 选择消耗品大类下的小类 THEN 系统 SHALL 设置输出路径为 Assets/111_Data/Items/{Foods|Potions}
5. WHEN 选择材料大类 THEN 系统 SHALL 设置输出路径为 Assets/111_Data/Items/Materials

---

## 需求 5：新增可放置物品类型支持

**用户故事**: 作为策划，我希望批量生成工作台、存储容器等可放置物品 SO。

### 验收标准

1. WHEN 选择工作台小类 THEN 系统 SHALL 显示工作台专属设置：工作台类型、是否需要燃料
2. WHEN 选择存储小类 THEN 系统 SHALL 显示存储专属设置：存储容量、是否可上锁
3. WHEN 选择交互展示小类 THEN 系统 SHALL 显示交互展示专属设置：显示标题、显示内容、显示时长
4. WHEN 选择简单事件小类 THEN 系统 SHALL 显示简单事件专属设置：事件类型、是否一次性、冷却时间
5. WHEN 生成可放置物品 SO THEN 系统 SHALL 自动设置 isPlaceable = true

---

## 需求 6：UI 布局优化

**用户故事**: 作为策划，我希望工具界面清晰易用，即使物品类型增多也不会拥挤。

### 验收标准

1. WHEN 显示物品类型区域 THEN 系统 SHALL 使用两行布局：第一行大类按钮，第二行小类按钮
2. WHEN 大类按钮数量超过 6 个 THEN 系统 SHALL 自动换行显示
3. WHEN 小类按钮数量超过 5 个 THEN 系统 SHALL 自动换行显示
4. WHEN 显示类型说明 THEN 系统 SHALL 显示当前选中的大类+小类组合及 ID 范围

---

## 需求 7：设置持久化

**用户故事**: 作为策划，我希望工具记住上次的选择，下次打开时自动恢复。

### 验收标准

1. WHEN 关闭工具窗口 THEN 系统 SHALL 保存当前选中的大类和小类
2. WHEN 重新打开工具窗口 THEN 系统 SHALL 恢复上次选中的大类和小类
3. WHEN 切换小类 THEN 系统 SHALL 保存该大类下最后选中的小类

