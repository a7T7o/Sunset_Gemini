# Code Reaper 锐评 - 会话 1：深度锐评与反思

**日期**：2025-01-08

## 总体评价

> "既然你是 Unity 6 环境，那我就更不能容忍这种还在用 Unity 5.x 时代'原始人'写法的代码了。"

> "你的代码在逻辑层是天真的，在性能层是奢侈的，在架构层是混乱的。"

---

## 病灶 1：NavGrid2D - 内存与CPU的双重谋杀

### GC 噩梦

```csharp
var allHits = Physics2D.OverlapCircleAll(worldPos, radius); // 💥 GC 炸弹
```

- 在双重循环里对几千甚至上万个格子调用 `OverlapCircleAll`
- 每次调用都在堆内存分配新的 `Collider2D` 数组
- 后果：几毫秒内制造数 MB 垃圾内存，GC 强制触发

### Unity 6 修正标准

- 必须使用 `Physics2D.OverlapCircleNonAlloc`
- 预先分配固定的 `Collider2D[]` 缓存数组（size=10），全程复用
- 零 GC 分配才是 Unity 6 的标准写法

### FindSmartStartPoint 算法复杂度爆炸

- 3 层嵌套循环 (r, dx, dy)，最大半径 10 格
- 最多遍历 20 * 20 = 400 个点
- 建议：用 BFS 替代评分系统

---

## 病灶 2：PlayerAutoNavigator - 伪科学的物理检测

### HasLineOfSight 的离散检测漏洞

```csharp
int sampleCount = Mathf.Max(3, Mathf.CeilToInt(distance / 0.3f));
for (int i = 0; i <= sampleCount; i++) {
    ... OverlapCircleAll ...
}
```

- 如果 0.3f 步长中间刚好夹着一个 0.1f 厚度的墙壁，检测点会跨过它
- 判定为"可见"，然后玩家就会撞墙

### Unity 6 修正标准

- 废除循环，使用 `Physics2D.CircleCast`
- 只需要 1 次调用，开销是现在的 1/30

### GetFacingDirection 的输入污染

- `PlayerMovement` 如果接收 `Vector2.right` 作为移动输入
- 物理位置会偏离 A* 规划好的路径
- 必须分离 `MovementInput` 和 `FacingInput`

---

## 病灶 3：GameInputManager - 架构癌变

### 硬编码逻辑

- `HandleChestInteraction` 里面直接写死 `TryLock`, `TryUnlock`
- 如果未来加了新功能，要继续往 InputManager 里塞代码

### 修正建议

引入 Command Pattern 或 Interface：
```csharp
IInteractable target = hit.GetComponent<IInteractable>();
target.OnInteract(playerContext);
```

---

## 病灶 4：多层级构筑的致命缺失

### 现状问题

- 代码里 `LayerMask` 全部是扁平的
- `NavGrid2D` 是纯 2D 数组 `bool[,]`
- 无法表示"桥上可走，桥下也可走，但桥的柱子不可走"

### 修正要求

- 必须引入 Elevation (层级/高度) 概念
- 寻路节点不再是 `Node(x, y)`，而应该是 `Node(x, y, elevation)`

---

## 最终通牒

> "如果不解决 OverlapCircleNonAlloc 的内存问题，不改掉 Lerp 视线检测，不解耦 InputManager，不引入 Z轴/层级 逻辑，这个项目只能是一个'看起来像游戏的代码堆砌物'。现在，去重写。"
