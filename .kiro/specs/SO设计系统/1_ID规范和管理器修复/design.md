# SO ID 规范完善和管理器修复 - 设计文档

## 概述

本设计旨在解决两个紧急问题：
1. 完善 SO ID 分配规范，明确所有物品类型的 ID 范围
2. 修复 TimeManager、SeasonManager、WeatherSystem 的 DontDestroyOnLoad 警告

---

## 问题分析

### 问题 1：ID 范围不完整

**现状**：
- KeyLockData.cs 中硬编码了 1420-1499 的 ID 范围检查
- SO 参数设计文档中**没有定义**钥匙/锁的 ID 范围
- 现有钥匙 SO 使用了错误的 ID（100-105）
- 导致控制台出现大量警告

**影响**：
- 开发者不知道应该使用什么 ID
- 批量生成工具使用错误的默认 ID
- 控制台警告干扰开发

### 问题 2：DontDestroyOnLoad 失败

**现状**：
- TimeManager、SeasonManager、WeatherSystem 调用 `DontDestroyOnLoad(gameObject)`
- 这三个管理器不是场景的根物体（可能在某个父物体下）
- Unity 要求 DontDestroyOnLoad 只能用于根物体

**错误信息**：
```
DontDestroyOnLoad only works for root GameObjects or components on root GameObjects.
```

**影响**：
- 场景切换时管理器可能被销毁
- 时间、季节、天气状态丢失
- 控制台警告干扰开发

---

## 解决方案

### 方案 1：完善 ID 分配规范

**更新后的 ID 分配规范**：

```
0XXX: 工具和武器
    00XX: 农业工具（锄头、水壶、镰刀、钓鱼竿）
    01XX: 采集工具（镐子、斧头）
    02XX: 武器（剑、弓、法杖）

1XXX: 种植类
    10XX: 种子
    11XX: 作物
    12XX: 树苗（Sapling）
        1200-1219: 树苗
    13XX: 建筑材料
        1300-1399: 建筑材料
    14XX: 钥匙和锁
        1420-1499: 钥匙/锁（KeyLockData）

2XXX: 动物产品
    20XX: 畜牧产品
    21XX: 肉类
    22XX: 水产

3XXX: 矿物和材料
    30XX: 矿石（未加工）
    31XX: 锭（加工后）
    32XX: 自然材料（木、石）
    33XX: 怪物掉落

4XXX: 消耗品
    40XX: 药水

5XXX: 食品
    50XX: 简单料理
    51XX: 高级料理

6XXX: 家具
7XXX: 特殊物品
```

**说明**：
- **12XX**：树苗（Sapling），已在使用（1200-1202）
- **13XX**：建筑材料，预留给未来的建筑系统
- **14XX**：钥匙和锁，特别是 1420-1499 范围

---

### 方案 2：修复 DontDestroyOnLoad

**方案 A：确保管理器是根物体（推荐）**

在场景中，确保 TimeManager、SeasonManager、WeatherSystem 是根物体：

```
Scene
├── TimeManager          ← 根物体
├── SeasonManager        ← 根物体
├── WeatherSystem        ← 根物体
├── ─── MANAGERS ───
│   └── (其他管理器)
└── ...
```

**优点**：
- 简单直接
- 符合 Unity 的设计
- 不需要修改代码

**缺点**：
- 需要调整场景层级

**方案 B：使用 transform.root（备选）**

修改代码，对根物体调用 DontDestroyOnLoad：

```csharp
private void Awake()
{
    if (instance == null)
    {
        instance = this;
        // 对根物体调用 DontDestroyOnLoad
        DontDestroyOnLoad(transform.root.gameObject);
        Initialize();
    }
    else if (instance != this)
    {
        Destroy(gameObject);
    }
}
```

**优点**：
- 不需要调整场景层级
- 代码修改简单

**缺点**：
- 如果多个管理器在同一个父物体下，会重复调用
- 可能影响其他子物体

**方案 C：创建独立的根物体（推荐）**

创建一个名为 "PersistentManagers" 的根物体，将所有需要持久化的管理器放在其下：

```
Scene
├── PersistentManagers   ← 根物体，调用 DontDestroyOnLoad
│   ├── TimeManager
│   ├── SeasonManager
│   └── WeatherSystem
├── ─── MANAGERS ───
│   └── (其他管理器)
└── ...
```

在 PersistentManagers 上添加一个简单的脚本：

```csharp
public class PersistentManagers : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
```

然后从各个管理器中移除 DontDestroyOnLoad 调用。

**优点**：
- 集中管理持久化逻辑
- 清晰的场景结构
- 避免重复调用

**缺点**：
- 需要创建新的脚本
- 需要调整场景层级

---

## 推荐方案

### ID 规范：直接更新文档

1. 更新 `Docx/设计/SO/SO参数设计.md`
2. 更新 `.kiro/steering/so-design.md`
3. 更新批量生成工具的默认 ID

### DontDestroyOnLoad：方案 C（创建 PersistentManagers）

1. 创建 `PersistentManagers.cs` 脚本
2. 在场景中创建 PersistentManagers 根物体
3. 将 TimeManager、SeasonManager、WeatherSystem 移到其下
4. 从各个管理器中移除 DontDestroyOnLoad 调用

---

## 组件设计

### PersistentManagers.cs

```csharp
using UnityEngine;

/// <summary>
/// 持久化管理器容器
/// 确保所有子管理器在场景切换时不被销毁
/// </summary>
public class PersistentManagers : MonoBehaviour
{
    private static PersistentManagers instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("<color=cyan>[PersistentManagers] 初始化完成，管理器将在场景切换时保持</color>");
        }
        else
        {
            Debug.LogWarning("<color=yellow>[PersistentManagers] 检测到重复实例，销毁</color>");
            Destroy(gameObject);
        }
    }
}
```

### TimeManager.cs 修改

```csharp
private void Awake()
{
    if (instance == null)
    {
        instance = this;
        // ❌ 移除：DontDestroyOnLoad(gameObject);
        // ✅ 由 PersistentManagers 统一处理
        Initialize();
    }
    else if (instance != this)
    {
        Destroy(gameObject);
    }
}
```

### SeasonManager.cs 修改

```csharp
private void Awake()
{
    if (Instance == null)
    {
        Instance = this;
        // ❌ 移除：DontDestroyOnLoad(gameObject);
        // ✅ 由 PersistentManagers 统一处理
    }
    else
    {
        Destroy(gameObject);
    }
}
```

### WeatherSystem.cs 修改

```csharp
private void Awake()
{
    if (instance == null)
    {
        instance = this;
        // ❌ 移除：DontDestroyOnLoad(gameObject);
        // ✅ 由 PersistentManagers 统一处理
    }
    else if (instance != this)
    {
        Destroy(gameObject);
    }
}
```

---

## 批量工具更新

### Tool_BatchItemSOGenerator.cs

更新默认 ID 范围：

```csharp
// 钥匙/锁
if (selectedType == typeof(KeyLockData))
{
    startID = 1420; // 默认从 1420 开始
    EditorGUILayout.HelpBox("钥匙/锁 ID 范围：1420-1499", MessageType.Info);
}

// 树苗
if (selectedType == typeof(SaplingData))
{
    startID = 1200; // 默认从 1200 开始
    EditorGUILayout.HelpBox("树苗 ID 范围：1200-1219", MessageType.Info);
}

// 建筑材料
if (selectedType == typeof(BuildingMaterialData))
{
    startID = 1300; // 默认从 1300 开始
    EditorGUILayout.HelpBox("建筑材料 ID 范围：1300-1399", MessageType.Info);
}
```

---

## 数据迁移

### 修复现有钥匙 ID

**方案 A：手动修改**

1. 打开每个钥匙 SO
2. 修改 itemID：
   - Key_0_0: 100 → 1420
   - Key_1_0: 101 → 1421
   - Key_2_0: 102 → 1422
   - Key_3_0: 103 → 1423
   - Key_4_0: 104 → 1424
   - Key_5_0: 105 → 1425

**方案 B：使用批量修改工具**

1. 选中所有钥匙 SO
2. 打开批量修改工具
3. 勾选"修改 itemID"
4. 设置新的 ID 范围
5. 批量修改

**方案 C：创建 ID 迁移工具（推荐）**

创建一个编辑器工具，自动扫描并修复错误的 ID：

```csharp
[MenuItem("Tools/🔧 修复钥匙 ID")]
public static void FixKeyIDs()
{
    var keys = AssetDatabase.FindAssets("t:KeyLockData")
        .Select(guid => AssetDatabase.LoadAssetAtPath<KeyLockData>(
            AssetDatabase.GUIDToAssetPath(guid)))
        .Where(k => k != null && k.itemID < 1420)
        .OrderBy(k => k.itemID)
        .ToList();

    if (keys.Count == 0)
    {
        Debug.Log("[FixKeyIDs] 没有需要修复的钥匙");
        return;
    }

    int newID = 1420;
    foreach (var key in keys)
    {
        int oldID = key.itemID;
        key.itemID = newID;
        EditorUtility.SetDirty(key);
        Debug.Log($"[FixKeyIDs] {key.name}: {oldID} → {newID}");
        newID++;
    }

    AssetDatabase.SaveAssets();
    Debug.Log($"<color=green>[FixKeyIDs] 完成！修复了 {keys.Count} 个钥匙</color>");
}
```

---

## 测试策略

### 单元测试

**测试 ID 范围验证**：
```csharp
[Test]
public void KeyLockData_ValidID_NoWarning()
{
    var key = ScriptableObject.CreateInstance<KeyLockData>();
    key.itemID = 1420;
    // 验证：不应有警告
}

[Test]
public void KeyLockData_InvalidID_ShowWarning()
{
    var key = ScriptableObject.CreateInstance<KeyLockData>();
    key.itemID = 100;
    // 验证：应有警告
}
```

### 集成测试

**测试管理器持久化**：
1. 启动游戏
2. 记录当前时间、季节、天气
3. 切换场景
4. 验证：管理器仍然存在
5. 验证：时间、季节、天气状态保持不变

### 手动测试

**测试 ID 修复**：
1. 运行 ID 迁移工具
2. 检查所有钥匙 SO 的 ID
3. 启动游戏
4. 验证：控制台无 ID 警告

**测试 DontDestroyOnLoad**：
1. 启动游戏
2. 验证：控制台无 DontDestroyOnLoad 警告
3. 切换场景
4. 验证：管理器仍然存在

---

## 部署策略

### 阶段 1：文档更新

1. 更新 SO 参数设计文档
2. 更新 so-design.md steering 规则
3. 更新工作区 memory.md

### 阶段 2：代码修改

1. 创建 PersistentManagers.cs
2. 修改 TimeManager.cs
3. 修改 SeasonManager.cs
4. 修改 WeatherSystem.cs
5. 更新批量生成工具

### 阶段 3：场景调整

1. 在场景中创建 PersistentManagers 根物体
2. 将三个管理器移到其下
3. 添加 PersistentManagers 组件
4. 保存场景

### 阶段 4：数据迁移

1. 创建 ID 迁移工具
2. 运行工具修复钥匙 ID
3. 验证所有 SO 正常
4. 同步到 ItemDatabase

### 阶段 5：测试验证

1. 启动游戏
2. 验证控制台无警告
3. 测试场景切换
4. 验证管理器持久化

---

## 相关文件

### 核心脚本

| 文件 | 修改内容 |
|------|---------|
| `Assets/YYY_Scripts/Service/PersistentManagers.cs` | 新建 |
| `Assets/YYY_Scripts/Service/TimeManager.cs` | 移除 DontDestroyOnLoad |
| `Assets/YYY_Scripts/Service/SeasonManager.cs` | 移除 DontDestroyOnLoad |
| `Assets/YYY_Scripts/Service/WeatherSystem.cs` | 移除 DontDestroyOnLoad |

### 编辑器工具

| 文件 | 修改内容 |
|------|---------|
| `Assets/Editor/Tool_BatchItemSOGenerator.cs` | 更新默认 ID |
| `Assets/Editor/Tool_FixKeyIDs.cs` | 新建 ID 迁移工具 |

### 文档

| 文件 | 修改内容 |
|------|---------|
| `Docx/设计/SO/SO参数设计.md` | 添加 12XX、13XX、14XX 范围 |
| `.kiro/steering/so-design.md` | 更新 ID 分配规范 |
| `.kiro/specs/SO设计系统/memory.md` | 记录本次修复 |

### 场景

| 文件 | 修改内容 |
|------|---------|
| `Assets/000_Scenes/Primary.unity` | 调整管理器层级 |

---

*设计文档结束*
