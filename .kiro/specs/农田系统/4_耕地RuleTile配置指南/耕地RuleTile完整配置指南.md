# 耕地 Rule Tile 完整配置指南

**创建日期**: 2026-01-25  
**适用版本**: Unity 2022.3+  
**前置条件**: 已有耕地 Sprite 资源

---

## 📦 你的资源清单

根据你提供的图片，你目前拥有：

### 耕地 Sprite（5个 → 需要 6个）

**现有资源**：
- **上边界** - 1个（只有上边有框）
- **下边界** - 1个（只有下边有框）
- **左边界** - 1个（只有左边有框）
- **右边界** - 1个（只有右边有框）
- **中心样式** - 1个（没有边框）

**缺少资源**：
- **完整边界** - 需要手动合成（四周都有边框）

> **重要**：你需要先制作"完整边界" Sprite，才能继续配置 Rule Tile！  
> 请参考"步骤 1.5：制作完整边界 Sprite"。

### 水渍 Sprite（3个）
- **水渍样式 A** - 1个
- **水渍样式 B** - 1个
- **水渍样式 C** - 1个

---

## 🎯 目标效果

配置完成后，耕地会自动根据周围情况显示不同样式：

| 场景 | 显示效果 |
|------|---------|
| 单独一格耕地 | 显示完整边界（上下左右都是边界） |
| 相邻有耕地 | 自动切换为中心样式 |
| 边缘位置 | 显示对应方向的边界 |

---

## 📝 步骤 1：创建 Rule Tile

### 1.1 创建 Rule Tile 资产

1. 在 Project 窗口中，导航到 `Assets/ZZZ_999_Package/000_Tile/`
2. 右键 → **Create** → **2D** → **Tiles** → **Rule Tile**
3. 命名为 `Farmland_RuleTile`

### 1.2 设置默认 Sprite

1. 选中 `Farmland_RuleTile`
2. 在 Inspector 中，找到 **Default Sprite** 字段
3. 将**中心样式** Sprite 拖入该字段

> **为什么选中心样式？**  
> 因为大部分耕地都是连在一起的，中心样式是最常见的情况。

---

## 📝 步骤 1.5：制作"完整边界" Sprite（必须）

### 问题说明

你目前有 5 个 Sprite：
- 上边界（只有上边有框）
- 下边界（只有下边有框）
- 左边界（只有左边有框）
- 右边界（只有右边有框）
- 中心样式（没有边框）

**但是**，当一格耕地孤立存在时，你需要显示**四周都有边框**的 Sprite，而你目前没有这个 Sprite！

### 解决方案：手动合成

#### 方法 A：使用 Photoshop/GIMP

1. 打开你的耕地 Sprite 图集
2. 创建一个新图层
3. 将上、下、左、右 4 个边界 Sprite 叠加在一起：
   - 上边界放在上方
   - 下边界放在下方
   - 左边界放在左侧
   - 右边界放在右侧
   - 中心样式放在中间
4. 合并图层
5. 导出为新的 Sprite，命名为 `Farmland_Complete_Border`

#### 方法 B：使用 Unity Sprite Editor

1. 在 Unity 中，找到你的耕地 Sprite 图集
2. 使用 Sprite Editor 切割出一个新的区域
3. 确保这个区域包含上下左右 4 个边框
4. 命名为 `Farmland_Complete_Border`

#### 方法 C：请美术提供

如果你有美术同事，直接让他们提供一个"四周都有边框"的耕地 Sprite。

### 验证

完成后，你应该有 **6 个**耕地 Sprite：
- 上边界
- 下边界
- 左边界
- 右边界
- 中心样式
- **完整边界**（新增）

---

## 📝 步骤 2：配置规则（核心）

### 2.1 规则配置原理

Rule Tile 通过检测周围 8 个方向的邻居来决定显示哪个 Sprite：

```
[左上] [上] [右上]
[左]  [中心] [右]
[左下] [下] [右下]
```

每个方向可以设置 3 种状态：
- **绿色箭头** = 必须有邻居
- **红色叉** = 必须没有邻居
- **空白** = 不关心（有没有都行）

---

### 2.2 规则 1：孤立耕地（完整边界）

**目标**：当一格耕地周围都没有其他耕地时，显示完整边界（四周都有边框的方块）。

#### 你的资源情况

你有 5 个耕地 Sprite：
- 上边界（只有上边有框）
- 下边界（只有下边有框）
- 左边界（只有左边有框）
- 右边界（只有右边有框）
- 中心样式（没有边框）

**问题**：你没有一个"四周都有边框"的完整边界 Sprite！

#### 解决方案 A：使用 Animated Tile（推荐）

Unity 的 Rule Tile **无法在一个格子里同时显示多个 Sprite**（上下左右边界叠加）。

**你需要做的**：
1. 在 Photoshop/其他图像编辑软件中，手动合成一个"完整边界" Sprite
2. 将上、下、左、右 4 个边界 Sprite 叠加在一起
3. 导出为一个新的 Sprite，命名为 `Farmland_Complete_Border`

#### 解决方案 B：使用代码控制（不推荐）

如果你不想手动合成 Sprite，可以用代码在运行时动态叠加 4 个边界 Sprite，但这会增加复杂度。

---

### 假设你已经有了"完整边界" Sprite

#### 配置步骤

1. 点击 Inspector 底部的 **+** 按钮，添加第一条规则
2. 在规则的 **Tiling Rules** 区域，点击 9 宫格的 **上、下、左、右** 4 个位置
3. 每点击一次，会循环切换状态：空白 → 绿色箭头 → 红色叉 → 空白
4. 将 **上、下、左、右** 都设置为 **红色叉**（表示这 4 个方向都没有邻居）
5. **左上、右上、左下、右下** 保持空白（不关心）

#### 最终效果

```
[  ] [X] [  ]
[X] [中心] [X]
[  ] [X] [  ]
```

#### 设置 Sprite

1. 在该规则下方，找到 **Output** 区域
2. 将 **Output** 设置为 **Single**
3. 将你合成的**完整边界 Sprite**（`Farmland_Complete_Border`）拖入 **Sprite** 字段

> **重要**：这个 Sprite 必须是四周都有边框的，不是单独的上/下/左/右边界。

---

### 2.3 规则 2：上边界

**目标**：当上方没有邻居，下方有邻居时，显示上边界。

#### 配置步骤

1. 点击 **+** 添加第二条规则
2. 设置 **上** = 红色叉（没有邻居）
3. 设置 **下** = 绿色箭头（有邻居）
4. **左、右** 保持空白（不关心）

#### 最终效果

```
[  ] [X] [  ]
[  ] [中心] [  ]
[  ] [↓] [  ]
```

#### 设置 Sprite

将你的**上边界 Sprite** 拖入 **Sprite** 字段。

---

### 2.4 规则 3：下边界

**目标**：当下方没有邻居，上方有邻居时，显示下边界。

#### 配置步骤

1. 点击 **+** 添加第三条规则
2. 设置 **下** = 红色叉
3. 设置 **上** = 绿色箭头
4. **左、右** 保持空白

#### 最终效果

```
[  ] [↑] [  ]
[  ] [中心] [  ]
[  ] [X] [  ]
```

#### 设置 Sprite

将你的**下边界 Sprite** 拖入 **Sprite** 字段。

---

### 2.5 规则 4：左边界

**目标**：当左方没有邻居，右方有邻居时，显示左边界。

#### 配置步骤

1. 点击 **+** 添加第四条规则
2. 设置 **左** = 红色叉
3. 设置 **右** = 绿色箭头
4. **上、下** 保持空白

#### 最终效果

```
[  ] [  ] [  ]
[X] [中心] [→]
[  ] [  ] [  ]
```

#### 设置 Sprite

将你的**左边界 Sprite** 拖入 **Sprite** 字段。

---

### 2.6 规则 5：右边界

**目标**：当右方没有邻居，左方有邻居时，显示右边界。

#### 配置步骤

1. 点击 **+** 添加第五条规则
2. 设置 **右** = 红色叉
3. 设置 **左** = 绿色箭头
4. **上、下** 保持空白

#### 最终效果

```
[  ] [  ] [  ]
[←] [中心] [X]
[  ] [  ] [  ]
```

#### 设置 Sprite

将你的**右边界 Sprite** 拖入 **Sprite** 字段。

---

### 2.7 规则 6：中心（默认）

**目标**：当上下左右都有邻居时，显示中心样式。

#### 配置步骤

1. 点击 **+** 添加第六条规则
2. 设置 **上、下、左、右** 都为 **绿色箭头**
3. **左上、右上、左下、右下** 保持空白

#### 最终效果

```
[  ] [↑] [  ]
[←] [中心] [→]
[  ] [↓] [  ]
```

#### 设置 Sprite

将你的**中心样式 Sprite** 拖入 **Sprite** 字段。

---

## 📝 步骤 3：创建水渍 Tile（3个）

水渍是独立的 Tile，不是 Rule Tile，因为它们不需要自动拼接。

### 3.1 创建水渍 Tile A

1. 在 Project 窗口中，导航到 `Assets/ZZZ_999_Package/000_Tile/`
2. 右键 → **Create** → **2D** → **Tiles** → **Tile**（注意是普通 Tile，不是 Rule Tile）
3. 命名为 `WaterPuddle_A`
4. 选中该 Tile，在 Inspector 中将**水渍样式 A** Sprite 拖入 **Sprite** 字段

### 3.2 创建水渍 Tile B

重复上述步骤，命名为 `WaterPuddle_B`，使用**水渍样式 B** Sprite。

### 3.3 创建水渍 Tile C

重复上述步骤，命名为 `WaterPuddle_C`，使用**水渍样式 C** Sprite。

---

## 📝 步骤 4：在场景中创建 Tilemap

### 4.1 场景结构

根据 `layers.md` 规范，每个楼层需要以下结构：

```
─── LAYER 1 ───
└── Tilemaps/
    ├── GroundBase (已存在)
    ├── Farmland (新建)
    └── WaterPuddle (新建)
```

### 4.2 创建 Farmland Tilemap

1. 在 Hierarchy 中，找到 `─── LAYER 1 ───/Tilemaps/`
2. 右键 → **2D Object** → **Tilemap** → **Rectangular**
3. 命名为 `Farmland`
4. 选中 `Farmland`，在 Inspector 中：
   - **Tilemap Renderer** → **Sorting Layer** = `Layer 1`
   - **Tilemap Renderer** → **Order in Layer** = `1`（在 GroundBase 之上）

### 4.3 创建 WaterPuddle Tilemap

1. 在 `Tilemaps/` 下再次右键 → **2D Object** → **Tilemap** → **Rectangular**
2. 命名为 `WaterPuddle`
3. 选中 `WaterPuddle`，在 Inspector 中：
   - **Tilemap Renderer** → **Sorting Layer** = `Layer 1`
   - **Tilemap Renderer** → **Order in Layer** = `2`（在 Farmland 之上）

### 4.4 重复其他楼层

如果你有 LAYER 2 和 LAYER 3，重复上述步骤。

---

## 📝 步骤 5：测试 Rule Tile

### 5.1 打开 Tile Palette

1. 菜单栏 → **Window** → **2D** → **Tile Palette**
2. 在 Tile Palette 窗口中，点击 **Create New Palette**
3. 命名为 `Farmland_Palette`，保存到 `Assets/ZZZ_999_Package/000_Tile/`

### 5.2 添加 Rule Tile 到 Palette

1. 将 `Farmland_RuleTile` 从 Project 窗口拖入 Tile Palette
2. 在 Tile Palette 中选中 `Farmland_RuleTile`
3. 在 Hierarchy 中选中 `LAYER 1/Tilemaps/Farmland`
4. 在 Scene 视图中绘制耕地

### 5.3 测试效果

绘制以下图案，观察 Rule Tile 是否正确切换：

```
测试 1：单独一格
[耕地]
→ 应该显示完整边界

测试 2：横向两格
[耕地][耕地]
→ 左边显示左边界，右边显示右边界

测试 3：纵向两格
[耕地]
[耕地]
→ 上边显示上边界，下边显示下边界

测试 4：田字形
[耕地][耕地]
[耕地][耕地]
→ 四个格子都显示中心样式
```

---

## 📝 步骤 6：配置 FarmingManager

### 6.1 找到 FarmingManager

1. 在 Hierarchy 中，找到 `─── MANAGERS ───/FarmingManager`
2. 如果没有，创建一个空 GameObject，命名为 `FarmingManager`，添加 `FarmingManager` 脚本

### 6.2 配置 Tilemap 引用

在 Inspector 中，找到 `FarmingManager` 的字段：

| 字段 | 拖入对象 |
|------|---------|
| **Farmland Tilemap** | `LAYER 1/Tilemaps/Farmland` |
| **Water Puddle Tilemap** | `LAYER 1/Tilemaps/WaterPuddle` |
| **Farmland Rule Tile** | `Farmland_RuleTile` 资产 |
| **Water Puddle Tiles** | 数组大小设为 3，拖入 `WaterPuddle_A/B/C` |

---

## ✅ 验收清单

完成以上步骤后，检查以下项目：

- [ ] `Farmland_RuleTile` 已创建，包含 6 条规则
- [ ] 3 个水渍 Tile 已创建（`WaterPuddle_A/B/C`）
- [ ] 每个 LAYER 下都有 `Farmland` 和 `WaterPuddle` Tilemap
- [ ] Tilemap 的 Sorting Layer 和 Order in Layer 设置正确
- [ ] Tile Palette 中可以绘制耕地
- [ ] 绘制测试图案，Rule Tile 自动切换正确
- [ ] FarmingManager 的 Tilemap 引用已配置

---

## 🐛 常见问题

### Q1：为什么 Rule Tile 不能直接叠加上下左右 4 个边界？

**原因**：Unity 的 Rule Tile 每个格子只能显示**一个** Sprite，无法同时显示多个 Sprite。

**解决方案**：
- 方案 A：手动合成一个"完整边界" Sprite（推荐）
- 方案 B：使用代码在运行时动态创建 4 个 SpriteRenderer 叠加（复杂，不推荐）
- 方案 C：使用 Tilemap Collider2D + Composite Collider2D 实现边界效果（仅适用于碰撞体，不适用于视觉）

### Q2：Rule Tile 不自动切换？

### Q2：Rule Tile 不自动切换？

**原因**：规则配置错误，或者规则顺序不对。

**解决**：
1. 检查每条规则的 9 宫格配置是否正确
2. 将"孤立"规则移到最上面（优先级最高）
3. 将"中心"规则移到最下面（优先级最低）

### Q3：水渍显示在耕地下面？

### Q3：水渍显示在耕地下面？

**原因**：`WaterPuddle` Tilemap 的 Order in Layer 设置错误。

**解决**：
- `Farmland` → Order in Layer = 1
- `WaterPuddle` → Order in Layer = 2

### Q4：绘制耕地时没有反应？

### Q4：绘制耕地时没有反应？

**原因**：Tile Palette 选中的 Tilemap 不对。

**解决**：
1. 在 Tile Palette 窗口顶部，确认 **Active Tilemap** 是 `Farmland`
2. 或者在 Hierarchy 中选中 `Farmland` Tilemap，再绘制

### Q5：我不想手动合成 Sprite，有其他办法吗？

**方案 A**：使用代码动态创建（复杂）

在 `FarmingManager` 中，当检测到孤立耕地时，动态创建 4 个 GameObject，每个 GameObject 有一个 SpriteRenderer，分别显示上下左右边界。

**缺点**：
- 增加代码复杂度
- 性能开销（每个孤立耕地需要 4 个额外的 GameObject）
- 难以维护

**方案 B**：修改需求

如果你不想制作"完整边界" Sprite，可以修改需求：
- 孤立耕地显示"中心样式"（没有边框）
- 只有相邻耕地才显示边界

**方案 C**：请美术提供

最简单的方案，直接让美术同事提供一个"四周都有边框"的 Sprite。

---

## 📚 下一步

完成 Rule Tile 配置后，你可以继续：

1. 阅读 `requirements.md` 了解农田系统的完整需求
2. 等待开发者实现 `FarmingManager` 脚本
3. 测试锄地、种植、浇水功能

---

**文档维护者**: Kiro  
**最后更新**: 2026-01-25
