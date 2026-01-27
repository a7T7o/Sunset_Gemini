# Steering 规则维护安全规范

> **本文档是 Steering 规则文件的维护指南，任何对规则文件的增删改操作都必须遵守本规范。**

---

## 一、删除/合并规则文件前的强制检查清单

### 🔴 删除前必须完成以下检查（缺一不可）

在认为某个规则文件"可能冗余"或"可以删除"时，**必须逐项确认**：

| # | 检查项 | 确认方式 | 若不确定 |
|---|--------|---------|---------|
| 1 | **是否包含游戏策划数值？** | 搜索关键词：`阶段`、`天数`、`血量`、`ID`、`数值表`、`配置` | 不得删除 |
| 2 | **是否包含不可替代的业务约束？** | 检查是否有"必须"、"禁止"、"规则"等约束性描述 | 不得删除 |
| 3 | **是否被其他规则或 specs 引用？** | 全局搜索文件名和关键内容 | 不得删除 |
| 4 | **信息是否已完整迁移到其他地方？** | 逐段对比，确认无遗漏 | 不得删除 |
| 5 | **迁移位置是否已明确记录？** | 在 memory.md 中记录迁移详情 | 不得删除 |

### ⚠️ 若任一检查项答案不确定

**禁止删除**，只能：
1. 标记为"疑似冗余，待人工确认"
2. 在 memory.md 中记录疑虑
3. 请求用户确认

### 📝 删除操作的标准流程

```
1. 执行上述 5 项检查
2. 在 memory.md 记录检查结果
3. 若需要合并数据：
   a. 先完成数据迁移
   b. 验证迁移完整性
   c. 记录迁移详情
4. 请求用户确认
5. 执行删除
6. 更新 README.md 索引
```

---

## 二、Single Source of Truth（唯一真理来源）标记规范

### 权威文件标记

在核心文件（如 `layers.md`、`rules.md`）头部添加：

```markdown
---
isCanonical: true
canonicalDomain: [Sorting Layers, 树木层级结构, 楼层设计]
---
```

### 引用文件标记

在引用其他文件内容的地方，必须明确标注：

```markdown
> **层级结构详见 `layers.md` 中的"树木层级结构"章节，此处不再赘述。**
```

或：

```markdown
> 本文件关于"树木层级结构"的描述仅为说明性文字，权威配置请以 `layers.md` 为准。
```

### 当前项目的 Canonical 文件

| 文件 | 权威领域 |
|------|---------|
| `layers.md` | Sorting Layers、楼层结构、树木层级结构、场景层级 |
| `rules.md` | 开发禁止事项、全局编辑器规范、API 规范 |
| `workspace-memory.md` | 工作区目录结构、memory.md 规范 |
| `communication.md` | 对话模式、语言要求 |

---

## 三、全局 vs 模块规则的归类准则

### 判断标准

| 问题 | 若答案为"是" | 归属 |
|------|-------------|------|
| 这条规则会影响多个系统吗？ | 是 | `rules.md` 或 `layers.md` |
| 这条规则是编辑器交互规范吗？ | 是 | `rules.md` |
| 这条规则是层级/排序相关吗？ | 是 | `layers.md` |
| 这条规则只影响单一系统吗？ | 是 | 对应模块文档 |

### 模块文档的限制

模块文档（如 `ui.md`、`trees.md`、`animation.md`）**只允许**：

1. ✅ 补充该模块特有的用法和配置
2. ✅ 显式引用"全局规则"章节，格式：`> 详见 rules.md 的 XXX 章节`
3. ❌ **禁止**：自造一份独立的全局规则定义

### 示例

**❌ 错误做法**（在 ui.md 中定义全局规则）：
```markdown
## Tag/Layer 多选下拉框规范
所有涉及 Tag 或 Layer 多选的字段，必须使用...
```

**✅ 正确做法**（在 ui.md 中引用全局规则）：
```markdown
## 编辑器交互规范
> Tag/Layer 多选下拉框规范已迁移至 `rules.md` 的"编辑器交互规范"章节，
> 因为这是全局规则，适用于所有系统。
```

---

## 四、规则文件冗余性审查流程

### 触发条件

当认为某个规则文件"可能与其他文件重复"时，必须执行本流程。

### 审查步骤

#### 步骤 1：内文关键字段扫描

搜索以下关键词，判断是否包含不可替代的内容：

| 类型 | 关键词 |
|------|--------|
| 游戏数值 | `阶段`、`天数`、`血量`、`ID`、`数值`、`配置`、`表` |
| 策划参数 | `Stage`、`Health`、`Days`、`Level`、`Cost` |
| 业务约束 | `必须`、`禁止`、`不能`、`只能`、`规则` |

#### 步骤 2：引用关系检查

```bash
# 在项目中搜索该文件名
grep -r "文件名" .kiro/

# 搜索文件中的关键内容
grep -r "关键内容" .kiro/
```

#### 步骤 3：与候选"合并目标"文件做 diff

明确差异类型：

| 差异类型 | 说明 | 处理方式 |
|---------|------|---------|
| 约束规则 | "必须"、"禁止"等强制性规则 | 必须保留或迁移 |
| 解释说明 | 背景介绍、原理解释 | 可以精简 |
| 实现代码 | 代码示例、API 用法 | 检查是否有独特内容 |
| 数值表/配置项 | 游戏策划数值 | **必须保留或迁移** |

#### 步骤 4：记录审查结果

在 memory.md 中记录：

```markdown
### 冗余性审查记录 - {文件名}

**审查时间**: YYYY-MM-DD
**候选合并目标**: {目标文件}

**关键字段扫描结果**:
- [ ] 包含游戏数值：是/否
- [ ] 包含策划参数：是/否
- [ ] 包含业务约束：是/否

**引用关系检查结果**:
- 被引用次数：X
- 引用位置：{列表}

**差异分析**:
| 内容 | 类型 | 是否独有 | 处理建议 |
|------|------|---------|---------|
| ... | ... | ... | ... |

**审查结论**: 可删除 / 需合并后删除 / 不可删除
**理由**: ...
```

#### 步骤 5：请求用户确认

将审查结果展示给用户，等待明确确认后再执行操作。

---

## 五、新增规则文件的规范

### 新增前检查

1. 是否已有覆盖该领域的规则文件？
2. 新内容应该放在现有文件还是新建文件？
3. 新文件的优先级是什么（P0-P4）？

### 新增后操作

1. 更新 `README.md` 索引
2. 设置正确的 front-matter（inclusion 字段）
3. 在 memory.md 记录新增原因

### Front-matter 模板

```yaml
---
inclusion: always | manual | fileMatch
fileMatchPattern: "**/*.cs"  # 仅 fileMatch 时需要
priority: P0 | P1 | P2 | P3 | P4
keywords: [关键词1, 关键词2]
lastUpdated: YYYY-MM-DD
---
```

---

## 六、中文概述规范

### 适用范围

所有包含中文概述的文档，特别是：
- `archive/` 目录下的所有文档（原文为英文）
- 任何需要为中文用户提供快速摘要的文档

### 必须包含的字段

中文概述章节**必须**包含以下内容：

| 字段 | 格式 | 说明 |
|------|------|------|
| **最后更新** | YYYY-MM-DD | 概述内容的最后更新日期，便于用户判断信息时效性 |
| **核心信息摘要** | 要点列表 | 文档的关键信息，让用户一眼抓到重点 |

### 标准格式模板

```markdown
## 中文概述（面向开发者本人）

- **最后更新**：YYYY-MM-DD
- **[核心字段1]**：[内容]
- **[核心字段2]**：[内容]
- ...

> **注意**：[可选的时效性提醒或其他说明]
```

### 示例

**progress.md 的中文概述**：
```markdown
## 中文概述（面向开发者本人）

- **最后更新**：2025-01-09
- **当前阶段**：第二阶段（玩法系统实现）
- **整体完成度**：约 45%
- **已完成模块**：时间系统、地图系统、角色系统...
- **进行中模块**：农田系统、云朵阴影系统
- **待开发模块**：战斗系统、NPC 系统...

> **注意**：此文档可能已过时，最新进度请查看各模块的 memory.md 文件。
```

### 为什么需要"最后更新"字段

用户在几个月后看到概述时，能立刻知道"这个信息是什么时候的"，避免被过时信息误导。

### 日期字段更新规范

| 更新场景 | front-matter 的 lastUpdated | 中文概述的"最后更新" |
|---------|---------------------------|-------------------|
| 修改规则文件的实质内容 | ✅ 必须更新 | ❌ 不更新（除非中文概述也变了） |
| 修改中文概述的内容 | ✅ 必须更新 | ✅ 必须更新 |
| 只修改格式/错别字 | ❌ 可选 | ❌ 不更新 |

---

## 七、定期维护检查

### 每月检查项

- [ ] 是否有规则文件内容过时？
- [ ] 是否有规则文件之间内容重复？
- [ ] 是否有全局规则误放在模块文档中？
- [ ] README.md 索引是否与实际文件一致？
- [ ] 中文概述的"最后更新"日期是否需要刷新？

### 检查后操作

1. 记录检查结果到 memory.md
2. 提出优化建议
3. 请求用户确认后执行



---

## 八、规则更新触发机制

### 触发条件

在以下情况下，**必须主动询问用户是否需要更新规则文档**：

| # | 触发条件 | 示例 | 提问模板 |
|---|---------|------|---------|
| 1 | **完成了一个完整的功能模块** | 树木系统的 6 阶段生长 + 砍伐 + 导航同步全部完成 | "XX系统已完成，是否需要我更新 XX.md 或相关 specs？" |
| 2 | **发现/修正了一个重要的设计错误** | 发现之前对玩家位置的理解是错的，修正后 | "我们刚修正了XX的规则，是否需要我更新 rules.md 或在 memory 里记录这次教训？" |
| 3 | **引入了一个新的全局约束** | 新增了"所有 Tag/Layer 多选必须用统一组件"的规则 | "这是个新的全局规则，是否需要我添加到 rules.md？" |
| 4 | **完成了一次较大的代码重构** | 把动画系统从 Mode A 升级到 Mode A++ | "XX系统已重构，是否需要我更新 XX.md 和相关 memory？" |
| 5 | **用户明确表达了不满/发现了重复错误** | 用户说"我都说了三遍了，你怎么又犯这个错" | "看起来这是个反复出现的问题，是否需要我把这条约束写进规则，避免以后再犯？" |

### 更新前必须征求用户同意

**不得在未经用户明确同意的情况下，自行创建/修改 steering 规则文件。**

更新流程：

```
1. 检测到触发条件（上述 1-5 项）
2. 向用户说明：
   - 我认为需要更新的文件是哪个
   - 我打算添加/修改的内容是什么
   - 这样做的理由是什么
3. 等待用户明确回复：
   - "同意" → 执行更新
   - "不需要" → 不更新，但在 memory 里记录"用户认为暂不需要"
   - "稍后再说" → 在 memory 里记录"待更新事项"
```

### 每次对话结束前的检查清单

在用户说"好了"、"完成了"、"今天就这样吧"等结束性话语时，**主动检查**：

- [ ] 本次对话是否完成了新功能？
- [ ] 本次对话是否发现了需要记录的教训？
- [ ] 本次对话是否引入了新的设计约束？
- [ ] memory.md 是否已更新本次对话记录？
- [ ] 是否有需要更新但尚未更新的规则文档？

若有任一项为"是"但尚未处理，**主动提醒用户**。

---

## 九、keywords 字段说明

### 当前用途

每个规则文件的 front-matter 中的 `keywords` 字段，用于：

1. **索引功能**：帮助用户快速查找相关规则
   - 例如：在 README 中看到"动画、工具、同步"，就知道该加载 `animation.md`

2. **提醒功能**：当用户在对话中提到这些关键词时，AI 应主动提示（但不自动加载）
   - 例如：用户说"我要开发动画功能"
   - AI 检测到关键词"动画"，主动提示："检测到你在讨论动画，是否需要加载 animation.md？"
   - 等待用户明确同意后，才加载

### 不会做的事

- ❌ 不会自动加载规则文件
- ❌ 不会在未经用户同意的情况下，主动加载规则

### 提醒触发规范

当满足以下条件时，AI 应主动提醒用户加载相关规则：

1. 用户在对话中提到某个关键词 **≥ 2 次**
2. 当前对话明显涉及该领域（例如用户说"我要开发XX系统"）
3. 对应的规则文件**尚未加载**

### 提醒格式

```
检测到你在讨论 [关键词]，建议加载以下规则：
- `规则文件名.md` - 简要说明

是否需要加载？
```

### 未来演进

目前采用"方案 A（手动加载 + 主动提醒）"，暂无计划升级为"方案 C（自动加载）"。
若未来需求变化，会先更新本文档，征得用户同意后再调整。


---

## 十、工作区分层与跨域协作规范

### specs 工作区的 memory 定位

每个 specs 工作区的 `memory.md` 只做三件事：

1. **模块概述 + 当前状态**：简短描述 + 完成度 + 当前阶段
2. **会话摘要**：每个会话只写 2-3 行 + 链接到详情文档
3. **跨工作区索引**：只写链接，不写全文

### memory 的体积控制

- **目标**：每个 memory.md 控制在 2000-3000 字符以内
- **详细内容**：拆分到子目录（如 `phases/`、`reviews/`）
- **长篇锐评**：归档到 `code-reaper-reviews/` 或类似目录

### 跨系统问题的处理

当一个工作区的问题涉及到其他系统时：

1. **在对应系统的工作区创建/更新内容**
2. **在当前工作区的 memory 里只写摘要 + 链接**
3. **不要**把其他系统的详细内容粘贴到当前 memory

示例：
```markdown
### 跨工作区索引

| 相关系统 | 工作区 | 关联问题 |
|---------|--------|---------|
| 树木系统 | `.kiro/specs/树木系统/` | NavGrid 动态刷新联动 |
| 箱子系统 | `.kiro/specs/箱子系统/` | IInteractable 实现 |
```

### 新建子文档的命名规范

- **阶段详情**：`phases/phase{N}-{简短描述}.md`
- **锐评归档**：`code-reaper-reviews/review-session{N}-{简短描述}.md`
- **设计文档**：`design-{模块名}.md`

### 工作区目录结构示例

```
.kiro/specs/导航系统重构/
├── memory.md                    # 总控 memory（轻量）
├── requirements.md              # 需求文档
├── design.md                    # 设计文档
├── tasks.md                     # 任务列表
├── phases/                      # 阶段详情目录
│   ├── phase1-navgrid-zero-gc.md
│   ├── phase2-los-and-physics.md
│   └── phase3-interactable-architecture.md
└── code-reaper-reviews/         # 锐评归档目录
    ├── review-session1-deep-critique.md
    └── review-session2-tactical-orders.md
```

### 为什么要这样做

1. **memory.md 只做索引** → 体积小，编辑快，不会超时
2. **详情在子文档** → 每个文档独立，不互相影响
3. **跨系统内容在各自工作区** → 职责清晰，便于维护


### 定期结构巡检

**频率**：每完成一次大型工作区重构后，或每月一次

**巡检内容**：
1. 检查所有 specs 工作区的 memory.md 字符数
2. 检查是否有分层结构（design / phases / reviews）
3. 检查会话摘要是否只有 2-3 行 + 链接
4. 更新 `.kiro/specs/specs-structure-audit.md`

**巡检标准**：
- ✅ memory.md 字符数 ≤ 3,000
- ✅ 有分层结构
- ✅ 会话摘要简洁
- ✅ 跨工作区问题有索引

**不符合标准的处理**：
- 标记为"待重构"
- 制定重构计划
- 在下次巡检前完成

**巡检报告位置**：`.kiro/specs/specs-structure-audit.md`


---

## 十一、世界物体 Sprite 状态切换规范（2026-01-14 新增）

### 适用范围

所有会切换 Sprite 的世界物体，包括但不限于：
- 树木（TreeControllerV2）
- 石头（RockController）
- 箱子（ChestController）
- 其他可交互物体

### 🔴 底部对齐规范（最高优先级）

**核心原则**：Sprite 底部中心在世界空间中必须始终保持固定锚点，不随 Sprite 替换而跳动。

**实现要求**：

1. **锚点缓存**：在 Awake 时记录初始底部中心位置
```csharp
private Vector3 _anchorWorldPos;
private bool _anchorInitialized = false;

private void Awake()
{
    if (!_anchorInitialized)
    {
        _anchorWorldPos = GetCurrentBottomCenterWorld();
        _anchorInitialized = true;
    }
}
```

2. **底部中心计算**：
```csharp
private Vector3 GetCurrentBottomCenterWorld()
{
    if (_spriteRenderer == null || _spriteRenderer.sprite == null)
        return transform.position;
    
    var bounds = _spriteRenderer.bounds;
    return new Vector3(bounds.center.x, bounds.min.y, transform.position.z);
}
```

3. **应用 Sprite 时修正位置**：
```csharp
private void ApplySpriteWithBottomAlign(Sprite newSprite)
{
    if (_spriteRenderer == null || newSprite == null) return;
    
    _spriteRenderer.sprite = newSprite;
    
    Vector3 newBottomCenter = GetCurrentBottomCenterWorld();
    Vector3 delta = _anchorWorldPos - newBottomCenter;
    transform.position += delta;
}
```

### 🔴 Sprite → Collider → NavGrid 刷新链路

**核心原则**：任意 Sprite 状态变化必须遵循以下顺序：

1. **更新 Sprite**（含底部对齐）
2. **重建 Collider**（PolygonCollider2D 等）
3. **刷新 NavGrid**（在刷新前 Physics2D.SyncTransforms）

**实现要求**：

```csharp
public void SetOpen(bool open)
{
    _isOpen = open;
    UpdateSpriteForState();  // 1. 更新 Sprite
    UpdateColliderShape();   // 2. 重建 Collider
    RequestNavGridRefresh(); // 3. 刷新 NavGrid
}
```

**Collider 更新方法**：
```csharp
private void UpdateColliderShape()
{
    if (_polyCollider == null || _spriteRenderer == null) return;
    
    var sprite = _spriteRenderer.sprite;
    int shapeCount = sprite.GetPhysicsShapeCount();
    
    _polyCollider.pathCount = shapeCount;
    
    var physicsShape = new List<Vector2>();
    for (int i = 0; i < shapeCount; i++)
    {
        physicsShape.Clear();
        sprite.GetPhysicsShape(i, physicsShape);
        _polyCollider.SetPath(i, physicsShape);
    }
    
    Physics2D.SyncTransforms(); // 确保物理系统同步
}
```

### 参考实现

- **TreeControllerV2.cs**：完整的底部对齐 + Collider 更新实现
- **ChestController.cs**：箱子系统的实现（2026-01-14 修正）

---

## 十二、Box UI 与 PackagePanel 互斥规范（2026-01-14 新增）

### UI 层级架构

```
Canvas
└── PackagePanel (panelRoot)
    ├── 6_Top (topParent) - Tab 按钮区
    ├── Main (pagesParent) - 页面内容区
    │   ├── 0_Props
    │   ├── 1_Recipes
    │   └── ...
    └── BoxUIRoot (boxUIRoot) - Box UI 容器
        └── Box_XX (动态实例化)
```

### 互斥规则

1. **打开箱子时**：
   - PackagePanel 根（panelRoot）**必须保持 active**
   - 隐藏 Main 和 Top（背包区域）
   - 在 BoxUIRoot 下实例化 Box UI

2. **打开背包时**：
   - 若有 Box UI 打开，先关闭 Box UI
   - 恢复 Main 和 Top
   - 调用 OnPanelJustOpened() 确保初始化

3. **关键约束**：
   - BoxPanelUI.Open() **禁止**调用 ClosePackagePanel()
   - 所有互斥逻辑**必须**集中在 PackagePanelTabsUI 中

### BoxPanelUI.Down 背包镜像

**索引策略**：Down 区显示主背包（不含 hotbar），从 `InventoryService.HotbarWidth`（12）开始

```csharp
int startIndex = InventoryService.HotbarWidth; // 12
for (int i = 0; i < _inventorySlots.Count; i++)
{
    int actualIndex = startIndex + i;
    slot.Bind(_inventoryService, null, _database, actualIndex, false);
}
```

**前置依赖**：
- panelRoot 已 active
- OnPanelJustOpened() 已触发
- InventoryPanelUI.EnsureBuilt() 已执行
- InventoryService/Database 已初始化

---

## 十三、UI 面板与世界输入互斥规范（2026-01-16 新增）

### 适用范围

所有会打开 UI 面板的系统，包括但不限于：
- PackagePanel（背包面板）
- BoxPanelUI（箱子 UI）
- 设置面板
- 其他全屏/半屏 UI

### 🔴 核心原则

**当任意 UI 面板打开时，必须禁用对世界的输入。**

### 需要禁用的输入

| 输入类型 | 说明 | 禁用方式 |
|---------|------|---------|
| 左键点击世界 | 使用工具、攻击 | `IsAnyPanelOpen` 检查 |
| 右键点击世界 | 自动导航 | `IsAnyPanelOpen` 检查 |
| 滚轮 | 切换工具 | `pointerOverUI` 检查 |
| 数字键 1-9 | 切换工具 | `IsAnyPanelOpen` 检查 |
| WASD | 移动 | `IsAnyPanelOpen` 检查 |

### 需要保持可用的输入

| 输入类型 | 说明 |
|---------|------|
| Tab | 切换/关闭面板 |
| ESC | 关闭面板 |
| B/M/L/O | 切换到对应面板页面 |

### 实现机制

`GameInputManager` 中的检查逻辑：

```csharp
// 1. IsAnyPanelOpen 检查
if (PackagePanelTabsUI.Instance != null && PackagePanelTabsUI.Instance.IsOpen)
    return; // 禁用世界输入

// 2. EventSystem UI 检查
if (EventSystem.current.IsPointerOverGameObject())
    return; // 鼠标在 UI 上，禁用世界输入

// 3. pointerOverUI 标志
private bool pointerOverUI;
// 在 Update 中更新，用于判断鼠标是否在 UI 上
```

### Box UI 的特殊处理

Box UI 在 PackagePanel 内部，`panelRoot` 保持 active，因此：
- `PackagePanelTabsUI.Instance.IsOpen` 返回 true
- 世界输入自动被禁用
- 无需额外处理

### 验证清单

新增 UI 面板时，必须验证：

- [ ] 面板打开时 `IsAnyPanelOpen` 返回 true
- [ ] 左键点击世界不触发任何行为
- [ ] 右键点击世界不触发自动导航
- [ ] 滚轮不切换工具
- [ ] 数字键不切换工具
- [ ] Tab/ESC 仍然可用
