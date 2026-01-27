# 农田系统重构 - 设计文档

**创建日期**: 2026-01-24  
**版本**: 1.0  
**状态**: 待审核

---

## 1. 架构概述

### 1.1 设计原则

1. **单一职责**：每个类只负责一个明确的功能
2. **数据与逻辑分离**：数据类纯数据，控制器处理逻辑
3. **事件驱动**：通过事件解耦系统间依赖
4. **性能优先**：避免不必要的遍历和对象创建

### 1.2 系统边界

```
农田系统 (FarmingSystem)
├── FarmingManager（协调者）
│   ├── 耕地操作入口
│   ├── 种植操作入口
│   ├── 浇水操作入口
│   └── 收获操作入口
│
├── FarmTileManager（耕地状态管理）
│   ├── 耕地字典管理
│   ├── 多楼层 Tilemap 管理
│   └── 耕地状态查询
│
├── CropManager（作物生命周期）
│   ├── 作物创建/销毁
│   ├── 生长逻辑
│   └── 收获逻辑
│
└── FarmVisualManager（视觉更新）
    ├── Tile 视觉更新
    ├── 水渍效果
    └── 作物 Sprite 更新
```

### 1.3 与现有系统的集成

```
┌─────────────────────────────────────────────────────────────┐
│                      GameInputManager                        │
│  (工具使用入口：锄头、水壶、收获)                              │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│                      FarmingManager                          │
│  TillSoil() / PlantSeed() / WaterTile() / HarvestCrop()     │
└─────────────────────┬───────────────────────────────────────┘
                      │
          ┌───────────┼───────────┐
          ▼           ▼           ▼
┌─────────────┐ ┌───────────┐ ┌─────────────┐
│InventoryService│ │TimeManager│ │ ItemDatabase │
│ AddItem()   │ │OnDayChanged│ │GetItemByID()│
│ RemoveItem()│ │OnHourChanged│ │            │
└─────────────┘ └───────────┘ └─────────────┘
```

---

## 2. 数据结构设计

### 2.1 FarmTileData（耕地格子数据）

```csharp
[System.Serializable]
public class FarmTileData
{
    // 位置
    public Vector3Int position;
    public int layerIndex;  // 所属楼层（0=LAYER1, 1=LAYER2, 2=LAYER3）
    
    // 耕地状态
    public bool isTilled;
    
    // 浇水状态（逻辑）
    public bool wateredToday;      // 今天是否浇水（记录参数）
    public bool wateredYesterday;  // 昨天是否浇水（影响生长）
    
    // 浇水状态（视觉）
    public float waterTime;        // 浇水时间（小时，用于视觉切换）
    public SoilMoistureState moistureState;
    public int puddleVariant;      // 水渍变体索引（0-2）
    
    // 作物
    public CropInstanceData cropData;  // 作物数据（可序列化）
    
    // 方法
    public bool CanPlant() => isTilled && cropData == null;
    public bool HasCrop() => cropData != null;
}
```

### 2.2 CropInstanceData（作物实例数据 - 纯数据）

```csharp
[System.Serializable]
public class CropInstanceData
{
    // 种子信息
    public int seedDataID;         // 种子 ID（用于从 ItemDatabase 获取 SeedData）
    
    // 生长状态
    public int currentStage;       // 当前生长阶段
    public int plantedDay;         // 种植时的游戏天数
    public int grownDays;          // 实际生长天数
    
    // 浇水状态
    public int daysWithoutWater;   // 连续未浇水天数
    public bool isWithered;        // 是否枯萎
    
    // 收获状态
    public int harvestCount;       // 已收获次数
    public int lastHarvestDay;     // 上次收获天数
    
    // 品质
    public int quality;            // 作物品质
}
```

### 2.3 LayerTilemaps（楼层 Tilemap 配置）

```csharp
[System.Serializable]
public class LayerTilemaps
{
    public string layerName;           // "LAYER 1", "LAYER 2", "LAYER 3"
    public Tilemap farmlandTilemap;    // 耕地 Tilemap
    public Tilemap waterPuddleTilemap; // 水渍叠加 Tilemap
    public Tilemap groundTilemap;      // 地面 Tilemap（用于检测可耕作区域）
}
```

### 2.4 SoilMoistureState（土壤湿度状态）

```csharp
public enum SoilMoistureState
{
    Dry = 0,            // 干燥（未浇水）
    WetWithPuddle = 1,  // 湿润+水渍（浇水后2小时内）
    WetDark = 2         // 湿润+深色（浇水2小时后）
}
```

---

## 3. 组件设计

### 3.1 FarmingManager（协调者）

```csharp
public class FarmingManager : MonoBehaviour
{
    public static FarmingManager Instance { get; private set; }
    
    [Header("子管理器")]
    [SerializeField] private FarmTileManager tileManager;
    [SerializeField] private CropManager cropManager;
    [SerializeField] private FarmVisualManager visualManager;
    
    [Header("系统引用")]
    [SerializeField] private ItemDatabase itemDatabase;
    
    // 公共接口
    public bool TillSoil(Vector3 worldPosition);
    public bool PlantSeed(Vector3 worldPosition, int seedDataID);
    public bool WaterTile(Vector3 worldPosition);
    public bool HarvestCrop(Vector3 worldPosition, out int cropID, out int amount);
    
    // 内部获取 ItemDatabase
    private ItemDatabase GetDatabase()
    {
        if (itemDatabase != null) return itemDatabase;
        var inventoryService = FindFirstObjectByType<InventoryService>();
        if (inventoryService != null) return inventoryService.Database;
        return null;
    }
}
```

### 3.2 FarmTileManager（耕地状态管理）

```csharp
public class FarmTileManager : MonoBehaviour
{
    [Header("多楼层配置")]
    [SerializeField] private LayerTilemaps[] layerTilemaps;
    
    // 数据存储：按楼层分组
    private Dictionary<int, Dictionary<Vector3Int, FarmTileData>> farmTilesByLayer;
    
    // 今天浇水的耕地（用于优化 OnHourChanged 遍历）
    private HashSet<(int layer, Vector3Int pos)> wateredTodayTiles;
    
    // 公共接口
    public FarmTileData GetTileData(int layerIndex, Vector3Int cellPosition);
    public bool CreateTile(int layerIndex, Vector3Int cellPosition);
    public void SetWatered(int layerIndex, Vector3Int cellPosition, float waterTime);
    public void ResetDailyWaterState();  // OnDayChanged 调用
    
    // 楼层检测
    public int GetCurrentLayerIndex(Vector3 playerPosition);
    public LayerTilemaps GetLayerTilemaps(int layerIndex);
}
```

### 3.3 CropManager（作物生命周期）

```csharp
public class CropManager : MonoBehaviour
{
    [Header("作物配置")]
    [SerializeField] private GameObject cropPrefab;
    [SerializeField] private Transform cropsContainer;
    
    // 运行时作物实例
    private Dictionary<(int layer, Vector3Int pos), CropController> activeCrops;
    
    // 公共接口
    public CropController CreateCrop(int layerIndex, Vector3Int cellPosition, SeedData seedData, int plantedDay);
    public void DestroyCrop(int layerIndex, Vector3Int cellPosition);
    public void ProcessDailyGrowth(FarmTileData tileData);  // OnDayChanged 调用
    
    // 收获
    public bool TryHarvest(int layerIndex, Vector3Int cellPosition, out int cropID, out int amount);
}
```

### 3.4 FarmVisualManager（视觉更新）

```csharp
public class FarmVisualManager : MonoBehaviour
{
    [Header("Tile 资源")]
    [SerializeField] private TileBase dryFarmlandTile;
    [SerializeField] private TileBase[] wetPuddleTiles;  // 3 种水渍变体
    [SerializeField] private TileBase wetDarkTile;
    
    [Header("音效")]
    [SerializeField] private AudioClip tillingSoundClip;
    [SerializeField] private AudioClip wateringSoundClip;
    [SerializeField] private AudioClip harvestSoundClip;
    
    [Header("粒子效果")]
    [SerializeField] private GameObject tillingParticlePrefab;
    [SerializeField] private GameObject wateringParticlePrefab;
    
    // 对象池
    private Queue<GameObject> tillingParticlePool;
    private Queue<GameObject> wateringParticlePool;
    
    // 公共接口
    public void UpdateTileVisual(LayerTilemaps tilemaps, Vector3Int cellPosition, FarmTileData tileData);
    public void PlayTillingEffects(Vector3 worldPosition);
    public void PlayWateringEffects(Vector3 worldPosition);
    public void PlayHarvestEffects(Vector3 worldPosition);
}
```

### 3.5 CropController（作物控制器 - MonoBehaviour）

```csharp
public class CropController : MonoBehaviour
{
    [Header("组件引用")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    [Header("成熟闪烁")]
    [SerializeField] private bool enableMatureGlow = true;
    [SerializeField] private Color glowColor = new Color(1f, 1f, 0.8f);
    [SerializeField] private float glowSpeed = 2f;
    
    // 运行时数据
    private SeedData seedData;
    private CropInstanceData instanceData;
    private bool isMature;
    
    // 枯萎颜色（统一）
    private static readonly Color WitheredColor = new Color(0.8f, 0.7f, 0.4f);
    
    // 公共接口
    public void Initialize(SeedData seedData, CropInstanceData data);
    public void UpdateVisuals();
    public bool IsMature();
    public bool IsWithered();
    
    // 生长
    public void Grow();
    public void SetWithered();
    
    // 收获后重置（可重复收获作物）
    public void ResetForReHarvest(int reGrowStage);
}
```

---

## 4. 流程设计

### 4.1 耕地流程

```
玩家右键点击地面（装备锄头）
    │
    ▼
GameInputManager.HandleToolUse(ToolType.Hoe, targetPosition)
    │
    ▼
FarmingManager.TillSoil(worldPosition)
    │
    ├─► 检测当前楼层 → layerIndex
    │
    ├─► 检测是否可耕作
    │   ├─ 有地面 Tile？
    │   ├─ 没有已有耕地？
    │   └─ 没有障碍物？
    │
    ├─► 创建 FarmTileData
    │
    ├─► 更新 Tilemap（Rule Tile 自动拼接）
    │
    └─► 播放音效和粒子
```

### 4.2 种植流程

```
玩家右键点击耕地（选中种子）
    │
    ▼
GameInputManager.HandleItemUse(SeedData, targetPosition)
    │
    ▼
FarmingManager.PlantSeed(worldPosition, seedDataID)
    │
    ├─► 检测当前楼层 → layerIndex
    │
    ├─► 获取 FarmTileData
    │   └─ 检查 CanPlant()
    │
    ├─► 检查季节
    │   └─ IsCorrectSeason(seedData)
    │
    ├─► 从背包移除种子
    │   └─ InventoryService.RemoveItem(seedDataID, 1)
    │
    ├─► 创建 CropInstanceData
    │
    ├─► 创建作物 GameObject
    │   └─ CropManager.CreateCrop()
    │
    └─► 播放音效
```

### 4.3 浇水流程

```
玩家右键点击耕地（装备水壶）
    │
    ▼
GameInputManager.HandleToolUse(ToolType.WateringCan, targetPosition)
    │
    ▼
FarmingManager.WaterTile(worldPosition)
    │
    ├─► 检测当前楼层 → layerIndex
    │
    ├─► 获取 FarmTileData
    │   └─ 检查 isTilled && !wateredToday
    │
    ├─► 更新浇水状态
    │   ├─ wateredToday = true
    │   ├─ waterTime = 当前时间
    │   ├─ moistureState = WetWithPuddle
    │   └─ puddleVariant = Random(0, 3)
    │
    ├─► 添加到 wateredTodayTiles
    │
    ├─► 更新 Tilemap 视觉
    │
    └─► 播放音效和粒子
```

### 4.4 每日生长流程

```
TimeManager.OnDayChanged
    │
    ▼
FarmingManager.OnDayChanged()
    │
    ├─► FarmTileManager.ResetDailyWaterState()
    │   │
    │   └─► 遍历所有耕地
    │       ├─ wateredYesterday = wateredToday
    │       ├─ wateredToday = false
    │       ├─ moistureState = Dry
    │       └─ 更新视觉（只更新状态变化的）
    │
    └─► CropManager.ProcessAllCrops()
        │
        └─► 遍历所有作物
            │
            ├─► 检查 wateredYesterday
            │   ├─ true → Grow()
            │   └─ false → daysWithoutWater++
            │
            ├─► 检查枯萎
            │   └─ daysWithoutWater >= 3 → SetWithered()
            │
            └─► 更新视觉
```

### 4.5 收获流程

```
玩家右键点击成熟作物
    │
    ▼
FarmingManager.HarvestCrop(worldPosition)
    │
    ├─► 检测当前楼层 → layerIndex
    │
    ├─► 获取 FarmTileData
    │   └─ 检查 HasCrop()
    │
    ├─► 检查作物状态
    │   ├─ IsWithered() → 返回 false
    │   └─ !IsMature() → 返回 false
    │
    ├─► 计算收获数量
    │   └─ Random(harvestAmountRange.x, harvestAmountRange.y + 1)
    │
    ├─► 获取 CropData
    │   └─ ItemDatabase.GetItemByID(harvestCropID)
    │
    ├─► 添加到背包
    │   └─ InventoryService.AddItem(cropID, quality, amount)
    │
    ├─► 处理作物
    │   ├─ 可重复收获 → ResetForReHarvest()
    │   └─ 普通作物 → DestroyCrop()
    │
    └─► 播放音效
```

---

## 5. 正确性属性

### 5.1 耕地系统属性

| ID | 属性 | 描述 |
|----|------|------|
| P1.1 | 耕地唯一性 | 同一位置不能创建重复耕地 |
| P1.2 | 耕地前置条件 | 只有有地面 Tile 的位置才能创建耕地 |
| P1.3 | 楼层隔离 | 不同楼层的耕地数据独立存储 |

### 5.2 种植系统属性

| ID | 属性 | 描述 |
|----|------|------|
| P2.1 | 种植消耗 | 种植成功后，背包中种子数量减少 1 |
| P2.2 | 种植前置条件 | 只有已耕作且无作物的耕地才能种植 |
| P2.3 | 季节限制 | 非 AllSeason 种子只能在对应季节种植 |

### 5.3 浇水系统属性

| ID | 属性 | 描述 |
|----|------|------|
| P3.1 | 每日一次 | 每块耕地每天只能浇水一次 |
| P3.2 | 视觉状态转换 | 浇水后 2 小时内为 WetWithPuddle，之后为 WetDark |
| P3.3 | 每日重置 | 每天开始时所有耕地恢复 Dry 状态 |

### 5.4 生长系统属性

| ID | 属性 | 描述 |
|----|------|------|
| P4.1 | 浇水生长 | 昨天浇水的作物，今天生长天数 +1 |
| P4.2 | 枯萎条件 | 连续 3 天未浇水的作物枯萎 |
| P4.3 | 成熟条件 | 生长天数 >= growthDays 时作物成熟 |

### 5.5 收获系统属性

| ID | 属性 | 描述 |
|----|------|------|
| P5.1 | 收获添加 | 收获成功后，背包中作物数量增加 |
| P5.2 | 收获前置条件 | 只有成熟且未枯萎的作物才能收获 |
| P5.3 | 可重复收获 | 可重复收获作物收获后回退阶段，不销毁 |

---

## 6. 性能优化设计

### 6.1 OnHourChanged 优化

```csharp
// 只遍历今天浇水的耕地
private HashSet<(int layer, Vector3Int pos)> wateredTodayTiles;

private void OnHourChanged(int currentHour)
{
    float currentTime = timeManager.GetHour() + timeManager.GetMinute() / 60f;
    
    foreach (var (layer, pos) in wateredTodayTiles)
    {
        var tileData = tileManager.GetTileData(layer, pos);
        if (tileData == null) continue;
        
        float hoursSinceWatering = currentTime - tileData.waterTime;
        if (hoursSinceWatering < 0) hoursSinceWatering += 20f; // 游戏一天 20 小时
        
        if (hoursSinceWatering >= hoursUntilPuddleDry)
        {
            if (tileData.moistureState == SoilMoistureState.WetWithPuddle)
            {
                tileData.moistureState = SoilMoistureState.WetDark;
                visualManager.UpdateTileVisual(...);
            }
        }
    }
}
```

### 6.2 OnDayChanged 优化

```csharp
private void OnDayChanged(int year, int day, int totalDays)
{
    foreach (var tileData in allTiles)
    {
        bool needsVisualUpdate = tileData.moistureState != SoilMoistureState.Dry;
        
        tileData.wateredYesterday = tileData.wateredToday;
        tileData.wateredToday = false;
        tileData.moistureState = SoilMoistureState.Dry;
        
        // 只有状态变化时才更新视觉
        if (needsVisualUpdate)
        {
            visualManager.UpdateTileVisual(...);
        }
    }
    
    wateredTodayTiles.Clear();
}
```

### 6.3 粒子效果对象池

```csharp
private Queue<GameObject> particlePool = new Queue<GameObject>();

private GameObject GetParticle(GameObject prefab)
{
    if (particlePool.Count > 0)
    {
        var particle = particlePool.Dequeue();
        particle.SetActive(true);
        return particle;
    }
    return Instantiate(prefab);
}

private void ReturnParticle(GameObject particle, float delay)
{
    StartCoroutine(ReturnParticleDelayed(particle, delay));
}

private IEnumerator ReturnParticleDelayed(GameObject particle, float delay)
{
    yield return new WaitForSeconds(delay);
    particle.SetActive(false);
    particlePool.Enqueue(particle);
}
```

---

## 7. 测试框架

### 7.1 单元测试

```csharp
// 测试文件：Assets/YYY_Tests/Editor/FarmingSystemTests.cs

[TestFixture]
public class FarmingSystemTests
{
    // P1.1 耕地唯一性
    [Test]
    public void TillSoil_SamePosition_ReturnsFalse()
    {
        // Arrange
        var manager = CreateTestFarmingManager();
        var position = new Vector3Int(0, 0, 0);
        
        // Act
        manager.TillSoil(position);
        bool result = manager.TillSoil(position);
        
        // Assert
        Assert.IsFalse(result);
    }
    
    // P2.1 种植消耗
    [Test]
    public void PlantSeed_Success_RemovesSeedFromInventory()
    {
        // Arrange
        var manager = CreateTestFarmingManager();
        var inventory = CreateTestInventory();
        inventory.AddItem(seedID, 0, 5);
        
        // Act
        manager.PlantSeed(position, seedID);
        
        // Assert
        Assert.AreEqual(4, inventory.GetItemCount(seedID));
    }
    
    // P4.2 枯萎条件
    [Test]
    public void Crop_ThreeDaysWithoutWater_BecomesWithered()
    {
        // Arrange
        var crop = CreateTestCrop();
        
        // Act
        crop.ProcessDailyGrowth(wateredYesterday: false); // Day 1
        crop.ProcessDailyGrowth(wateredYesterday: false); // Day 2
        crop.ProcessDailyGrowth(wateredYesterday: false); // Day 3
        
        // Assert
        Assert.IsTrue(crop.IsWithered());
    }
}
```

---

## 8. 文件结构

```
Assets/YYY_Scripts/Farm/
├── FarmingManager.cs          # 协调者（重写）
├── FarmTileManager.cs         # 耕地状态管理（新建）
├── CropManager.cs             # 作物生命周期（新建）
├── FarmVisualManager.cs       # 视觉更新（新建）
├── CropController.cs          # 作物控制器（重写）
├── Data/
│   ├── FarmTileData.cs        # 耕地数据（扩展）
│   ├── CropInstanceData.cs    # 作物实例数据（新建）
│   ├── LayerTilemaps.cs       # 楼层配置（新建）
│   └── SoilMoistureState.cs   # 土壤状态（保留）
└── [废弃]
    ├── CropGrowthSystem.cs    # 合并到 CropManager
    └── CropInstance.cs        # 替换为 CropInstanceData
```

