# 需求文档 - StoneController 优化

## 简介

优化 StoneController 的编辑器界面和 Sprite 管理系统，使其与 TreeControllerV2 保持一致的交互体验，支持动态参数调节和自动 Sprite 加载。

**参考文档**：
- `.kiro/specs/矿石系统/old/design.md` - 原始设计文档
- `.kiro/specs/矿石系统/old/requirements.md` - 原始需求文档
- `.kiro/specs/矿石系统/memory.md` - 开发记忆
- `.kiro/specs/树木系统/memory.md` - TreeControllerV2 参考实现

## 术语表

- **StoneController**: 石头/矿石控制器组件，管理石头的状态、血量、掉落等
- **OreType**: 矿物类型枚举
  - `None` = 纯石头（无矿物）
  - `C1` = 铜矿（需要生铁镐或更高）
  - `C2` = 铁矿（需要石镐或更高）
  - `C3` = 金矿（需要钢镐或更高）
- **StoneStage**: 石头阶段枚举
  - `M1` = 最大阶段，血量36，石料总量12
  - `M2` = 中等阶段，血量17，石料总量6
  - `M3` = 最小阶段，血量9，石料总量4，最终阶段
  - `M4` = 装饰石头，血量4，石料总量2，最终阶段
- **OreIndex**: 矿物含量指数
  - M1 阶段：0-4（对应 Sprite 序号 0-4）
  - M2 阶段：0-4（对应 Sprite 序号 0-4）
  - M3 阶段：0-3（对应 Sprite 序号 0-3）
  - M4 阶段：0-7（对应 Sprite 序号 0-7，装饰变体）
- **MaterialTier**: 材料等级（0=木，1=石，2=生铁，3=黄铜，4=钢，5=金）

## 核心数据配置（来自 memory.md）

### 血量配置表

| 阶段 | 血量 | 石料总量 | 是否最终阶段 | 下一阶段 |
|------|------|---------|-------------|---------|
| M1 | 36 | 12 | ✗ | M2 |
| M2 | 17 | 6 | ✗ | M3 |
| M3 | 9 | 4 | ✓ | - |
| M4 | 4 | 2 | ✓ | - |

### 矿物掉落数量表

| 阶段_含量 | 矿物总量 |
|----------|---------|
| M1_0 | 0 |
| M1_1 | 3 |
| M1_2 | 5 |
| M1_3 | 7 |
| M1_4 | 9 |
| M2_0 | 0 |
| M2_1 | 1 |
| M2_2 | 3 |
| M2_3 | 5 |
| M2_4 | 7 |
| M3_0 | 0 |
| M3_1 | 1 |
| M3_2 | 2 |
| M3_3 | 3 |

### 镐子采集能力表

| 镐子材质 | 材料等级 | 可获取矿物 |
|---------|---------|-----------|
| 木镐 | 0 | 无（只能获得石料） |
| 石镐 | 1 | 铁矿(C2) |
| 生铁镐 | 2 | 铁矿(C2)、铜矿(C1) |
| 黄铜镐 | 3 | 铁矿(C2)、铜矿(C1) |
| 钢镐 | 4 | 所有矿物 |
| 金镐 | 5 | 所有矿物 |

### Sprite 文件夹结构

```
Assets/Sprites/Stone/
├── C1/                    # 铜矿
│   ├── M1/               # M1 阶段
│   │   ├── Stone_C1_M1_0
│   │   ├── Stone_C1_M1_1
│   │   ├── Stone_C1_M1_2
│   │   ├── Stone_C1_M1_3
│   │   └── Stone_C1_M1_4
│   ├── M2/               # M2 阶段
│   │   ├── Stone_C1_M2_0
│   │   ├── Stone_C1_M2_1
│   │   ├── Stone_C1_M2_2
│   │   ├── Stone_C1_M2_3
│   │   └── Stone_C1_M2_4
│   ├── M3/               # M3 阶段
│   │   ├── Stone_C1_M3_0
│   │   ├── Stone_C1_M3_1
│   │   ├── Stone_C1_M3_2
│   │   └── Stone_C1_M3_3
│   └── M4/               # M4 阶段（装饰变体）
│       ├── Stone_C1_M4_0
│       ├── Stone_C1_M4_1
│       ├── ...
│       └── Stone_C1_M4_7
├── C2/                    # 铁矿（结构同上）
└── C3/                    # 金矿（结构同上）
```

## 需求

### 需求 1

**用户故事**: 作为开发者，我希望 StoneController 的 Inspector 界面清晰易用，参考 TreeControllerV2Editor 的分组折叠风格。

#### 验收标准

1. WHEN 开发者选中带有 StoneController 的 GameObject THEN StoneControllerEditor SHALL 显示分组折叠的 Inspector 界面
2. WHEN 开发者展开"阶段配置"分组 THEN StoneControllerEditor SHALL 固定显示 4 个阶段（M1-M4）的配置，每个阶段包含血量设置、掉落设置、阶段设置
3. WHEN 开发者展开"当前状态"分组 THEN StoneControllerEditor SHALL 显示当前阶段（下拉框）、矿物类型（下拉框）、含量指数（滑块）、当前血量（只读）
4. WHEN 开发者展开"Sprite 配置"分组 THEN StoneControllerEditor SHALL 显示 Sprite 渲染器引用、Sprite 根文件夹路径

### 需求 2

**用户故事**: 作为开发者，我希望通过三个参数（OreType、StoneStage、OreIndex）自动加载对应的 Sprite。

#### 验收标准

1. WHEN 开发者修改 OreType 参数 THEN StoneController SHALL 自动更新 Sprite 为 `Stone_{OreType}_{Stage}_{Index}` 格式的图片
2. WHEN 开发者修改 StoneStage 参数 THEN StoneController SHALL 自动更新 Sprite 并重置血量为该阶段默认值
3. WHEN 开发者修改 OreIndex 参数 THEN StoneController SHALL 自动更新 Sprite 显示
4. WHEN OreIndex 超出当前阶段允许范围（M1/M2: 0-4, M3: 0-3, M4: 0-7）THEN StoneController SHALL 自动钳制到有效范围

### 需求 3

**用户故事**: 作为开发者，我希望在运行时修改参数时能实时看到 Sprite 变化，参考 TreeControllerV2 的运行时调试功能。

#### 验收标准

1. WHEN 游戏运行中开发者在 Inspector 修改 OreType THEN StoneController SHALL 在 Update() 中检测变化并立即更新 Sprite
2. WHEN 游戏运行中开发者在 Inspector 修改 StoneStage THEN StoneController SHALL 立即更新 Sprite 并重置血量
3. WHEN 游戏运行中开发者在 Inspector 修改 OreIndex THEN StoneController SHALL 立即更新 Sprite
4. WHEN 参数变化导致 Sprite 不存在 THEN StoneController SHALL 在 Console 输出警告并保持当前 Sprite

### 需求 4

**用户故事**: 作为开发者，我希望能拖拽 Sprite 文件夹到 Inspector，自动扫描并缓存所有可用的 Sprite。

#### 验收标准

1. WHEN 开发者拖拽 Stone 根文件夹到 Inspector THEN StoneControllerEditor SHALL 自动扫描 C1/C2/C3 子文件夹
2. WHEN 扫描完成 THEN StoneControllerEditor SHALL 在 Inspector 显示"已加载 X 个 Sprite"统计信息
3. WHEN 文件夹结构为 Stone/{C}/{M}/Stone_{C}_{M}_{Num} THEN StoneController SHALL 正确解析并缓存 Sprite 引用
4. WHEN Sprite 缓存完成 THEN StoneController SHALL 使用缓存而非每次动态加载

### 需求 5

**用户故事**: 作为开发者，我希望 Inspector 显示当前石头的详细状态预览信息。

#### 验收标准

1. WHEN 开发者展开"状态预览"分组 THEN StoneControllerEditor SHALL 显示当前 Sprite 名称（如 `Stone_C1_M1_4`）
2. WHEN 开发者展开"状态预览"分组 THEN StoneControllerEditor SHALL 显示当前阶段血量上限和剩余血量（如 `36/36`）
3. WHEN 开发者展开"状态预览"分组 THEN StoneControllerEditor SHALL 显示预计掉落（如 `矿物: 9, 石料: 12`）
4. WHEN 开发者展开"状态预览"分组 THEN StoneControllerEditor SHALL 显示所需镐子等级（如 `需要: 生铁镐(2) 或更高`）

### 需求 6

**用户故事**: 作为开发者，我希望 OreIndex 的范围根据当前阶段自动调整。

#### 验收标准

1. WHEN 当前阶段为 M1 或 M2 THEN StoneControllerEditor SHALL 将 OreIndex 滑块范围设为 0-4
2. WHEN 当前阶段为 M3 THEN StoneControllerEditor SHALL 将 OreIndex 滑块范围设为 0-3
3. WHEN 当前阶段为 M4 THEN StoneControllerEditor SHALL 将 OreIndex 滑块范围设为 0-7
4. WHEN 阶段切换导致 OreIndex 超出新范围 THEN StoneController SHALL 自动钳制到新范围最大值

