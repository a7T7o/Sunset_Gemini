# 设计文档

## 概述

本设计文档整合箱子系统前两个工作区的未完成任务，并针对新发现的问题进行综合修复。核心目标包括：

**工作区 1 未完成功能**：
1. Sprite 状态管理 - 4个 Sprite 字段的自动切换
2. 抖动和推动效果 - 受击抖动、多次跳跃推动动画
3. IResourceNode 接口实现 - 工具系统集成
4. UI 状态同步 - BoxPanelUI 与 ChestController 的 isOpen 状态
5. 批量生成工具更新 - 支持 KeyData 和 LockData
6. 初始化逻辑完善 - 支持设置 origin

**新发现问题**：
1. PolygonCollider2D 从 Sprite 的 Custom Physics Shape 自动更新
2. 放置系统在向左/向下方向时的放置失败
3. 箱子丢弃时使用 Box 预制体而非 WorldItem
4. 箱子打开功能无法触发
5. 钥匙和锁的材质等级扩展到 0-5（6种）

## 整体架构

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           ChestController                                │
│                         （箱子控制器核心）                                │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  状态管理                                                                │
│  ├── Sprite 状态（4个 Sprite 字段）                                      │
│  │   ├── spriteUnlockedClosed                                           │
│  │   ├── spriteUnlockedOpen                                             │
│  │   ├── spriteLockedClosed                                             │
│  │   └── spriteLockedOpen                                               │
│  ├── isOpen (bool)                                                      │
│  ├── isLocked (bool)                                                    │
│  ├── origin (ChestOrigin)                                               │
│  └── ownership (ChestOwnership)                                         │
│                                                                         │
│  核心方法                                                                │
│  ├── UpdateSprite()          - 更新 Sprite 显示                         │
│  ├── UpdateColliderFromSprite() - 更新碰撞体形状                        │
│  ├── SetOpen(bool)           - 设置打开状态                             │
│  ├── PlayShakeEffect()       - 播放抖动效果                             │
│  ├── TryPush()               - 推动箱子（多次跳跃）                      │
│  ├── OnHit()                 - 受击处理（集成来源归属）                  │
│  └── Initialize()            - 初始化（支持 origin）                     │
│                                                                         │
│  IResourceNode 接口                                                      │
│  ├── ResourceTag → "Chest"                                              │
│  ├── IsDepleted → false                                                 │
│  ├── CanAccept → true                                                   │
│  └── GetBounds() → SpriteRenderer.bounds                                │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
                              │
                              │ 交互
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                          BoxPanelUI                                      │
│                        （箱子 UI 面板）                                   │
├─────────────────────────────────────────────────────────────────────────┤
│  + Open(ChestController)  → 调用 chest.SetOpen(true)                    │
│  + Close()                → 调用 chest.SetOpen(false)                   │
└─────────────────────────────────────────────────────────────────────────┘
                              │
                              │ 管理
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                       GameInputManager                                   │
│                      （输入管理器）                                       │
├─────────────────────────────────────────────────────────────────────────┤
│  + HandleChestInteraction() - 检测箱子交互                               │
│  + TryOpenChest()           - 打开箱子（距离检测+导航）                  │
└─────────────────────────────────────────────────────────────────────────┘
```

## 详细设计

### 1. Sprite 状态管理（工作区 1 未完成）

#### 设计目标
箱子根据状态（上锁/未锁、打开/关闭）自动切换显示对应的 Sprite。

#### 核心方法

```csharp
/// <summary>
/// 获取当前状态对应的 Sprite
/// </summary>
private Sprite GetCurrentSprite()
{
    if (isLocked)
    {
        return _isOpen ? spriteLockedOpen : spriteLockedClosed;
    }
    else
    {
        return _isOpen ? spriteUnlockedOpen : spriteUnlockedClosed;
    }
}

/// <summary>
/// 更新 Sprite 显示
/// </summary>
public void UpdateSprite()
{
    if (_spriteRenderer == null) return;
    
    Sprite newSprite = GetCurrentSprite();
    if (newSprite != null)
    {
        _spriteRenderer.sprite = newSprite;
        UpdateColliderFromSprite(); // 同时更新碰撞体
    }
}

/// <summary>
/// 设置打开状态
/// </summary>
public void SetOpen(bool open)
{
    _isOpen = open;
    UpdateSprite();
}

/// <summary>
/// 编辑器验证
/// </summary>
private void OnValidate()
{
    if (spriteUnlockedClosed == null) Debug.LogWarning("[ChestController] spriteUnlockedClosed 为空");
    if (spriteUnlockedOpen == null) Debug.LogWarning("[ChestController] spriteUnlockedOpen 为空");
    if (spriteLockedClosed == null) Debug.LogWarning("[ChestController] spriteLockedClosed 为空");
    if (spriteLockedOpen == null) Debug.LogWarning("[ChestController] spriteLockedOpen 为空");
}
```

#### 状态映射表

| isLocked | isOpen | 显示 Sprite |
|----------|--------|-------------|
| false | false | spriteUnlockedClosed |
| false | true | spriteUnlockedOpen |
| true | false | spriteLockedClosed |
| true | true | spriteLockedOpen |

---

### 2. PolygonCollider2D 更新（新问题）

#### 设计目标
箱子的 PolygonCollider2D 从 Sprite 的 Custom Physics Shape 自动初始化和更新。

#### 核心方法

```csharp
/// <summary>
/// 从当前 Sprite 的 Custom Physics Shape 更新 PolygonCollider2D
/// 参考：TreeControllerV2.UpdateColliderFromSprite()
/// </summary>
private void UpdateColliderFromSprite()
{
    if (_polygonCollider == null || _spriteRenderer == null) return;
    
    Sprite sprite = _spriteRenderer.sprite;
    if (sprite == null) return;
    
    int shapeCount = sprite.GetPhysicsShapeCount();
    if (shapeCount == 0)
    {
        Debug.LogWarning($"[ChestController] Sprite {sprite.name} 没有 Custom Physics Shape");
        return;
    }
    
    // 清除现有路径
    _polygonCollider.pathCount = shapeCount;
    
    // 从 Sprite 获取物理形状并设置到碰撞体
    List<Vector2> path = new List<Vector2>();
    for (int i = 0; i < shapeCount; i++)
    {
        path.Clear();
        sprite.GetPhysicsShape(i, path);
        _polygonCollider.SetPath(i, path);
    }
}
```

#### 调用时机
- Awake() - 缓存 PolygonCollider2D 引用
- Start() - 初始化时调用 UpdateSprite()
- UpdateSprite() - 每次 Sprite 变化时调用

---

### 3. 抖动和推动效果（工作区 1 未完成）

#### 3.1 抖动效果

```csharp
[Header("抖动效果")]
[SerializeField] private float shakeDuration = 0.2f;
[SerializeField] private float shakeIntensity = 0.1f;

/// <summary>
/// 播放抖动效果
/// </summary>
public void PlayShakeEffect()
{
    if (_shakeCoroutine != null)
        StopCoroutine(_shakeCoroutine);
    _shakeCoroutine = StartCoroutine(ShakeCoroutine());
}

/// <summary>
/// 抖动协程
/// </summary>
private IEnumerator ShakeCoroutine()
{
    Vector3 originalPos = transform.position;
    float elapsed = 0f;
    
    while (elapsed < shakeDuration)
    {
        float x = Random.Range(-shakeIntensity, shakeIntensity);
        float y = Random.Range(-shakeIntensity, shakeIntensity);
        transform.position = originalPos + new Vector3(x, y, 0);
        
        elapsed += Time.deltaTime;
        yield return null;
    }
    
    transform.position = originalPos;
}
```

#### 3.2 推动动画（多次跳跃）+ 安全机制

```csharp
[Header("推动效果")]
[SerializeField] private LayerMask obstacleLayerMask; // 障碍物层级
private bool _isPushing = false; // 推动锁，防止协程重入
private Coroutine _pushCoroutine;

/// <summary>
/// 尝试推动箱子
/// </summary>
public void TryPush(Vector2 direction)
{
    // 🔴 安全机制 1：防止协程重入
    if (_isPushing)
    {
        if (showDebugInfo)
            Debug.Log("[ChestController] 箱子正在移动中，拒绝新的推动请求");
        return;
    }
    
    // 计算目标位置（总位移 1.0 单位）
    Vector3 targetPos = transform.position + (Vector3)direction.normalized * 1.0f;
    
    // 🔴 安全机制 2：碰撞检测 - 检测目标位置是否有障碍物
    Vector2 boxSize = _polygonCollider != null ? _polygonCollider.bounds.size : Vector2.one;
    RaycastHit2D hit = Physics2D.BoxCast(
        transform.position,
        boxSize,
        0f,
        direction,
        1.0f,
        obstacleLayerMask
    );
    
    if (hit.collider != null)
    {
        if (showDebugInfo)
            Debug.Log($"[ChestController] 目标位置被阻挡：{hit.collider.name}，只播放抖动");
        
        // 有障碍物，只播放抖动，不移动
        PlayShakeEffect();
        return;
    }
    
    // 无障碍物，执行推动动画
    if (_pushCoroutine != null)
        StopCoroutine(_pushCoroutine);
    _pushCoroutine = StartCoroutine(PushCoroutine(targetPos));
}

/// <summary>
/// 推动协程 - 3次跳跃，越跳越短越矮
/// </summary>
private IEnumerator PushCoroutine(Vector3 targetPos)
{
    _isPushing = true; // 🔴 设置推动锁
    
    Vector3 startPos = transform.position;
    
    // 3次跳跃配置
    float[] distances = { 0.5f, 0.3f, 0.2f };
    float[] heights = { 1.0f, 0.6f, 0.3f };
    float jumpDuration = 0.2f;
    
    for (int i = 0; i < 3; i++)
    {
        Vector3 jumpStart = transform.position;
        Vector3 jumpEnd = jumpStart + (targetPos - startPos).normalized * distances[i];
        float elapsed = 0f;
        
        while (elapsed < jumpDuration)
        {
            float t = elapsed / jumpDuration;
            float height = Mathf.Sin(t * Mathf.PI) * heights[i];
            
            transform.position = Vector3.Lerp(jumpStart, jumpEnd, t) + Vector3.up * height;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
    
    transform.position = targetPos;
    _isPushing = false; // 🔴 释放推动锁
}
```

#### 3.3 安全机制说明

**问题 1：协程重入导致"无限升空"**
- 现象：玩家快速连击时，箱子在半空中被再次击中，以半空中的位置作为起点再次跳跃
- 后果：箱子会越打越高，最后飞出屏幕
- 解决：使用 `_isPushing` 标志位，在推动过程中拒绝新的推动请求

**问题 2：推动穿墙**
- 现象：箱子后面有墙或其他障碍物时，推动会导致箱子卡进墙里
- 后果：物理系统疯狂抖动或物体被挤出地图
- 解决：使用 `Physics2D.BoxCast` 检测目标位置，有障碍物则只播放抖动

---

### 4. IResourceNode 接口实现（工作区 1 未完成）

#### 接口实现

```csharp
public class ChestController : MonoBehaviour, IResourceNode
{
    // IResourceNode 接口实现
    public string ResourceTag => "Chest";
    public bool IsDepleted => false; // 箱子不会耗尽
    
    public bool CanAccept(ToolHitContext ctx)
    {
        return true; // 所有工具都能触发抖动
    }
    
    public Bounds GetBounds()
    {
        if (_spriteRenderer != null)
            return _spriteRenderer.bounds;
        return new Bounds(transform.position, Vector3.one);
    }
    
    public Bounds GetColliderBounds()
    {
        if (_polygonCollider != null)
            return _polygonCollider.bounds;
        return GetBounds();
    }
    
    // 注册和注销
    private void Start()
    {
        ResourceNodeRegistry.Instance?.Register(this);
        UpdateSprite(); // 初始化显示
    }
    
    private void OnDestroy()
    {
        ResourceNodeRegistry.Instance?.Unregister(this);
    }
}
```

---

### 5. OnHit 方法重构（工作区 1 未完成）+ 野外箱子清理后路

#### 集成来源归属系统

```csharp
[Header("野外箱子清理设置")]
[SerializeField] private bool allowDestroyEmptyWorldChest = false; // 🟡 是否允许清理空的野外箱子

public void OnHit(ToolHitContext ctx)
{
    // 始终播放抖动效果
    PlayShakeEffect();

    // 非镐子工具只抖动
    if (ctx.toolType != ToolType.Pickaxe)
        return;

    // 野外上锁箱子（包括已开锁的）不能被挖取或推动
    if (!CanBeMinedOrMoved())
    {
        if (showDebugInfo)
            Debug.Log("[ChestController] 野外上锁箱子不能被挖取或移动");
        return;
    }

    // 有物品：推动
    if (!IsEmpty)
    {
        TryPush(ctx.hitDir);
        return;
    }

    // 空箱子：造成伤害
    int damage = Mathf.Max(1, Mathf.RoundToInt(ctx.baseDamage));
    currentHealth -= damage;

    if (currentHealth <= 0)
        OnDestroyed();
}

/// <summary>
/// 判断箱子是否可以被挖取或移动
/// </summary>
private bool CanBeMinedOrMoved()
{
    // 玩家制作的箱子：始终可以挖取和移动
    if (origin == ChestOrigin.PlayerCrafted)
        return true;
    
    // 野外箱子：
    // - 从未上锁：可以挖取和移动
    // - 上过锁（即使现在已开锁）：
    //   - 默认不可挖取和移动（作为一次性宝箱）
    //   - 🟡 但如果开启了 allowDestroyEmptyWorldChest 且箱子为空，则允许清理
    if (origin == ChestOrigin.WorldSpawned)
    {
        if (!hasBeenLocked)
            return true; // 从未上锁，可以挖取
        
        // 🟡 后路：允许清理空的野外箱子
        if (allowDestroyEmptyWorldChest && IsEmpty && !isLocked)
            return true;
        
        return false; // 上过锁，不可挖取（除非开启了清理开关）
    }
    
    return false;
}
```

#### 野外箱子清理逻辑说明

**设计理念**：
- 默认行为：野外上锁箱子 = 一次性宝箱，开锁后不可清理
- 后路设计：提供 `allowDestroyEmptyWorldChest` 开关，允许清理空的野外箱子

**为什么需要后路**：
- 用户的原始需求是"野外上锁箱子不能被挖取/移动"
- 但这可能导致"满地图无法销毁的空箱子"问题
- 通过开关设计，可以在测试后快速调整，无需重构逻辑

**开关行为**：
- `allowDestroyEmptyWorldChest = false`（默认）：严格遵守原始需求，野外箱子不可清理
- `allowDestroyEmptyWorldChest = true`：允许清理空的、已开锁的野外箱子

### 6. UI 状态同步（工作区 1 未完成）- 事件驱动架构

#### 设计目标
使用事件驱动架构实现 UI 与数据层的解耦，ChestController 负责状态管理并触发事件，BoxPanelUI 订阅事件并响应状态变化。

#### ChestController 事件定义

```csharp
public class ChestController : MonoBehaviour
{
    // 🟡 事件：箱子打开状态变化
    public event Action<bool> OnOpenStateChanged;
    
    /// <summary>
    /// 设置打开状态
    /// </summary>
    public void SetOpen(bool open)
    {
        if (_isOpen == open) return; // 避免重复触发
        
        _isOpen = open;
        UpdateSprite();
        
        // 🟡 触发事件，通知所有监听者
        OnOpenStateChanged?.Invoke(_isOpen);
    }
}
```

#### BoxPanelUI 事件订阅

```csharp
public class BoxPanelUI : MonoBehaviour
{
    private ChestController _currentChest;
    
    /// <summary>
    /// 打开箱子 UI
    /// </summary>
    public void Open(ChestController chest)
    {
        // 取消订阅旧箱子
        if (_currentChest != null)
            _currentChest.OnOpenStateChanged -= OnChestStateChanged;
        
        _currentChest = chest;
        
        // 订阅新箱子的状态变化
        if (_currentChest != null)
        {
            _currentChest.OnOpenStateChanged += OnChestStateChanged;
            _currentChest.SetOpen(true); // 触发打开
        }
        
        gameObject.SetActive(true);
        RefreshUI();
    }
    
    /// <summary>
    /// 关闭箱子 UI
    /// </summary>
    public void Close()
    {
        if (_currentChest != null)
        {
            _currentChest.SetOpen(false); // 触发关闭
            _currentChest.OnOpenStateChanged -= OnChestStateChanged;
        }
        
        gameObject.SetActive(false);
        _currentChest = null;
    }
    
    /// <summary>
    /// 响应箱子状态变化
    /// </summary>
    private void OnChestStateChanged(bool isOpen)
    {
        // UI 响应状态变化（如果需要）
        // 例如：更新标题文字、播放动画等
        if (showDebugInfo)
            Debug.Log($"[BoxPanelUI] 箱子状态变化：{(isOpen ? "打开" : "关闭")}");
    }
}
```

#### 事件驱动架构的优势

1. **解耦性**：UI 不直接修改数据层状态，而是通过事件监听状态变化
2. **可扩展性**：未来如果有多个 UI 需要响应箱子状态（如小地图标记、任务系统），只需订阅同一个事件
3. **一致性**：与项目中已有的 `TimeManager.OnDayChanged` 等事件系统保持架构一致
4. **可维护性**：单向数据流，状态变化路径清晰，易于调试

---

### 7. 批量生成工具更新（工作区 1 未完成）

#### 工具扩展

已在工作区 1 中完成，确认以下功能：
- ItemSOType 枚举包含 KeyData 和 LockData
- DrawKeySettings() 方法 - 钥匙材质和开锁概率配置
- DrawLockSettings() 方法 - 锁材质配置
- CreateKeyData() 和 CreateLockData() 方法
- 输出文件夹映射：Keys 和 Locks

---

### 8. 初始化逻辑完善（工作区 1 未完成）

#### Initialize 重载

```csharp
/// <summary>
/// 初始化箱子（支持设置来源）
/// </summary>
public void Initialize(StorageData data, ChestOrigin origin, ChestOwnership ownership)
{
    storageData = data;
    this.origin = origin;
    this.ownership = ownership;
    
    currentHealth = data.maxHealth;
    isLocked = (ownership == ChestOwnership.World);
    hasBeenLocked = isLocked;
    
    UpdateSprite();
}

/// <summary>
/// Awake - 缓存引用
/// </summary>
private void Awake()
{
    _spriteRenderer = GetComponent<SpriteRenderer>();
    _polygonCollider = GetComponent<PolygonCollider2D>();
}

/// <summary>
/// Start - 初始化显示
/// </summary>
private void Start()
{
    ResourceNodeRegistry.Instance?.Register(this);
    UpdateSprite();
}
```

---

### 9. 放置系统修复（新问题）+ 安全机制

#### 问题分析
- 向左/向下放置时，导航目标点计算可能导致玩家无法到达
- 放置以"到达目的地"为终止条件，而非"放置成功"
- 🔴 **致命问题**：箱子生成在玩家位置时，玩家（Dynamic Rigidbody2D）会被弹飞

#### PlacementNavigator 修改

```csharp
[Header("放置触发设置")]
[SerializeField] private float placementTriggerDistance = 1.5f;
[SerializeField] private float navigationTimeout = 10f; // 导航超时时间
[SerializeField] private float colliderEnableDelay = 0.5f; // 碰撞体启用延迟

private float _navigationStartTime;

/// <summary>
/// 计算导航目标点 - 修改为放置点中心
/// </summary>
public Vector3 CalculateNavigationTarget(Bounds playerBounds, Bounds previewBounds)
{
    // 直接返回放置点中心
    return previewBounds.center;
}

/// <summary>
/// 检查是否到达放置触发距离
/// </summary>
public bool CheckReached()
{
    if (playerTransform == null) return false;
    
    Vector3 playerCenter = GetPlayerCenter();
    float distance = Vector2.Distance(
        new Vector2(playerCenter.x, playerCenter.y),
        new Vector2(targetPosition.x, targetPosition.y)
    );
    
    return distance <= placementTriggerDistance;
}

/// <summary>
/// 🔴 安全机制：检查导航是否超时或卡死
/// </summary>
public bool IsNavigationStuck()
{
    // 检查超时
    if (Time.time - _navigationStartTime > navigationTimeout)
        return true;
    
    // 检查路径是否有效
    if (playerNavigator != null && !playerNavigator.HasValidPath())
        return true;
    
    return false;
}
```

#### 放置成功检测 + 物理安全机制

在 PlacementManagerV3 中添加：
```csharp
private void Update()
{
    // ... 现有逻辑 ...
    
    // 🔴 安全机制 1：检测导航超时或卡死
    if (isNavigating && placementNavigator.IsNavigationStuck())
    {
        Debug.LogWarning("[PlacementManager] 导航超时或卡死，取消放置");
        CancelPlacement();
        return;
    }
    
    // 检测放置成功
    if (placementSucceeded)
    {
        StopNavigation();
        placementSucceeded = false;
    }
}

/// <summary>
/// 🔴 安全机制 2：放置物体时的物理保护
/// </summary>
private void PlaceObject(GameObject placedObject)
{
    // 获取箱子的 Collider
    Collider2D chestCollider = placedObject.GetComponent<Collider2D>();
    
    if (chestCollider != null)
    {
        // 方案 A：暂时设为 Trigger，延迟后恢复
        chestCollider.isTrigger = true;
        StartCoroutine(EnableColliderAfterDelay(chestCollider, colliderEnableDelay));
        
        // 方案 B（备选）：暂时忽略与玩家的碰撞
        // Collider2D playerCollider = player.GetComponent<Collider2D>();
        // Physics2D.IgnoreCollision(chestCollider, playerCollider, true);
        // StartCoroutine(RestoreCollisionAfterDelay(chestCollider, playerCollider, colliderEnableDelay));
    }
}

/// <summary>
/// 延迟启用碰撞体
/// </summary>
private IEnumerator EnableColliderAfterDelay(Collider2D collider, float delay)
{
    yield return new WaitForSeconds(delay);
    
    if (collider != null)
    {
        // 检查玩家是否还在范围内
        Vector3 playerPos = player.transform.position;
        float distance = Vector2.Distance(playerPos, collider.transform.position);
        
        if (distance > 0.5f) // 玩家已离开
        {
            collider.isTrigger = false;
        }
        else // 玩家还在附近，继续等待
        {
            StartCoroutine(EnableColliderAfterDelay(collider, 0.1f));
        }
    }
}
```

#### 安全机制说明

**问题 1：物理弹飞**
- 现象：箱子（Static Rigidbody2D）生成在玩家（Dynamic Rigidbody2D）位置时，Unity 物理引擎会施加巨大的反向力给玩家
- 后果：玩家被弹飞到地图外或穿模
- 解决：
  - 方案 A：放置瞬间将箱子 Collider 设为 Trigger，等玩家离开后再恢复
  - 方案 B：使用 `Physics2D.IgnoreCollision` 暂时忽略玩家碰撞

**问题 2：导航死锁**
- 现象：玩家被卡住或路径无效时，导航无法完成，放置流程死锁
- 后果：玩家无法取消放置，进退不得
- 解决：添加导航超时检测和路径有效性检测，超时后自动取消放置

---

### 10. 箱子丢弃机制（新问题）

#### 当前流程（错误）
```
背包丢弃 → WorldItem 预制体 → 可拾取状态 → 拾取
```

#### 目标流程（正确）
```
背包丢弃 → Box 预制体 + 临时阴影 → 弹跳动画 → 落地成为世界物体
```

#### ChestDropHandler 修改

```csharp
/// <summary>
/// 处理箱子掉落 - 使用 Box 预制体而非 WorldItem
/// </summary>
public static IEnumerator HandleChestDropWithAnimation(
    StorageData storageData, 
    Vector3 dropPosition,
    Vector3 dropDirection)
{
    // 1. 实例化 Box 预制体
    GameObject chestObj = Object.Instantiate(
        storageData.storagePrefab, 
        dropPosition, 
        Quaternion.identity
    );
    
    // 2. 添加临时阴影
    GameObject shadowObj = CreateTemporaryShadow(chestObj, storageData);
    
    // 3. 播放弹跳动画
    yield return PlayBounceAnimation(chestObj, shadowObj, dropDirection);
    
    // 4. 动画完成，移除临时阴影
    if (shadowObj != null)
        Object.Destroy(shadowObj);
    
    // 5. 初始化 ChestController
    ChestController controller = chestObj.GetComponent<ChestController>();
    if (controller != null)
    {
        controller.Initialize(storageData, ChestOrigin.PlayerCrafted, ChestOwnership.Player);
    }
}

/// <summary>
/// 创建临时阴影
/// </summary>
private static GameObject CreateTemporaryShadow(GameObject chestObj, StorageData data)
{
    GameObject shadowObj = new GameObject("TempShadow");
    shadowObj.transform.SetParent(chestObj.transform);
    shadowObj.transform.localPosition = Vector3.zero;
    
    SpriteRenderer sr = shadowObj.AddComponent<SpriteRenderer>();
    sr.sprite = data.shadowSprite; // 假设 StorageData 有 shadowSprite 字段
    sr.sortingLayerName = "Ground";
    sr.sortingOrder = -1;
    
    return shadowObj;
}
```

#### WorldSpawnService 修改

```csharp
public void SpawnWithAnimation(ItemData itemData, Vector3 position, Vector3 direction)
{
    // 检测是否为箱子
    if (itemData is StorageData storageData)
    {
        StartCoroutine(ChestDropHandler.HandleChestDropWithAnimation(
            storageData, position, direction));
        return;
    }
    
    // 其他物品使用原有逻辑
    // ...
}
```

---

### 11. 箱子打开功能修复（新问题）+ 交互优化

#### GameInputManager 修改

```csharp
[Header("箱子 UI")]
[SerializeField] private BoxPanelUI boxPanelUI; // 🟡 优先使用 SerializeField 引用

/// <summary>
/// 处理箱子交互
/// </summary>
private void HandleChestInteraction()
{
    // 检测点击的箱子
    ChestController chest = GetChestAtMousePosition();
    if (chest == null) return;
    
    // 尝试打开箱子
    TryOpenChest(chest);
}

/// <summary>
/// 尝试打开箱子
/// </summary>
private void TryOpenChest(ChestController chest)
{
    // 检查 UI 引用（优先使用 SerializeField，动态查找作为备选）
    if (boxPanelUI == null)
    {
        boxPanelUI = FindFirstObjectByType<BoxPanelUI>();
        if (boxPanelUI == null)
        {
            Debug.LogError("[GameInputManager] 找不到 BoxPanelUI，请在 Inspector 中配置引用");
            return;
        }
    }
    
    // 🟡 优化：使用 ClosestPoint 计算距离，支持不规则形状箱子
    float distance = CalculateInteractionDistance(chest);
    
    if (distance <= interactionDistance)
    {
        // 在交互距离内，直接打开
        chest.TryOpen();
        if (chest.CanOpen)
            boxPanelUI.Open(chest);
    }
    else
    {
        // 超出距离，触发自动导航
        playerNavigator.NavigateToTarget(chest.transform.position, () =>
        {
            chest.TryOpen();
            if (chest.CanOpen)
                boxPanelUI.Open(chest);
        });
    }
}

/// <summary>
/// 🟡 优化：计算与箱子的精确交互距离
/// </summary>
private float CalculateInteractionDistance(ChestController chest)
{
    Vector3 playerPos = playerTransform.position;
    
    // 尝试使用 Collider 的 ClosestPoint 获取最近点
    Collider2D chestCollider = chest.GetComponent<Collider2D>();
    if (chestCollider != null)
    {
        Vector2 closestPoint = chestCollider.ClosestPoint(playerPos);
        return Vector2.Distance(playerPos, closestPoint);
    }
    
    // 备选：使用中心点距离
    return Vector2.Distance(playerPos, chest.transform.position);
}
```

#### 交互距离优化说明

**问题：中心点距离判定不准确**
- 现象：大型箱子（如 2x1）的中心点在物体内部，玩家贴脸时中心距离可能 > 1.5
- 后果：玩家明明贴着箱子，系统却判定"距离不够"，强制触发导航
- 解决：使用 `Collider.ClosestPoint()` 计算到碰撞体边缘的距离，而非中心点距离

**优势**：
- 支持不规则形状的箱子
- 交互判定更符合玩家直觉
- 避免"手短"问题

### 12. 钥匙和锁材质等级扩展（新问题）

#### 当前设计（不完整）
- KeyLockData.material 使用 ChestMaterial 枚举（Wood/Iron/Special）
- 只支持 3 种材质

#### 目标设计（完整）
- 支持 0-5 共 6 种材质等级
- 与游戏中的材质系统保持一致

#### MaterialTier 静态类

```csharp
/// <summary>
/// 材质等级常量
/// </summary>
public static class MaterialTier
{
    public const int Wood = 0;
    public const int Stone = 1;
    public const int Iron = 2;
    public const int Copper = 3;
    public const int Steel = 4;
    public const int Gold = 5;
    
    public static readonly string[] Names = new string[]
    {
        "木质", "石质", "铁质", "铜质", "钢质", "金质"
    };
    
    public static string GetName(int tier)
    {
        if (tier < 0 || tier >= Names.Length)
            return "未知";
        return Names[tier];
    }
}
```

#### KeyLockData 修改

```csharp
[Header("=== 钥匙/锁专属属性 ===")]
[Tooltip("类型：钥匙或锁")]
public KeyLockType keyLockType = KeyLockType.Key;

[Tooltip("材质等级（0=木质, 1=石质, 2=铁质, 3=铜质, 4=钢质, 5=金质）")]
[Range(0, 5)]
public int materialTier = 0;

[Header("=== 开锁概率（仅钥匙有效）===")]
[Tooltip("开锁成功概率（0-1）")]
[Range(0f, 1f)]
public float unlockChance = 0.1f;

/// <summary>
/// 获取材质名称
/// </summary>
public string GetMaterialName()
{
    return MaterialTier.GetName(materialTier);
}
```

#### StorageData 修改

```csharp
[Header("=== 箱子专属属性 ===")]
[Tooltip("材质等级（0=木质, 1=石质, 2=铁质, 3=铜质, 4=钢质, 5=金质）")]
[Range(0, 5)]
public int materialTier = 0;

[Tooltip("箱子最大血量")]
[Range(1, 20)]
public int maxHealth = 2;

[Tooltip("被开锁概率（0-1）")]
[Range(0f, 1f)]
public float baseUnlockChance = 0.5f;

/// <summary>
/// 获取材质名称
/// </summary>
public string GetMaterialName()
{
    return MaterialTier.GetName(materialTier);
}
```

#### ChestController 修改

```csharp
/// <summary>
/// 玩家尝试上锁（使用 int 材质等级）
/// </summary>
public LockResult TryLockByPlayer(int lockMaterialTier)
{
    // 检查材质匹配
    if (lockMaterialTier != storageData.materialTier)
        return LockResult.MaterialMismatch;
    
    // ... 其他逻辑 ...
}

/// <summary>
/// 使用钥匙尝试开锁（使用 int 材质等级）
/// </summary>
public UnlockResult TryUnlockWithKey(KeyLockData keyData)
{
    // 计算开锁概率（不再检查材质匹配）
    float totalChance = keyData.unlockChance + storageData.baseUnlockChance;
    bool success = Random.value <= totalChance;
    
    // ... 其他逻辑 ...
}
```

---

## 数据模型

### 箱子状态矩阵

```
┌─────────────┬─────────────┬─────────────┬─────────────────────────────────┐
│   origin    │  isLocked   │ hasBeenLocked│         交互规则                │
├─────────────┼─────────────┼─────────────┼─────────────────────────────────┤
│ PlayerCrafted│   false    │    false    │ 可上锁、可挖取、可移动           │
│ PlayerCrafted│   true     │    true     │ 不能再上锁、可挖取、可移动       │
│ WorldSpawned │   false    │    false    │ 可上锁(变Player)、可挖取、可移动 │
│ WorldSpawned │   true     │    true     │ 需钥匙开锁、不能挖取、不能移动   │
│ WorldSpawned │   false    │    true     │ 已开锁、不能挖取、不能移动       │
└─────────────┴─────────────┴─────────────┴─────────────────────────────────┘
```

### 材质等级配置

| 等级 | 名称 | 钥匙开锁概率 | 说明 |
|------|------|-------------|------|
| 0 | 木质 | 0.10 | Wood |
| 1 | 石质 | 0.15 | Stone |
| 2 | 铁质 | 0.20 | Iron |
| 3 | 铜质 | 0.25 | Copper |
| 4 | 钢质 | 0.30 | Steel |
| 5 | 金质 | 0.40 | Gold |

### 箱子被开锁概率

| 箱子材质 | 被开锁概率 |
|---------|-----------|
| 木箱 (0) | 0.60 |
| 石箱 (1) | 0.55 |
| 铁箱 (2) | 0.50 |
| 铜箱 (3) | 0.45 |
| 钢箱 (4) | 0.40 |
| 金箱 (5) | 0.35 |

**最终开锁概率 = 钥匙概率 + 箱子概率**

---

## 正确性属性

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: 状态到 Sprite 的映射一致性
*对于任意* 箱子状态组合（isLocked, isOpen），GetCurrentSprite() 应返回对应的 Sprite
**Validates: Requirements 1.1, 1.2, 1.3, 1.4**

### Property 2: 碰撞体与 Sprite 同步
*对于任意* 箱子状态变化，PolygonCollider2D 的形状应该与当前 Sprite 的 Custom Physics Shape 一致
**Validates: Requirements 2.1, 2.2**

### Property 3: 工具类型决定伤害
*对于任意* 工具类型和可挖取的空箱子，只有镐子能造成伤害
**Validates: Requirements 5.5**

### Property 4: 放置方向无关性
*对于任意* 放置方向（上/下/左/右），放置成功率应该相同
**Validates: Requirements 9.1, 9.2**

### Property 5: 放置成功终止
*对于任意* 放置操作，系统应该以放置成功为终止条件，而非以导航完成为终止条件
**Validates: Requirements 9.3**

### Property 6: 箱子丢弃不可拾取
*对于任意* 从背包丢弃的箱子，在落地前不应该变为可拾取状态
**Validates: Requirements 10.4**

### Property 7: 箱子打开可触发
*对于任意* 在交互距离内的箱子，右键点击应该能够打开箱子 UI
**Validates: Requirements 11.1, 11.2**

### Property 8: 材质等级范围
*对于任意* KeyLockData 或 StorageData，materialTier 应该在 0-5 范围内
**Validates: Requirements 12.1, 12.2, 12.3, 12.7**

---

## 错误处理

### 1. Sprite 为空
- 场景：任意 Sprite 字段为空
- 处理：OnValidate 中输出警告，运行时使用当前 Sprite 不变

### 2. Sprite 无 Custom Physics Shape
- 场景：Sprite 没有设置 Custom Physics Shape
- 处理：保持原有碰撞体不变，输出警告日志

### 3. 放置导航超时
- 场景：玩家无法到达放置点
- 处理：超时后取消放置，恢复到预览状态

### 4. BoxPanelUI 引用为空
- 场景：GameInputManager 无法找到 BoxPanelUI
- 处理：通过 FindFirstObjectByType 动态获取，失败则输出错误日志

### 5. 材质等级不兼容
- 场景：锁的材质等级与箱子不兼容
- 处理：显示提示信息"锁与箱子材质不匹配"，操作失败

### 6. KeyData 为空
- 场景：TryUnlockWithKey 传入 null
- 处理：返回失败结果

---

## 测试策略

### 单元测试

1. **Sprite 状态映射测试**
   - 验证 GetCurrentSprite() 在不同状态下返回正确的 Sprite
   - 测试所有 4 种状态组合

2. **碰撞体更新测试**
   - 验证 UpdateColliderFromSprite() 正确读取 Sprite Physics Shape
   - 测试 Sprite 无 Custom Physics Shape 的错误处理

3. **材质等级范围测试**
   - 验证 materialTier 在 0-5 范围内
   - 测试 GetMaterialName() 返回正确的名称

### 集成测试

1. **箱子状态变化流程**
   - 箱子打开 → Sprite 更新 → 碰撞体更新
   - 箱子上锁 → Sprite 更新 → 碰撞体更新

2. **放置系统流程**
   - 各方向放置 → 导航到放置点 → 放置成功
   - 测试向左、向右、向上、向下四个方向

3. **箱子丢弃流程**
   - 背包丢弃 → Box 预制体实例化 → 弹跳动画 → 落地成为世界物体

4. **箱子交互流程**
   - 右键点击 → 距离检测 → 导航（如需要）→ UI 打开

### 属性测试

使用 NUnit + 随机数据生成器：
- 测试放置方向无关性（100 次迭代）
- 测试材质等级范围（100 次迭代）
- 测试工具类型决定伤害（100 次迭代）
- 每个属性测试运行 100 次迭代

---

## 相关文件

| 文件 | 说明 |
|------|------|
| `Assets/YYY_Scripts/World/Placeable/ChestController.cs` | 箱子控制器（核心） |
| `Assets/YYY_Scripts/World/Placeable/ChestDropHandler.cs` | 箱子掉落处理器 |
| `Assets/YYY_Scripts/Data/Items/KeyLockData.cs` | 钥匙/锁数据类型 |
| `Assets/YYY_Scripts/Data/Items/StorageData.cs` | 箱子数据 SO |
| `Assets/YYY_Scripts/UI/Box/BoxPanelUI.cs` | 箱子 UI 面板 |
| `Assets/YYY_Scripts/Controller/Input/GameInputManager.cs` | 输入管理器 |
| `Assets/YYY_Scripts/Service/Placement/PlacementNavigator.cs` | 放置导航组件 |
| `Assets/YYY_Scripts/Service/World/WorldSpawnService.cs` | 世界物品生成服务 |
| `Assets/Editor/Tool_BatchItemSOGenerator.cs` | 批量生成工具 |
