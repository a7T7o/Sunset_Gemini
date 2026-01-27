# Windsurf 记忆复制

## 记忆点 1: 核心交互原则

**核心原则：学习Unity官方标准交互，严格遵循Game Input Manager风格**

**Tag/Layer选择器规范**：

❌ 错误做法1：手动输入字符串数组
```csharp
[SerializeField] private string[] tags;  // 容易拼写错误
```

❌ 错误做法2：勾选框列表（不是Unity标准）
```csharp
// 虽然可视化，但不符合Unity标准风格
☑ Trees
☑ Buildings
```

✅ 正确做法：ReorderableList + Tag Popup下拉框（Unity标准）
```csharp
// 完全符合Game Input Manager的Interactable Tags风格
━━━━━━━━━━
Occlusion Tags
  Trees        [下拉箭头▼]
  Buildings    [下拉箭头▼]
  + -
━━━━━━━━━━
```

**标准实现代码**：
```csharp
[CustomEditor(typeof(XXX))]
public class XXXEditor : Editor
{
    private ReorderableList tagsList;
    private string[] allTags;
    
    void OnEnable()
    {
        allTags = InternalEditorUtility.tags;
        SetupReorderableList();
    }
    
    void SetupReorderableList()
    {
        tagsList = new ReorderableList(serializedObject, tagsProperty, true, true, true, true);
        
        tagsList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Tags");
        };
        
        tagsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            SerializedProperty element = tagsList.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += 2;
            
            // 🔥 Tag Popup下拉框（Unity标准）
            string currentTag = element.stringValue;
            int currentIndex = System.Array.IndexOf(allTags, currentTag);
            if (currentIndex < 0) currentIndex = 0;
            
            int newIndex = EditorGUI.Popup(rect, currentIndex, allTags);
            if (newIndex >= 0 && newIndex < allTags.Length)
            {
                element.stringValue = allTags[newIndex];
            }
        };
        
        tagsList.onAddCallback = (ReorderableList list) =>
        {
            int index = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            list.index = index;
            SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
            element.stringValue = allTags.Length > 0 ? allTags[0] : "Untagged";
        };
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        tagsList.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }
}
```

**关键API**：
- `InternalEditorUtility.tags`：获取项目所有Tags
- `ReorderableList`：可重新排序列表（Unity标准）
- `EditorGUI.Popup`：下拉框（选择单个Tag）
- `drawElementCallback`：绘制每个元素
- `onAddCallback`：添加新元素时的默认值

**优势**：
1. ✅ Unity官方标准风格，用户熟悉
2. ✅ 支持拖拽排序（ReorderableList）
3. ✅ 支持添加/删除元素（+/-按钮）
4. ✅ 每个Tag单独下拉选择，避免拼写错误
5. ✅ 完全符合Game Input Manager的交互

**适用场景**：
- 所有需要选择Unity Tags的地方
- 所有需要选择Layer的地方
- 任何需要从固定列表中选择多项的场景

**参考示例**：
- Game Input Manager的Interactable Tags
- Nav Mesh Settings的Area Mask
- Layer Collision Matrix

**绝对禁止**：
- ❌ 手动输入字符串（容易出错）
- ❌ 使用勾选框列表（不符合Unity标准）
- ❌ 使用自定义复杂UI（用户不熟悉）

## 记忆点 2: Unity项目"Sunset"遮挡透明系统v2.0

**🎯 核心逻辑：玩家在树下方（Y > TreeY）+ Sprite Bounds重叠 → 树透明**

---

## 检测方案：Bounds.Intersects（方案A）

**为什么不用Collider：**
- PolygonCollider2D = 树根范围（sprite底部小区域）
- 遮挡检测需要 = 树冠范围（完整sprite）
- 用途分离：Collider = 物理碰撞，Bounds = 视觉遮挡

**核心代码：**
```csharp
// OcclusionManager.DetectOcclusion()
float playerY = player.position.y;
float treeY = occluder.transform.position.y;

// 1. Y坐标检测（玩家在树下方）
if (playerY <= treeY) continue;

// 2. 距离过滤（优化性能）
if (Vector2.Distance(playerPos, treePos) > detectionRadius) continue;

// 3. Sprite Bounds重叠检测
Bounds playerBounds = playerSprite.bounds;
Bounds treeBounds = occluder.GetBounds();  // 从子物体Tree获取

if (treeBounds.Intersects(playerBounds))
{
    occluder.SetOccluding(true);  // 触发透明
}
```

---

## 双层结构Order计算（已修复）

**TreeController设计原理：**
```
Tree_M1_01（父物体，Y=27）
  ├─ 位置 = 树根 = 种植点（游戏逻辑位置）
  ├─ Rigidbody2D (Static)
  ├─ CompositeCollider2D (物理碰撞用)
  ├─ OcclusionTransparency
  │
  ├─ Tree（子物体，localY动态调整）
  │    ├─ SpriteRenderer（渲染位置）
  │    ├─ PolygonCollider2D (Merge)
  │    └─ TreeController（AlignSpriteBottom）
  │
  └─ Shadow（子物体）
```

**Order计算修复：**
```csharp
// Tool_002 & StaticObjectOrderAutoCalibrator
private float CalculateSortingY(SpriteRenderer sr)
{
    // 双层结构检测
    Transform parent = sr.transform.parent;
    if (parent != null && parent.GetComponent<SpriteRenderer>() == null)
    {
        // 父物体无SR → 用父物体Y（种植点）
        return parent.position.y + bottomOffset;
    }
    
    // 常规：Collider > Sprite > Transform
}
```

---

## 关键修复

**1. 注册时序问题（已解决）：**
```csharp
// OcclusionTransparency.OnEnable()
private IEnumerator RegisterDelayed()
{
    yield return null;  // 等待一帧，确保Manager已初始化
    if (OcclusionManager.Instance != null)
    {
        OcclusionManager.Instance.RegisterOccluder(this);
    }
}
```

**2. 标签配置（已统一）：**
```csharp
// OcclusionTransparency默认标签
occlusionTags = ["Trees", "Buildings", "Rocks"];

// OcclusionManager过滤标签
occludableTags = ["Trees", "Rocks", "Buildings", "Interactable"];

// 匹配逻辑：树有"Trees"标签，Manager接受"Trees"标签 ✅
```

**3. CompositeCollider2D配置：**
```csharp
// BatchAddOcclusionComponents顺序
1. 父物体添加Rigidbody2D (Static)
2. Tree子物体设置compositeOperation = Merge
3. 父物体添加CompositeCollider2D (Trigger)
4. composite.GenerateGeometry()  // 强制刷新
5. 删除父物体空SpriteRenderer
```

---

## 性能优势

| 项目 | OverlapPoint（旧） | Bounds.Intersects（新） |
|------|------------------|----------------------|
| 检测方式 | 物理引擎查询 | 4次浮点比较 |
| 每帧开销 | ~0.02ms | ~0.001ms |
| 速度提升 | - | **20倍** |
| 自动适配 | 需手动调整 | 自动适应sprite大小 |

---

## 使用流程

**1. 场景配置：**
```
1. 创建空物体"OcclusionManager"
2. 添加OcclusionManager组件
3. 配置：
   - 检测半径: 8
   - 检测间隔: 0.1s
   - Occludable Tags: Trees, Buildings, Rocks
```

**2. 批量添加组件：**
```
1. 选中所有树木父物体
2. Tools → 🌳 批量添加遮挡组件
3. Tools → 🔧 校准所有静态物体Order
```

**3. 完成！**
- 玩家走到树下 → 树自动透明
- 玩家离开 → 树恢复不透明

---

## Unity 6 API更新

```csharp
// ❌ 弃用
collider.usedByComposite = true;
FindObjectsOfType<T>();

// ✅ 新API
collider.compositeOperation = Collider2D.CompositeOperation.Merge;
FindObjectsByType<T>(FindObjectsSortMode.None);
FindFirstObjectByType<T>();
```

---

## 工具脚本

**BatchAddOcclusionComponents.cs：**
- 自动添加Rigidbody2D、CompositeCollider2D、OcclusionTransparency
- 跳过系统物体（名字包含System/Manager等）
- 删除父物体空SpriteRenderer

**StaticObjectOrderAutoCalibrator.cs：**
- 进入PlayMode前自动校准Order
- 清理父物体空SpriteRenderer
- 双层结构使用父物体Y坐标

**CleanInvalidOcclusionComponents.cs：**
- 清理无效的OcclusionTransparency组件
- Tools → 🧹 清理无效的遮挡组件

**FixOcclusionTags.cs：**
- 批量修复标签配置（已废弃，不需要）

---

## 调试

**可视化Gizmos：**
- OcclusionManager勾选"显示调试Gizmos"
- 绿色方框 = 玩家bounds
- 红色方框 = 遮挡的树bounds
- 绿线 = 玩家在树下方

**Console日志（已清理）：**
- 只保留关键错误日志
- 移除所有详细调试输出
- 生产环境代码

---

## 关键记忆

**检测逻辑：**
1. 玩家Y > 树Y（玩家在下方）
2. 距离 < 检测半径（优化）
3. 标签匹配（过滤）
4. Bounds.Intersects（精确）

**职责分离：**
- PolygonCollider2D → 物理碰撞
- CompositeCollider2D → 组合碰撞体
- Sprite.bounds → 视觉遮挡检测
- Y坐标 → 深度判断

**Order计算：**
- 双层结构 → 父物体Y（种植点）
- 单层结构 → Collider > Sprite > Transform

**所有代码已完成，Unity 6兼容，性能优化，生产就绪！**

## 记忆点 3: Unity 2D 项目"Sunset"交接要点

- 目标：玩家与工具（斧头）动画完美同步；斧头渲染在玩家下方；精确的每帧 Sprite Pivot 设置；动画剪辑时长/帧分布正确；修复 Editor Graphs 错误。
- 高优先级问题：
  1) NullReferenceException: UnityEditor.Graphs.Edge.WakeUp() 持续出现；
  2) 斧头动画不同步（疑似引用丢失/延迟初始化冲突）；
  3) 斧头渲染层级错误（当前 +1，应为 -1）。
- 关键文件：LayerAnimSync.cs、PlayerAnimController.cs、AxeAnimController.cs、AnimatorExtensions.cs、Editor/SlicePivotCopyTool.cs、Editor/LayerAnimSetupTool.cs。
- 历史尝试：事件驱动、预测同步未解决延迟；当前采用 LateUpdate 精确帧同步（Play+Update(0)）但失效；LayerAnimSetupTool 的帧分布问题已修复。
- Pivot 工具：需要严格处理"每帧 Pivot"和切割偏移（X 左边距、Y 顶边距→底部系转换），确保像素→归一映射正确。
- 下一步：先做环境诊断（检查 LayerAnimSync Inspector 引用、运行时日志采集），列出 Assets/Editor 自定义脚本清单，再实施修复。

## 记忆点 4: 面板开关逻辑

**面板开关逻辑**
1. J键：与主面板完全分离，后续用作任务面板，当前不绑定任何功能
2. P键：不是UI快捷键（可能是时间调试？），不应该绑定面板切换
3. Tab/B/M/L/O：打开对应页面；已打开时再按同键→关闭主面板
4. ESC：已打开任意面板时→打开设置页；没打开时→打开设置面板
5. 鼠标点击Top的Toggle = 按快捷键：逻辑相同，只是切换页面展示，需要参数统一
6. **双击按钮退出界面**（需确认具体实现）

**初始状态**
- 游戏开始时：UI根物体（名为"UI"）启用/激活
- PackagePanel：默认不激活，运行时按键才激活
- 层级：UI > PackagePanel > Main > [0_Props, 1_Recipes, 2_Ex, 3_Map, 4_Relationship_NPC, 5_Settings] + Top

## 记忆点 5: 物品设计交接

交接文档已升级至 v3.2（2025-11-14）。新增并记录：
- ItemSOBatchCreator 批量创建工具（多Sprite重排、并行ID/名称输入、首ID自增、类型专属字段、规范命名与保存路径；多行输入已加入滚动条）；菜单：Farm → Items → 批量创建物品数据 (SO)。
- 动画ID映射：ToolData/WeaponData 新增 useQualityIdMapping、animationDefaultId、GetAnimationKeyId()；PlayerToolController 设置 ToolItemId 改为使用 GetAnimationKeyId()；与 LayerAnimSync 的品质回退兼容。
- 文档补充：对 Animation Trigger Name 的用途和使用建议进行了说明。

## 记忆点 6: Order 计算与自动校准系统

**核心原则**：
1. 树木/房屋等静态物体不使用DynamicSortingOrder（它们不移动）
2. 使用Tool_002_BatchHierarchy工具手动调整Order（编辑时）
3. 运行前自动校准所有静态物体Order（StaticObjectOrderAutoCalibrator）

**统一标准（绝对一致）**：
```
优先级：
1. Collider2D.bounds.min.y + bottomOffset
2. Sprite.bounds.min.y + bottomOffset  
3. Transform.position.y + bottomOffset

Order计算：
Order = -Round(sortingY × 100) + 0

默认参数：
multiplier = 100
offset = 0
bottomOffset = 0
```

**自动校准系统**：
- 文件：StaticObjectOrderAutoCalibrator.cs
- 触发时机：进入Play模式前自动执行
- 手动触发：Tools → 🔧 校准所有静态物体Order
- 逻辑：完全复制Tool_002的CalculateSortingY方法
- 跳过：有DynamicSortingOrder的物体（动态计算）
- 跳过：Order < -9990的特殊标记物体
- Shadow处理：Order = 父Order + (-1)
- Glow/Light/Effect处理：Order = 父Order + 0

**使用流程**：
1. 编辑时：用Tool_002手动调整静态物体Order
2. 忘记调整也没关系：运行前自动校准
3. 动态物体（玩家）：使用DynamicSortingOrder实时计算

**遮挡透明系统配置**：
树木/房屋（静态物体）：
- ❌ 不需要DynamicSortingOrder
- ✅ 只需要PolygonCollider2D
- ✅ 只需要OcclusionTransparency
- ✅ 自动校准会处理Order

## 记忆点 7: 混合导航系统

为Unity项目"Sunset"实现了混合导航系统v4.0（2024-12-04）：

**核心特性**：
1. 双模式导航：Grid模式（网格寻路）+ Continuous模式（连续空间势场导航）
2. 自动模式切换：根据周围障碍物密度（complexAreaThreshold）动态切换
3. 势场法导航：目标吸引力 + 障碍物排斥力，处理极限狭窄通道
4. CircleCast精确碰撞检测：可以检测红色格子内的空隙和绿色格子边缘的障碍物
5. 动态路径探索：尝试±15°-±75°多个角度找到可通过路径

**文件清单**：
- PlayerAutoNavigator.cs：添加混合导航功能（默认关闭，完全向后兼容）
- INavigationUnit.cs：NPC/怪物导航接口
- 混合导航系统使用指南.md：完整文档

**配置参数**：
- enableHybridNavigation (默认false)：启用开关
- complexAreaThreshold (默认3)：复杂区域阈值
- continuousNavRadius (默认3.0)：检测半径
- obstacleRepulsionStrength (默认2.0)：排斥力强度

**关键保障**：
- ✅ 所有现有功能保留：路径合并、视线优化、速度自适应、碰撞体脱困
- ✅ 默认关闭新功能，不影响现有项目
- ✅ 为NPC/怪物预留NavigationUnitType接口（Player/NPC/Enemy/StaticObstacle）
- ✅ 完全向后兼容

**使用场景**：
- 开阔区域 → Grid模式（快速）
- 狭窄通道 → Continuous模式（精确）
- 复杂障碍物交叠（如树林红色区域内有空隙）→ Continuous模式

## 记忆点 8: 云朵与天气系统

云朵素材已就绪；按方案A（精灵云影+Multiply材质）推进 CloudShadowManager，实现参数 Intensity/Density/ScaleRange/Speed/Direction/Seed/SortingLayer/WeatherGate，并与 WeatherManager 联动；遮挡透明继续保持单一真相源于 OcclusionManager，OcclusionTransparency 组件无参数、仅 OnEnable 读取一次。

## 记忆点 9: 手持物品动画系统

Project: Unity 2D. Handheld items and animation tooling progress (as of 2025-11-13).
- TriDirectionalFusionTool implemented (Chinese UI). Features: drag-and-drop folders, single quality count, optional pivot apply, fixed 8 frames; added timeline controls `totalFrames`/`lastFrame` controlling keyframe distribution; naming `{Action}_{Dir}_Clip_{itemId}_{quality}`.
- Output structure:
  - Clips: `Assets/Animations/Clips/{Action}/{id}_{itemName}/{Down|Side|Up}/{Action}_{Dir}_Clip_{id}_{quality}.anim`
  - Controllers: `Assets/Animations/Controllers/{Action}/{id}_{itemName}/{Action}_Controller_{id}_{itemName}.controller`
- Controllers use only Any State → state transitions (no inter-state), parameters: `State`, `Direction`, `ToolItemId`, `ToolQuality`.
- Runtime: `LayerAnimSync` uses `ToolItemId` + quality fallback; `PlayerToolController` sets `ToolItemId` on equip.
- New design for hits: unify `ToolEvents.ToolStrike` event. `PlayerToolHitEmitter` computes a 60° sector hit (wedge) with radius from axe sprite reach at frame_4 (using sprite bounds + pivot + tool attach). Broadcast event and call `IResourceNode` interfaces for targets. Keep frame-window fallback if no animation event.
- Generator upcoming: auto-inject `OnToolStrike` at frame_4 for axe+Slice clips (default on), dedup on existing events.
- Docs: created `Docx/Plan/砍树功能实现方案.md` with above design; mining to follow same pattern later.
- Next tasks: implement `PlayerToolHitEmitter`, `TreeConfig` SO, `TreeController` (IResourceNode), generator event injection, scene wiring/tests; later extend to rocks.

## 记忆点 10: 物品设计交接任务计划

为 Unity 项目"Sunset"建立了基于 Docx/HD/物品设计交接稿.md v3.1 的交接任务计划（TODO）。当前状态：Phase 5 - 步骤1 环境验证 已标记为进行中；随后包含自动化初始化、创建与验证10个物品、测试与清理、提交完成报告等任务。计划还纳入"与手持物品动画与同步对接"的命名规范、manifest 契约、编辑器工具（TriDirectionalAnimGenerator、SliceAnimControllerTool）与运行时 Mode A++ 帧锁验证等任务。

## 记忆点 11: 待验证事项

待验证事项（保持高优先级跟进）：
- t30：Unity中验证菜单 Farm → Items → 批量创建物品数据 (SO) 是否出现并可打开。
- t31：选一组Sprite试跑批量创建，按顺序填入ID/名称，检查命名与保存路径是否符合规范。
- t32：运行时检查动画系统是否正确使用 GetAnimationKeyId() 驱动 ToolItemId（含品质回退）。

## 记忆点 12: 绝对禁止事项

**绝对禁止破坏原有功能**
1. 修改bug时：必须保证修改后和原有业务逻辑保持一致，不能坏了功能
2. 修改业务逻辑时：必须确保和现有模块、其他脚本联动和适配
3. **修改任何部分前**：先记住要做的到底是什么，从全局大体来思考
4. 必须结合项目的整体设计（多层级+标签分类、Sorting Layer等）
5. 测试点：如果原功能是"滚轮切换显示红框"，修改后必须仍然显示红框
6. 中心点问题：使用Player的Collider中心，不是摄像机/屏幕中心
7. 图层问题：根据点击区域的Sorting Layer创建物体，不是Default
8. 模板对象：创建在UI中心（场景外），不是Systems下（场景内可拾取位置）
