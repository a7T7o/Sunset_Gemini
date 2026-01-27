# Rule Tile 配置指南 - 耕地系统

## 📋 素材准备清单

根据你提供的图片，你需要准备以下 Sprite：

### 耕地边界 Sprite（棕色土地）

| 编号 | 名称 | 说明 | 你的素材 |
|------|------|------|---------|
| 1 | `Farmland_Top` | 上边界 | ✅ 已有 |
| 2 | `Farmland_Bottom` | 下边界 | ✅ 已有 |
| 3 | `Farmland_Left` | 左边界 | ✅ 已有 |
| 4 | `Farmland_Right` | 右边界 | ✅ 已有 |
| 5 | `Farmland_Center_A` | 中心样式 A | ✅ 已有 |
| 6 | `Farmland_Center_B` | 中心样式 B | ✅ 已有 |

### 水渍 Sprite（蓝色水渍）

| 编号 | 名称 | 说明 | 你的素材 |
|------|------|------|---------|
| 1 | `WaterPuddle_A` | 水渍样式 A | ✅ 已有 |
| 2 | `WaterPuddle_B` | 水渍样式 B | ✅ 已有 |
| 3 | `WaterPuddle_C` | 水渍样式 C | ✅ 已有 |

### ⚠️ 可能缺少的素材

根据标准 Rule Tile 配置，你可能还需要：

| 编号 | 名称 | 说明 | 状态 |
|------|------|------|------|
| 7 | `Farmland_TopLeft` | 左上角 | ❓ 需确认 |
| 8 | `Farmland_TopRight` | 右上角 | ❓ 需确认 |
| 9 | `Farmland_BottomLeft` | 左下角 | ❓ 需确认 |
| 10 | `Farmland_BottomRight` | 右下角 | ❓ 需确认 |
| 11 | `Farmland_Single` | 单独一格（四周都是边界） | ❓ 需确认 |

---

## 🔧 Step 1：导入 Sprite

1. 将所有耕地 Sprite 放入 `Assets/Sprites/Farm/Farmland/` 文件夹
2. 将所有水渍 Sprite 放入 `Assets/Sprites/Farm/WaterPuddle/` 文件夹

### Sprite 设置

选中所有 Sprite，在 Inspector 中设置：

| 属性 | 值 |
|------|-----|
| Texture Type | Sprite (2D and UI) |
| Sprite Mode | Single |
| Pixels Per Unit | 16（或你的像素单位） |
| Filter Mode | Point (no filter) |
| Compression | None |

---

## 🔧 Step 2：创建 Rule Tile

### 2.1 创建 Rule Tile 资源

1. 在 Project 窗口右键
2. 选择 `Create → 2D → Tiles → Rule Tile`
3. 命名为 `Farmland_RuleTile`
4. 保存到 `Assets/111_Data/Tiles/` 文件夹

### 2.2 配置 Rule Tile

选中 `Farmland_RuleTile`，在 Inspector 中：

1. **Default Sprite**: 设置为 `Farmland_Center_A`
2. **Default Collider**: None（耕地不需要碰撞）

### 2.3 添加规则

点击 `+` 按钮添加规则，按以下顺序配置：

---

### 规则配置详解

#### 📌 规则 1：单独一格（四周都没有耕地）

```
邻居检测（3x3 网格）：
┌───┬───┬───┐
│ ✗ │ ✗ │ ✗ │
├───┼───┼───┤
│ ✗ │ X │ ✗ │
├───┼───┼───┤
│ ✗ │ ✗ │ ✗ │
└───┴───┴───┘

✗ = 红色（此处不能有 Tile）
X = 当前格子

输出 Sprite: Farmland_Single（或用 Center 代替）
```

**在 Unity 中操作**：
1. 点击 `+` 添加新规则
2. 在 3x3 网格中，点击所有 8 个邻居格子，设为红色 ✗
3. 设置 Output Sprite 为你的单独格子样式

---

#### 📌 规则 2：上边界（上方没有耕地）

```
邻居检测：
┌───┬───┬───┐
│   │ ✗ │   │
├───┼───┼───┤
│   │ X │   │
├───┼───┼───┤
│   │ ✓ │   │
└───┴───┴───┘

✗ = 红色（此处不能有 Tile）
✓ = 绿色（此处必须有 Tile）
空 = 灰色（不关心）

输出 Sprite: Farmland_Top
```

---

#### 📌 规则 3：下边界（下方没有耕地）

```
邻居检测：
┌───┬───┬───┐
│   │ ✓ │   │
├───┼───┼───┤
│   │ X │   │
├───┼───┼───┤
│   │ ✗ │   │
└───┴───┴───┘

输出 Sprite: Farmland_Bottom
```

---

#### 📌 规则 4：左边界（左方没有耕地）

```
邻居检测：
┌───┬───┬───┐
│   │   │   │
├───┼───┼───┤
│ ✗ │ X │ ✓ │
├───┼───┼───┤
│   │   │   │
└───┴───┴───┘

输出 Sprite: Farmland_Left
```

---

#### 📌 规则 5：右边界（右方没有耕地）

```
邻居检测：
┌───┬───┬───┐
│   │   │   │
├───┼───┼───┤
│ ✓ │ X │ ✗ │
├───┼───┼───┤
│   │   │   │
└───┴───┴───┘

输出 Sprite: Farmland_Right
```

---

#### 📌 规则 6：中心（四周都有耕地）- 随机变体

```
邻居检测：
┌───┬───┬───┐
│   │ ✓ │   │
├───┼───┼───┤
│ ✓ │ X │ ✓ │
├───┼───┼───┤
│   │ ✓ │   │
└───┴───┴───┘

输出：随机选择 Farmland_Center_A 或 Farmland_Center_B
```

**配置随机输出**：
1. 在 Output 下拉菜单选择 `Random`
2. 添加 2 个 Sprite：`Farmland_Center_A` 和 `Farmland_Center_B`

---

### 简化版规则（如果你只有上下左右中心）

如果你没有角落素材，可以使用简化版：

| 规则 | 条件 | 输出 |
|------|------|------|
| 1 | 上方无 Tile | Top |
| 2 | 下方无 Tile | Bottom |
| 3 | 左方无 Tile | Left |
| 4 | 右方无 Tile | Right |
| 5 | 默认（四周都有） | Center（随机 A/B） |

---

## 🔧 Step 3：创建水渍 Tile

水渍不需要 Rule Tile，使用普通 Tile 即可：

1. 右键 Project 窗口 → `Create → 2D → Tiles → Tile`
2. 创建 3 个 Tile：
   - `WaterPuddle_Tile_A`
   - `WaterPuddle_Tile_B`
   - `WaterPuddle_Tile_C`
3. 分别设置对应的 Sprite

---

## 🔧 Step 4：场景配置

### 4.1 创建 Tilemap 结构

在每个 LAYER 下创建以下结构：

```
─── LAYER 1 ───
└── Tilemaps
    ├── GroundBase (已有)
    ├── Grass (已有)
    ├── Farmland (新建)      ← 耕地 Tilemap
    └── WaterPuddle (新建)   ← 水渍 Tilemap
```

### 4.2 创建 Farmland Tilemap

1. 在 `LAYER 1/Tilemaps` 下右键 → `2D Object → Tilemap → Rectangular`
2. 重命名为 `Farmland`
3. 设置 Tilemap Renderer：
   - Sorting Layer: `Layer 1`（与该楼层一致）
   - Order in Layer: 1（在 GroundBase 之上）

### 4.3 创建 WaterPuddle Tilemap

1. 同样创建一个 Tilemap，命名为 `WaterPuddle`
2. 设置 Tilemap Renderer：
   - Sorting Layer: `Layer 1`
   - Order in Layer: 2（在 Farmland 之上）

### 4.4 为每个楼层重复

对 LAYER 2、LAYER 3 等重复上述步骤。

---

## 🔧 Step 5：测试 Rule Tile

### 手动测试

1. 打开 Tile Palette 窗口（`Window → 2D → Tile Palette`）
2. 创建新的 Palette，命名为 `Farm`
3. 将 `Farmland_RuleTile` 拖入 Palette
4. 选中 Rule Tile，在场景中绘制测试

### 预期效果

- 单独一格：显示完整边界
- 相邻格子：自动切换为中心样式
- 边缘格子：显示对应方向的边界

---

## ✅ 配置完成检查清单

- [ ] 所有 Sprite 已导入并设置正确
- [ ] Rule Tile 已创建并配置规则
- [ ] 3 个水渍 Tile 已创建
- [ ] 每个 LAYER 下都有 Farmland 和 WaterPuddle Tilemap
- [ ] Tilemap 的 Sorting Layer 和 Order 设置正确
- [ ] 手动测试 Rule Tile 边界拼接正常

---

## 🚀 下一步

配置完成后，告诉我，我将：
1. 更新 `FarmingManager.cs` 支持多楼层
2. 集成 Rule Tile 和水渍系统
3. 连接锄头工具交互
