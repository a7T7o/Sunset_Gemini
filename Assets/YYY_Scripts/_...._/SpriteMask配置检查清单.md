# 🔍 Sprite Mask不显示 - 完整排查清单

## 问题现象
- ✅ HandMask动画在播放（控制台有日志）
- ❌ 看不到遮罩效果
- ❌ 斧头完全不显示

---

## 🎯 核心原因：3个配置必须同时正确

### 1️⃣ **SpriteMask组件配置**（HandMask GameObject上）

#### A. Sprite字段
- 应该动态显示当前的Mask Sprite
- 在Play模式下观察是否在切换

#### B. **Mask Interaction设置** ⚠️ 关键！
```
必须设置为：Visible Inside Mask
```

#### C. **Sorting Layer配置**
选项1：使用Custom Range（推荐）
```
✅ Custom Range = 启用
✅ Front Sorting Layer = Player所在层（如Default）
✅ Front Sorting Order = Player的Order + 1（如果Player是5，这里设6）
✅ Back Sorting Layer = Player所在层
✅ Back Sorting Order = Player的Order - 1（如果Player是5，这里设4）
```

选项2：使用固定Layer
```
✅ Sorting Layer = 和Player相同（如Default）
✅ Order in Layer = Player的Order + 1
```

---

### 2️⃣ **斧头SpriteRenderer配置**（Axe GameObject上）

#### A. **Mask Interaction** ⚠️ 最关键！
```
❌ None（默认）           → 斧头不会被遮罩影响
✅ Visible Inside Mask    → 只在遮罩白色区域显示 ✅✅✅
❌ Visible Outside Mask   → 只在遮罩外显示
```
**这是99%的问题所在！**

#### B. Sorting Layer配置
```
✅ Sorting Layer = 和Player相同（如Default）
✅ Order in Layer = Player的Order + 1
   （要比Player高，但在Mask的Range范围内）
```

#### C. Sprite设置
```
✅ Sprite = 斧头的完整图（彩色的斧头sprite）
✅ Color = 白色（255,255,255,255）
```

---

### 3️⃣ **Mask Sprite本身**（黑白图）

#### 正确的遮罩Sprite应该是：
```
⬜ 白色区域 = 显示斧头的部分（手+斧头的形状）
⬛ 黑色区域 = 隐藏的部分（背景）
✅ Alpha通道 = 不透明（255）
```

#### ⚠️ 常见错误：
- 全黑图 → 什么都不显示
- 全白图 → 整个斧头都显示（失去遮罩效果）
- 颜色反了 → 看到的是反的

---

## 📋 立即检查步骤

### Step 1: 检查HandMask（遮罩GameObject）
1. 选中`HandMask` GameObject
2. 查看`SpriteMask`组件：
   ```
   Sprite: [应该显示当前的mask sprite]
   Mask Interaction: Visible Inside Mask ✅
   Custom Range: ✅ 启用
      Front Layer: Default（和Player相同）
      Front Order: 6（比Player大1）
      Back Layer: Default
      Back Order: 4（比Player小1）
   ```

### Step 2: 检查Axe（斧头GameObject）
1. 选中`Axe` GameObject（HandMask的子物体）
2. 查看`SpriteRenderer`组件：
   ```
   Sprite: [彩色的斧头图]
   Color: 白色(255,255,255,255)
   
   Additional Settings:
      Mask Interaction: Visible Inside Mask ✅✅✅ 最关键！
   
   Sorting Layer: Default（和Player相同）
   Order in Layer: 6（比Player大1，在Mask Range内）
   ```

### Step 3: 运行时Debug
1. 进入Play模式
2. 按下攻击键（如'2'）
3. 在Inspector中观察`HandMask`的`SpriteMask`组件
4. **Sprite字段应该在动态切换！**
5. 打开Console，应该看到：
   ```
   [HandMask] 动作改变: State=6, Type=0, Direction=0
   [HandMask] 更新: Slice Frame 0/3, Dir=0
   ```

---

## 🔧 快速修复脚本

如果手动设置太麻烦，创建这个Editor脚本一键设置：

```csharp
// Assets/Editor/SpriteMaskSetupHelper.cs
using UnityEngine;
using UnityEditor;

public class SpriteMaskSetupHelper : EditorWindow
{
    [MenuItem("Tools/修复Sprite Mask配置")]
    static void ShowWindow()
    {
        GetWindow<SpriteMaskSetupHelper>("Mask修复工具");
    }
    
    private GameObject handMaskGO;
    private GameObject axeGO;
    private int playerSortingOrder = 5;
    
    void OnGUI()
    {
        EditorGUILayout.LabelField("Sprite Mask配置修复", EditorStyles.boldLabel);
        
        handMaskGO = EditorGUILayout.ObjectField("HandMask", handMaskGO, typeof(GameObject), true) as GameObject;
        axeGO = EditorGUILayout.ObjectField("Axe", axeGO, typeof(GameObject), true) as GameObject;
        playerSortingOrder = EditorGUILayout.IntField("Player Sorting Order", playerSortingOrder);
        
        if (GUILayout.Button("一键修复"))
        {
            FixSpriteMask();
        }
    }
    
    void FixSpriteMask()
    {
        if (handMaskGO == null || axeGO == null)
        {
            EditorUtility.DisplayDialog("错误", "请先拖入GameObject！", "确定");
            return;
        }
        
        // 修复SpriteMask
        SpriteMask mask = handMaskGO.GetComponent<SpriteMask>();
        if (mask != null)
        {
            mask.isCustomRangeActive = true;
            mask.frontSortingLayerID = SortingLayer.NameToID("Default");
            mask.frontSortingOrder = playerSortingOrder + 1;
            mask.backSortingLayerID = SortingLayer.NameToID("Default");
            mask.backSortingOrder = playerSortingOrder - 1;
            
            Debug.Log($"✅ SpriteMask配置完成！Range: {playerSortingOrder-1} ~ {playerSortingOrder+1}");
        }
        
        // 修复Axe SpriteRenderer
        SpriteRenderer axeSR = axeGO.GetComponent<SpriteRenderer>();
        if (axeSR != null)
        {
            axeSR.maskInteraction = SpriteMaskInteraction.VisibleInsideMask; // 关键！
            axeSR.sortingLayerName = "Default";
            axeSR.sortingOrder = playerSortingOrder + 1;
            
            Debug.Log("✅ Axe SpriteRenderer配置完成！Mask Interaction = VisibleInsideMask");
        }
        
        EditorUtility.SetDirty(handMaskGO);
        EditorUtility.SetDirty(axeGO);
        
        EditorUtility.DisplayDialog("完成", "Sprite Mask配置已修复！\n请进入Play模式测试。", "确定");
    }
}
```

---

## ❓ 常见问题

### Q: 为什么要设置Custom Range？
A: 确保Mask只影响特定Order范围内的物体，不影响其他UI或背景。

### Q: 为什么Axe的Order要在Range内？
A: 只有在`[Back Order, Front Order]`范围内的物体才会被Mask影响。

### Q: 我的遮罩Sprite是彩色的行吗？
A: 理论上可以，但最好用纯黑白图，避免意外的颜色混合。

### Q: 可以用alpha通道做遮罩吗？
A: 不推荐。Unity的Sprite Mask主要看RGB值（白=显示，黑=隐藏）。

---

## 📸 正确配置的截图参考

### HandMask - SpriteMask组件
```
┌─────────────────────────────────┐
│ Sprite Mask                     │
├─────────────────────────────────┤
│ Sprite: [slice_down_mask_0]    │ ← 动态切换
│                                 │
│ ✅ Custom Range                 │
│   Front: Default, Order: 6      │
│   Back:  Default, Order: 4      │
└─────────────────────────────────┘
```

### Axe - SpriteRenderer组件
```
┌─────────────────────────────────┐
│ Sprite Renderer                 │
├─────────────────────────────────┤
│ Sprite: [axe_full]             │
│ Color: ⬜ 白色                  │
│                                 │
│ Additional Settings:            │
│ ✅ Mask Interaction:            │
│    Visible Inside Mask          │ ← 最关键！
│                                 │
│ Sorting Layer: Default          │
│ Order in Layer: 6               │
└─────────────────────────────────┘
```

---

## 🎬 下一步
1. 按照Step 1-3检查配置
2. 特别注意**Axe的Mask Interaction**
3. 进入Play模式测试
4. 如果还不行，截图发给我看：
   - HandMask的Inspector
   - Axe的Inspector
   - 运行时的Scene视图

Good luck! 🍀


