---
inclusion: always
priority: P0
isCanonical: true
canonicalDomain: [开发禁止事项, 全局编辑器规范, API规范, Tag/Layer多选规范]
lastUpdated: 2025-01-09
---

# 开发规则与禁止事项

## 🔴🔴🔴 最高优先级规则（必须严格遵守）🔴🔴🔴

### 1. 玩家位置中心点（最高危险级别）
- **玩家位置 = 玩家 Collider 的中心（bounds.center）**
- **绝对不是屏幕中心、Camera 中心、Sprite 中心**
- 所有涉及玩家位置的计算都必须使用 `playerCollider.bounds.center`
- 这是最基础的规则，违反此规则会导致所有位置相关功能失效
- **用户原话**："人物是以其collider为中心，而非屏幕Camera为中心的，请你严肃规范！"

### 2. 树木碰撞体只覆盖树根（严重错误来源）
- **树木的 PolygonCollider2D 只覆盖树根部分**
- **不是整个树冠/树干**
- 碰撞体用途：物理碰撞、导航阻挡
- **遮挡检测不能依赖碰撞体**，因为碰撞体不覆盖树冠
- **用户原话**："碰撞体仅仅只是树根"

### 3. 遮挡系统与碰撞体无关（严重错误来源）
- 遮挡检测基于 **Sprite Bounds**（矩形包围盒）
- 精确遮挡检测需要基于 **Sprite 的实际可见像素区域**
- **不能使用 PolygonCollider2D 进行遮挡检测**
- **用户原话**："我们最开始的设计完全就和碰撞体无关"

### 4. 先设计后实现
- 所有修改必须先提交设计方案
- 用户审核通过后才能修改代码
- **用户原话**："请你全部重新进行思考，不直接进行更新，而是全盘重新构造详细的需求和设计，我审核通过后才准进行修改代码"

### 5. 新增并兼容
- 新增功能不能破坏原有逻辑
- 修改 bug 时必须保证和原有业务逻辑保持一致

## 绝对禁止事项

### 1. 破坏原有功能
- 修改 bug 时：必须保证修改后和原有业务逻辑保持一致
- 修改业务逻辑时：必须确保和现有模块、其他脚本联动和适配
- **修改任何部分前**：先记住要做的到底是什么，从全局大体来思考
- **新增功能必须"新增并兼容"，不能直接修改破坏原有逻辑**

### 2. 忽视项目整体设计
- 必须结合项目的整体设计（多层级+标签分类、Sorting Layer 等）
- 测试点：如果原功能是"滚轮切换显示红框"，修改后必须仍然显示红框

### 3. 错误的坐标/层级处理
- 中心点问题：使用 Player 的 Collider 中心，不是摄像机/屏幕中心
- 图层问题：根据点击区域的 Sorting Layer 创建物体，不是 Default
- 模板对象：创建在 UI 中心（场景外），不是 Systems 下（场景内可拾取位置）

### 4. 混淆碰撞体用途
- 树木碰撞体：仅用于物理碰撞和导航阻挡，不用于遮挡检测
- 命中检测：基于 Sprite Bounds，不依赖碰撞体
- 遮挡检测：基于 Sprite Bounds，不依赖碰撞体

## Unity 6 API 更新

### 弃用 API
```csharp
// ❌ 弃用
collider.usedByComposite = true;
FindObjectsOfType<T>();

// ✅ 新 API
collider.compositeOperation = Collider2D.CompositeOperation.Merge;
FindObjectsByType<T>(FindObjectsSortMode.None);
FindFirstObjectByType<T>();
```

## 编辑器交互规范

### 🔴🔴🔴 Tag/Layer 多选下拉框（最高优先级 - 全局规则）🔴🔴🔴

> **此规则适用于所有需要 Tag/Layer 多选的组件，不仅限于 UI 系统！**

**所有涉及 Tag 或 Layer 多选的字段，必须使用 `NavGrid2DEditor.DrawTagMask` 方法！**

#### 正确做法
```csharp
// 在 TagMaskEditors.cs 中添加 CustomEditor
[CustomEditor(typeof(YourComponent))]
public class YourComponentEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        // 排除 tags 字段，单独绘制
        DrawPropertiesExcluding(serializedObject, "m_Script", "yourTagsField");
        // 使用 NavGrid2DEditor.DrawTagMask 绘制多选下拉框
        NavGrid2DEditor.DrawTagMask(serializedObject.FindProperty("yourTagsField"), "Your Tags Label");
        serializedObject.ApplyModifiedProperties();
    }
}
```

#### 效果
- 显示为多选下拉框（MaskField）
- 列出所有项目中的 Tags
- 支持多选，显示已选中的 Tags
- 与 NavGrid2D 的 Obstacle Tags 样式一致

#### 已使用此规范的组件（跨系统）
- `NavGrid2D` - obstacleTags（导航系统）
- `GameInputManager` - interactableTags（输入系统）
- `AutoPickupService` - pickupTags（拾取系统）
- `PlacementManager` - obstacleTags（建造系统）
- `PlacementManagerV2` - obstacleTags（建造系统）

#### ❌ 禁止的做法
- 手动输入字符串数组（默认 Inspector）
- 使用勾选框列表
- 使用 ReorderableList（太复杂）
- 任何其他自定义 UI

#### 标准实现文件
- `Assets/YYY_Scripts/Utils/Editor/TagMaskEditors.cs` - 所有 Tag 多选编辑器集中在此文件

### Tag/Layer 选择器（单选场景）
```csharp
// ✅ 正确做法：ReorderableList + Tag Popup 下拉框
tagsList = new ReorderableList(serializedObject, tagsProperty, true, true, true, true);
tagsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
{
    int newIndex = EditorGUI.Popup(rect, currentIndex, allTags);
    element.stringValue = allTags[newIndex];
};

// ❌ 错误做法1：手动输入字符串数组
[SerializeField] private string[] tags;

// ❌ 错误做法2：勾选框列表
```

## ScriptableObject 访问规范

### ItemDatabase 访问
```csharp
// ✅ 正确：从已有引用的 MonoBehaviour 获取
if (inventory != null)
    database = inventory.Database;

// ❌ 错误：永远返回 null
database = FindFirstObjectByType<ItemDatabase>();
```

## 事件系统规范

### 时间事件订阅
```csharp
// ✅ 正确：使用静态事件
TimeManager.OnDayChanged += OnDayChanged;

// ❌ 错误：使用实例方法
timeManager.OnDayChanged += OnDayChanged;
```

### 导航网格刷新
```csharp
// ✅ 正确：使用全局静态事件
NavGrid2D.OnRequestGridRefresh?.Invoke();
```

## 动画系统规范

### 帧同步
```csharp
// ✅ 正确：使用帧索引硬锁
toolAnimator.Play(targetHash, 0, toolNormalized);
toolAnimator.Update(0);

// ❌ 错误：仅依赖时间对齐
toolAnimator.Play(targetHash, 0, playerNormalizedTime);
```

### 动画 ID 获取
```csharp
// ✅ 正确：使用 GetAnimationKeyId()
int animId = toolData.GetAnimationKeyId();

// ❌ 错误：直接使用 itemID
int animId = toolData.itemID;
```

## 性能规范

### 导航网格
- 推荐 cellSize: 0.5f
- 推荐场景大小: 100x80（~150ms 重建）
- 延迟刷新: 0.2 秒合并请求

### 遮挡检测
- 使用 `Bounds.Intersects` 而非 `OverlapPoint`
- 检测间隔: 0.1 秒
- 检测半径: 8 单位

## 命名规范

### 动画资产
- 状态名：`{ActionType}_{Direction}_Clip_{itemId}_{quality}`
- 控制器：`{ActionType}_Controller_{itemId}_{itemName}.controller`

### 物品 ID
- 工具：0XXX
- 种子：10XX
- 作物：11XX
- 材料：3XXX
- 食品：5XXX

### 标签
- 树木：`Tree`（单数）
- 建筑：`Building`
- 岩石：`Rock`

## 调试规范

### 日志输出
- 使用 `[组件名]` 前缀
- 生产环境关闭详细日志
- 使用 `enableDetailedDebug` 开关控制

### 可视化调试
- 使用 Gizmos 显示检测范围
- 提供 Inspector 开关控制显示
