# 设计文档 - StoneController 优化

## 概述

本设计文档描述 StoneController 的编辑器界面优化和 Sprite 管理系统重构，参考 TreeControllerV2Editor 的实现模式。

## 架构

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        StoneController 优化架构                          │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌─────────────────────┐     ┌─────────────────────────────────────┐    │
│  │  StoneControllerEditor │     │        StoneController              │    │
│  │  (CustomEditor)       │     │        (MonoBehaviour)              │    │
│  ├─────────────────────┤     ├─────────────────────────────────────┤    │
│  │ - 分组折叠 UI        │     │ - 运行时参数检测                    │    │
│  │ - 阶段配置展示       │     │ - Sprite 自动加载                   │    │
│  │ - 状态预览信息       │     │ - OreIndex 范围钳制                 │    │
│  │ - 文件夹拖拽扫描     │     │ - 血量自动重置                      │    │
│  └─────────────────────┘     └─────────────────────────────────────┘    │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

## 组件和接口

### StoneController 新增/修改字段

```csharp
#region 序列化字段 - Sprite 配置（新增）
[Header("━━━━ Sprite 配置 ━━━━")]
[Tooltip("Sprite 根文件夹（如 Assets/Sprites/Stone）")]
[SerializeField] private DefaultAsset spriteRootFolder;

[Tooltip("是否使用缓存的 Sprite（推荐开启）")]
[SerializeField] private bool useSpriteCache = true;

[Tooltip("缓存的 Sprite 字典（编辑器自动填充）")]
[SerializeField] private List<StoneSpriteEntry> spriteCache = new List<StoneSpriteEntry>();
#endregion

#region 运行时调试（新增）
// 用于检测 Inspector 参数变化
private OreType lastOreType;
private StoneStage lastStage;
private int lastOreIndex;
#endregion
```

### StoneSpriteEntry 数据结构（新增）

```csharp
[System.Serializable]
public class StoneSpriteEntry
{
    public OreType oreType;
    public StoneStage stage;
    public int oreIndex;
    public Sprite sprite;
    
    public string Key => $"{oreType}_{stage}_{oreIndex}";
}
```

### StoneControllerEditor 结构

```csharp
[CustomEditor(typeof(StoneController))]
public class StoneControllerEditor : Editor
{
    // 折叠状态
    private bool showStageConfigs = true;
    private bool showCurrentState = true;
    private bool showSpriteConfig = true;
    private bool showStatusPreview = true;
    private bool showDropConfig = true;
    private bool showSoundSettings = true;
    private bool showDebugSettings = true;
    
    // 阶段配置折叠
    private bool[] stageConfigFoldouts = new bool[4];
    
    // 阶段名称
    private static readonly string[] StageNames = { "M1 (最大)", "M2 (中等)", "M3 (最小)", "M4 (装饰)" };
}
```

## 数据模型

### OreIndex 范围配置

| 阶段 | OreIndex 范围 | 说明 |
|------|--------------|------|
| M1 | 0-4 | 5 种含量变体 |
| M2 | 0-4 | 5 种含量变体 |
| M3 | 0-3 | 4 种含量变体 |
| M4 | 0-7 | 8 种装饰变体 |

### Sprite 命名规范

```
格式: Stone_{OreType}_{Stage}_{OreIndex}
示例: Stone_C1_M1_4, Stone_C2_M2_0, Stone_C3_M3_3
```

### 状态预览计算

```csharp
// 预计掉落计算
int totalOre = StoneDropConfig.GetOreTotal(currentStage, oreIndex);
int totalStone = StoneDropConfig.GetStoneTotal(currentStage);

// 所需镐子等级
string requiredPickaxe = MaterialTierHelper.GetRequiredPickaxeForOre(oreType);
```

## 正确性属性

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. 
Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Sprite 名称生成正确性
*For any* OreType、StoneStage 和 OreIndex 组合，生成的 Sprite 名称应符合 `Stone_{OreType}_{Stage}_{OreIndex}` 格式
**Validates: Requirements 2.1**

### Property 2: 阶段切换血量重置
*For any* StoneStage 切换，血量应重置为该阶段的默认值（M1=36, M2=17, M3=9, M4=4）
**Validates: Requirements 2.2**

### Property 3: OreIndex 范围钳制
*For any* OreIndex 值，应被钳制到当前阶段的有效范围内（M1/M2: 0-4, M3: 0-3, M4: 0-7）
**Validates: Requirements 2.4, 6.1-6.4**

## 错误处理

1. **Sprite 不存在**：输出警告日志，保持当前 Sprite
2. **文件夹路径无效**：显示错误提示，不执行扫描
3. **OreIndex 超出范围**：自动钳制到有效范围

## 测试策略

### 单元测试
- Sprite 名称生成逻辑
- OreIndex 范围钳制逻辑
- 血量重置逻辑

### 手动测试
- Inspector UI 显示正确性
- 运行时参数修改实时更新
- 文件夹拖拽扫描功能
