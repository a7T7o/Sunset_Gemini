# Code Reaper 锐评 - 箱子系统专项

## 锐评日期
2026-01-10

---

## 评分

**结构能力**：9/10（导航/树木重构做得好）
**箱子系统**：❌ 严重不达标

---

## 核心问题

### 1. 箱子系统只是"能跑的 MonoBehaviour"

不是完整的"存储容器子系统"：
- `_contents` 只是个 `List<ItemStack>`
- 没有事件（OnSlotChanged）
- 没有 UI 绑定
- 没有存档/读档路径

### 2. UI 需求（8-10）基本是空壳

| 需求 | 状态 |
|------|------|
| 需求 8：自动导航 + 打开 UI | ❓ 部分实现 |
| 需求 9：Box 与 PackagePanel 互斥 | ❌ 未实现 |
| 需求 10：双区域布局 + 不同预制体 | ❌ 未实现 |

### 3. BoxPanelUI 依赖单例

```csharp
if (BoxPanelUI.Instance != null)
{
    BoxPanelUI.Instance.Open(this);
}
```

如果场景里没有 BoxPanelUI，整个系统就是废的。

---

## 强制重构指令

### P1 任务

1. **建立箱子系统 specs 结构**
   - memory.md（轻量索引）
   - design/（按主题拆解）
   - phases/（阶段记录）
   - code-reaper-reviews/（锐评归档）

2. **需求 1-10 对照检查**
   - 在 design/*.md 中标记 ✅/❌/❓

3. **专项处理 UI 需求（8-10）**
   - BoxPanelUI 行为说明
   - 与 PackagePanel 的互斥
   - 箱子 UI 结构

### P1.5 任务

把 `_contents` 规范化为"箱子 Inventory"：
- 类似 InventoryService 的数据结构
- 事件（OnSlotChanged / OnInventoryChanged）
- UI 绑定路径清晰

---

## 最后评语

> "要把这个模块从'会被打'的 MonoBehaviour，升级为'有自己 specs / design / phases / UI 行为的完整系统'，这才配得上你前面那套规范。"
