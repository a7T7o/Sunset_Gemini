using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FarmGame.Data;
using FarmGame.Combat;
using FarmGame.UI;

namespace FarmGame.World
{
    /// <summary>
    /// 箱子控制器 - 管理箱子世界物体的所有交互逻辑
    /// 包括：受击、推动、上锁、解锁、打开、Sprite状态管理
    /// 实现 IResourceNode 接口以与工具攻击系统集成
    /// 实现 IInteractable 接口以支持统一的交互系统
    /// </summary>
    public class ChestController : MonoBehaviour, IResourceNode, IInteractable
    {
        #region 序列化字段

        [Header("=== 数据引用 ===")]
        [Tooltip("关联的 StorageData")]
        [SerializeField] private StorageData storageData;

        [Header("=== Sprite 配置 ===")]
        [Tooltip("未锁关闭状态的 Sprite")]
        [SerializeField] private Sprite spriteUnlockedClosed;

        [Tooltip("未锁打开状态的 Sprite")]
        [SerializeField] private Sprite spriteUnlockedOpen;

        [Tooltip("上锁关闭状态的 Sprite")]
        [SerializeField] private Sprite spriteLockedClosed;

        [Tooltip("上锁打开状态的 Sprite")]
        [SerializeField] private Sprite spriteLockedOpen;

        [Header("=== 来源与归属 ===")]
        [Tooltip("箱子来源（玩家制作/野外生成）")]
        [SerializeField] private ChestOrigin origin = ChestOrigin.PlayerCrafted;

        [Tooltip("是否曾经被上过锁（上过锁的箱子不能再次上锁）")]
        [SerializeField] private bool hasBeenLocked = false;

        [Header("=== 运行时状态 ===")]
        [SerializeField] private int currentHealth;
        [SerializeField] private ChestOwnership ownership = ChestOwnership.Player;
        [SerializeField] private bool isLocked = false;

        [Header("=== 推动设置 ===")]
        [Tooltip("推动距离（单位）")]
        [SerializeField] private float pushDistance = 1f;

        [Tooltip("推动动画总时长")]
        [SerializeField] private float pushDuration = 0.5f;

        [Tooltip("推动跳跃高度")]
        [SerializeField] private float pushJumpHeight = 0.3f;

        [Tooltip("碰撞检测半径")]
        [SerializeField] private float collisionCheckRadius = 0.4f;

        [Header("=== 抖动效果 ===")]
        [Tooltip("抖动幅度")]
        [SerializeField] private float shakeIntensity = 0.05f;

        [Tooltip("抖动持续时间")]
        [SerializeField] private float shakeDuration = 0.15f;

        [Header("=== 调试 ===")]
        [SerializeField] private bool showDebugInfo = false;

        #endregion

        #region 私有字段

        private ChestInventory _inventory;
        private bool _isPushing = false;
        private bool _isShaking = false;
        private bool _isOpen = false;
        private Collider2D _collider;
        private PolygonCollider2D _polyCollider;
        private SpriteRenderer _spriteRenderer;
        private Vector3 _originalPosition;
        
        // 🔥 修正 Ⅲ：底部对齐锚点
        private Vector3 _anchorWorldPos;
        private bool _anchorInitialized = false;
        
        // 🔥 缓存引用（性能优化）
        private PackagePanelTabsUI _cachedPackagePanel;
        private Canvas _cachedCanvas;

        #endregion

        #region 属性

        public StorageData StorageData => storageData;
        public int CurrentHealth => currentHealth;
        public ChestOwnership Ownership => ownership;
        public bool IsLocked => isLocked;
        
        /// <summary>
        /// 箱子库存（新接口，推荐使用）
        /// </summary>
        public ChestInventory Inventory => _inventory;
        
        /// <summary>
        /// 兼容旧接口：获取所有内容物
        /// </summary>
        public ItemStack[] Contents => _inventory?.GetAllSlots() ?? System.Array.Empty<ItemStack>();
        
        public bool IsPushing => _isPushing;
        public bool IsOpen => _isOpen;
        public ChestOrigin Origin => origin;
        public bool HasBeenLocked => hasBeenLocked;

        /// <summary>
        /// 是否为空（委托给 ChestInventory）
        /// </summary>
        public bool IsEmpty => _inventory == null || _inventory.IsEmpty;

        #endregion

        #region IResourceNode 接口实现

        public string ResourceTag => "Chest";
        public bool IsDepleted => false;

        public bool CanAccept(ToolHitContext ctx) => true;

        public void OnHit(ToolHitContext ctx)
        {
            // 始终播放抖动效果
            PlayShakeEffect();

            // 非镐子工具只抖动
            if (ctx.toolType != ToolType.Pickaxe)
            {
                if (showDebugInfo)
                    Debug.Log($"[ChestController] 非镐子工具击中，只抖动: {ctx.toolType}");
                return;
            }

            // 检查是否可以被挖取或移动
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

            if (showDebugInfo)
                Debug.Log($"[ChestController] 受到伤害: {damage}, 剩余血量: {currentHealth}");

            if (currentHealth <= 0)
                OnDestroyed();
        }

        public Bounds GetBounds()
        {
            if (_spriteRenderer != null && _spriteRenderer.sprite != null)
                return _spriteRenderer.bounds;
            return new Bounds(transform.position, Vector3.one);
        }

        public Bounds GetColliderBounds()
        {
            if (_collider != null && _collider.enabled)
                return _collider.bounds;
            return GetBounds();
        }

        public Vector3 GetPosition() => transform.position;

        #endregion


        #region Unity 生命周期

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            _polyCollider = GetComponent<PolygonCollider2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            
            // 🔥 修正 Ⅲ：初始化底部对齐锚点
            if (!_anchorInitialized && _spriteRenderer != null)
            {
                _anchorWorldPos = GetCurrentBottomCenterWorld();
                _anchorInitialized = true;
            }
        }

        private void Start()
        {
            Initialize();

            if (ResourceNodeRegistry.Instance != null)
            {
                ResourceNodeRegistry.Instance.Register(this, gameObject.GetInstanceID());
                if (showDebugInfo)
                    Debug.Log($"[ChestController] 已注册到 ResourceNodeRegistry: {gameObject.name}");
            }
            
            // 🔥 关键修复：箱子放置后通知 NavGrid 刷新
            // 延迟一帧确保碰撞体已完全初始化
            StartCoroutine(RequestNavGridRefreshDelayed());
        }
        
        /// <summary>
        /// 延迟请求 NavGrid 刷新（确保碰撞体已初始化）
        /// </summary>
        private IEnumerator RequestNavGridRefreshDelayed()
        {
            yield return null; // 等待一帧
            RequestNavGridRefresh();
        }
        
        /// <summary>
        /// 请求 NavGrid 刷新（供外部调用）
        /// </summary>
        public void RequestNavGridRefresh()
        {
            NavGrid2D.OnRequestGridRefresh?.Invoke();
            if (showDebugInfo)
                Debug.Log($"[ChestController] 已请求 NavGrid 刷新");
        }

        private void OnDestroy()
        {
            if (ResourceNodeRegistry.Instance != null)
                ResourceNodeRegistry.Instance.Unregister(gameObject.GetInstanceID());
        }

        #endregion

        #region 初始化

        public void Initialize()
        {
            if (storageData != null)
            {
                currentHealth = storageData.maxHealth;
                isLocked = storageData.defaultLocked;

                // 🔥 使用 ChestInventory 替代 List<ItemStack>
                _inventory = new ChestInventory(storageData.storageCapacity);

                // 🔥 C4：添加调试日志验证每个箱子有独立的 ChestInventory 实例
                if (showDebugInfo)
                    Debug.Log($"[ChestController] 初始化: {storageData.itemName}, 血量={currentHealth}, 容量={storageData.storageCapacity}, instanceId={GetInstanceID()}, inventoryHash={_inventory.GetHashCode()}");
            }

            // 🔥 修正 Ⅳ：初始化时完整执行 Sprite → Collider → NavGrid 链路
            UpdateSprite();
            UpdateColliderShape();
            // NavGrid 刷新由 Start 中的延迟调用处理
        }

        public void Initialize(StorageData data, ChestOwnership initialOwnership = ChestOwnership.Player)
        {
            storageData = data;
            ownership = initialOwnership;
            Initialize();
        }

        /// <summary>
        /// 初始化箱子（支持设置来源）
        /// </summary>
        public void Initialize(StorageData data, ChestOrigin chestOrigin, ChestOwnership initialOwnership = ChestOwnership.Player, bool initialLocked = false)
        {
            storageData = data;
            origin = chestOrigin;
            ownership = initialOwnership;
            isLocked = initialLocked;
            hasBeenLocked = initialLocked;
            Initialize();
        }
        
        /// <summary>
        /// 设置物品数据库引用（供 BoxPanelUI 调用）
        /// </summary>
        public void SetDatabase(ItemDatabase database)
        {
            _inventory?.SetDatabase(database);
        }

        #endregion

        #region Sprite 管理

        public void SetOpen(bool open)
        {
            _isOpen = open;
            UpdateSpriteForState();
            
            // 🔥 修正 Ⅳ：状态切换后更新 Collider 和 NavGrid
            UpdateColliderShape();
            RequestNavGridRefresh();
            
            if (showDebugInfo)
                Debug.Log($"[ChestController] 设置打开状态: {open}, ownership={ownership}, isLocked={isLocked}");
        }

        /// <summary>
        /// 🔥 修正 Ⅲ：根据 ownership、isLocked、_isOpen 决定 Sprite，并保持底部对齐
        /// 玩家箱子：开→上锁打开，关→上锁关闭
        /// 野外箱子（已解锁）：常驻上锁打开
        /// </summary>
        public void UpdateSpriteForState()
        {
            if (_spriteRenderer == null) return;
            
            Sprite targetSprite = null;
            
            // 野外箱子且曾经被解锁过：常驻"上锁打开"样式
            if (origin == ChestOrigin.WorldSpawned && hasBeenLocked && !isLocked)
            {
                targetSprite = spriteLockedOpen;
            }
            // 玩家箱子或普通逻辑
            else
            {
                targetSprite = GetCurrentSprite();
            }
            
            if (targetSprite != null)
            {
                // 🔥 修正：先更新 Sprite，再执行底部对齐
                _spriteRenderer.sprite = targetSprite;
                AlignSpriteBottom();
            }
        }

        public void UpdateSprite()
        {
            UpdateSpriteForState();
        }

        public Sprite GetCurrentSprite()
        {
            if (isLocked)
                return _isOpen ? spriteLockedOpen : spriteLockedClosed;
            else
                return _isOpen ? spriteUnlockedOpen : spriteUnlockedClosed;
        }
        
        #region 底部对齐（修正 Ⅲ）
        
        /// <summary>
        /// 获取当前 Sprite 底部中心的世界坐标
        /// </summary>
        private Vector3 GetCurrentBottomCenterWorld()
        {
            if (_spriteRenderer == null || _spriteRenderer.sprite == null)
                return transform.position;
            
            var bounds = _spriteRenderer.bounds; // 世界空间 bounds
            return new Vector3(bounds.center.x, bounds.min.y, transform.position.z);
        }
        
        /// <summary>
        /// 底部对齐 - 与 TreeControllerV2 保持一致
        /// 修改子物体的 localPosition.y，使 Sprite 底部对齐到父物体位置
        /// </summary>
        private void AlignSpriteBottom()
        {
            if (_spriteRenderer == null || _spriteRenderer.sprite == null) return;
            
            // 使用与 TreeControllerV2 完全一致的逻辑
            Bounds spriteBounds = _spriteRenderer.sprite.bounds;
            float spriteBottomOffset = spriteBounds.min.y;
            
            Vector3 localPos = transform.localPosition;
            localPos.y = -spriteBottomOffset;
            transform.localPosition = localPos;
            
            // 更新锚点（用于后续 Sprite 切换时的相对对齐）
            _anchorWorldPos = GetCurrentBottomCenterWorld();
            _anchorInitialized = true;
            
            if (showDebugInfo)
                Debug.Log($"[ChestController] AlignSpriteBottom: spriteBottomOffset={spriteBottomOffset}, localPos.y={localPos.y}");
        }
        
        /// <summary>
        /// 应用 Sprite 并保持底部对齐（旧方法，保留兼容）
        /// </summary>
        [System.Obsolete("使用 AlignSpriteBottom() 替代")]
        private void ApplySpriteWithBottomAlign(Sprite newSprite)
        {
            if (_spriteRenderer == null || newSprite == null) return;
            
            // 应用新 Sprite
            _spriteRenderer.sprite = newSprite;
            
            // 使用统一的底部对齐方法
            AlignSpriteBottom();
        }
        
        #endregion
        
        #region Collider 更新（修正 Ⅳ）
        
        /// <summary>
        /// 更新 PolygonCollider2D 形状以匹配当前 Sprite
        /// </summary>
        private void UpdateColliderShape()
        {
            if (_polyCollider == null || _spriteRenderer == null || _spriteRenderer.sprite == null) 
                return;
            
            var sprite = _spriteRenderer.sprite;
            int shapeCount = sprite.GetPhysicsShapeCount();
            
            if (shapeCount == 0)
            {
                _polyCollider.pathCount = 0;
                return;
            }
            
            _polyCollider.pathCount = shapeCount;
            
            var physicsShape = new System.Collections.Generic.List<Vector2>();
            for (int i = 0; i < shapeCount; i++)
            {
                physicsShape.Clear();
                sprite.GetPhysicsShape(i, physicsShape);
                _polyCollider.SetPath(i, physicsShape);
            }
            
            // 🔥 确保物理系统同步
            Physics2D.SyncTransforms();
            
            if (showDebugInfo)
                Debug.Log($"[ChestController] UpdateColliderShape: shapeCount={shapeCount}");
        }
        
        #endregion

        #endregion

        #region 抖动效果

        public void PlayShakeEffect()
        {
            if (_isShaking) return;
            StartCoroutine(ShakeCoroutine());
        }

        private IEnumerator ShakeCoroutine()
        {
            _isShaking = true;
            _originalPosition = transform.position;
            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / shakeDuration;
                float currentIntensity = shakeIntensity * (1f - t);
                float offsetX = Random.Range(-currentIntensity, currentIntensity);
                float offsetY = Random.Range(-currentIntensity, currentIntensity);
                transform.position = _originalPosition + new Vector3(offsetX, offsetY, 0f);
                yield return null;
            }

            transform.position = _originalPosition;
            _isShaking = false;
        }

        #endregion

        #region 受击处理（旧接口）

        /// <summary>
        /// 尝试推动箱子
        /// </summary>
        private void TryPush(Vector2 direction)
        {
            if (_isPushing) return;
            
            // 标准化方向
            Vector2 pushDir = direction.normalized;
            if (pushDir.sqrMagnitude < 0.01f) return;
            
            // 计算目标位置
            Vector3 targetPos = transform.position + (Vector3)(pushDir * pushDistance);
            
            // 碰撞检测
            var hits = Physics2D.OverlapCircleAll(targetPos, collisionCheckRadius);
            foreach (var hit in hits)
            {
                if (hit.gameObject != gameObject && !hit.isTrigger)
                {
                    if (showDebugInfo)
                        Debug.Log($"[ChestController] 推动被阻挡: {hit.gameObject.name}");
                    return;
                }
            }
            
            StartCoroutine(PushCoroutine(targetPos));
        }
        
        private IEnumerator PushCoroutine(Vector3 targetPos)
        {
            _isPushing = true;
            Vector3 startPos = transform.position;
            float elapsed = 0f;
            
            while (elapsed < pushDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / pushDuration;
                
                // 水平移动
                Vector3 pos = Vector3.Lerp(startPos, targetPos, t);
                
                // 添加跳跃效果
                float jumpT = Mathf.Sin(t * Mathf.PI);
                pos.y += jumpT * pushJumpHeight;
                
                transform.position = pos;
                yield return null;
            }
            
            transform.position = targetPos;
            _isPushing = false;
            
            // 🔥 关键修复：推动完成后刷新 NavGrid
            RequestNavGridRefresh();
        }
        
        #endregion
        
        #region 锁定系统
        
        /// <summary>
        /// 判断箱子是否可以被挖取或移动
        /// 野外上锁箱子（无论是否已开锁）不能被挖取或移动
        /// </summary>
        public bool CanBeMinedOrMoved()
        {
            // 野外上锁箱子（hasBeenLocked=true）不能被挖取或移动
            if (origin == ChestOrigin.WorldSpawned && hasBeenLocked)
                return false;
            return true;
        }

        /// <summary>
        /// 玩家尝试上锁（消耗锁道具）
        /// </summary>
        public LockResult TryLockByPlayer()
        {
            // 已经上过锁的箱子不能再次上锁
            if (hasBeenLocked)
            {
                if (showDebugInfo)
                    Debug.Log("[ChestController] 箱子已上过锁，不能再次上锁");
                return LockResult.AlreadyLocked;
            }
            
            // 野外上锁箱子不能被玩家上锁
            if (origin == ChestOrigin.WorldSpawned && isLocked)
            {
                if (showDebugInfo)
                    Debug.Log("[ChestController] 野外上锁箱子不能被玩家上锁");
                return LockResult.AlreadyLocked;
            }
            
            isLocked = true;
            hasBeenLocked = true;
            
            // 野外未锁箱子上锁后变为玩家归属
            if (origin == ChestOrigin.WorldSpawned)
                ownership = ChestOwnership.Player;
            
            UpdateSprite();
            
            if (showDebugInfo)
                Debug.Log($"[ChestController] 玩家上锁成功");
            
            return LockResult.Success;
        }

        /// <summary>
        /// 使用钥匙尝试开锁（概率系统）
        /// </summary>
        /// <param name="keyData">钥匙数据</param>
        /// <returns>开锁结果</returns>
        public UnlockResult TryUnlockWithKey(KeyLockData keyData)
        {
            if (keyData == null)
            {
                if (showDebugInfo)
                    Debug.Log("[ChestController] 钥匙数据为空");
                return UnlockResult.MaterialMismatch;
            }

            // 确保是钥匙而不是锁
            if (keyData.keyLockType != KeyLockType.Key)
            {
                if (showDebugInfo)
                    Debug.Log("[ChestController] 不是钥匙类型");
                return UnlockResult.MaterialMismatch;
            }

            if (!isLocked)
                return UnlockResult.NotLocked;
            
            // 玩家自己的箱子不需要钥匙
            if (ownership == ChestOwnership.Player)
                return UnlockResult.AlreadyOwned;
            
            // 计算开锁概率
            float chestChance = storageData != null ? storageData.baseUnlockChance : 0.5f;
            float totalChance = keyData.unlockChance + chestChance;
            bool success = Random.value <= totalChance;
            
            if (showDebugInfo)
                Debug.Log($"[ChestController] 开锁尝试: 钥匙概率={keyData.unlockChance}, 箱子概率={chestChance}, 总概率={totalChance}, 结果={success}");
            
            if (success)
            {
                isLocked = false;
                // 野外箱子开锁后保持 World 归属，不能被挖取
                UpdateSprite();
                return UnlockResult.Success;
            }
            
            // 失败时钥匙会被消耗（由调用方处理）
            return UnlockResult.MaterialMismatch; // 复用枚举表示失败
        }

        /// <summary>
        /// 尝试上锁（旧接口，保留兼容）
        /// </summary>
        public LockResult TryLock(ChestMaterial lockMaterial)
        {
            if (hasBeenLocked) return LockResult.AlreadyLocked;
            if (isLocked) return LockResult.AlreadyLocked;
            
            if (storageData != null && storageData.chestMaterial != lockMaterial)
                return LockResult.MaterialMismatch;
            
            isLocked = true;
            hasBeenLocked = true;
            ownership = ChestOwnership.Locked;
            UpdateSprite();
            
            if (showDebugInfo)
                Debug.Log($"[ChestController] 上锁成功");
            
            return LockResult.Success;
        }
        
        /// <summary>
        /// 尝试解锁（旧接口，保留兼容）
        /// </summary>
        public UnlockResult TryUnlock(ChestMaterial keyMaterial)
        {
            if (!isLocked) return UnlockResult.NotLocked;
            if (ownership == ChestOwnership.Player) return UnlockResult.AlreadyOwned;
            
            if (storageData != null && storageData.chestMaterial != keyMaterial)
                return UnlockResult.MaterialMismatch;
            
            isLocked = false;
            // 野外箱子开锁后保持 World 归属
            if (origin != ChestOrigin.WorldSpawned)
                ownership = ChestOwnership.Player;
            UpdateSprite();
            
            if (showDebugInfo)
                Debug.Log($"[ChestController] 解锁成功");
            
            return UnlockResult.Success;
        }
        
        /// <summary>
        /// 尝试打开箱子
        /// 🔥 修正：玩家自己的箱子即使上锁也可以直接打开，不需要钥匙
        /// </summary>
        public OpenResult TryOpen()
        {
            // 🔥 玩家自己的箱子：即使上锁也可以直接打开
            if (ownership == ChestOwnership.Player)
            {
                SetOpen(true);
                return OpenResult.Success;
            }
            
            // 非玩家箱子：检查锁定状态
            if (isLocked) return OpenResult.Locked;
            
            SetOpen(true);
            return OpenResult.Success;
        }
        
        /// <summary>
        /// 设置指定槽位的物品（委托给 ChestInventory）
        /// </summary>
        public void SetSlot(int index, ItemStack stack)
        {
            _inventory?.SetSlot(index, stack);
        }
        
        /// <summary>
        /// 获取指定槽位的物品（委托给 ChestInventory）
        /// </summary>
        public ItemStack GetSlot(int index)
        {
            return _inventory?.GetSlot(index) ?? ItemStack.Empty;
        }
        
        #endregion
        
        #region IInteractable 接口实现

        /// <summary>
        /// 交互优先级（箱子为 50）
        /// </summary>
        public int InteractionPriority => 50;

        /// <summary>
        /// 交互距离
        /// </summary>
        public float InteractionDistance => 1.5f;

        /// <summary>
        /// 是否可以交互
        /// </summary>
        public bool CanInteract(InteractionContext context)
        {
            // 箱子始终可以交互（即使上锁也可以尝试开锁）
            return true;
        }

        /// <summary>
        /// 执行交互 - 核心逻辑从 GameInputManager 移到这里
        /// 🔥 修正：玩家箱子交互时不消耗钥匙
        /// </summary>
        public void OnInteract(InteractionContext context)
        {
            if (context == null) return;

            // 🔥 玩家自己的箱子：直接打开，不处理锁/钥匙逻辑
            if (ownership == ChestOwnership.Player)
            {
                OpenBoxUI();
                return;
            }

            // 检查手持物品类型（仅对非玩家箱子生效）
            if (context.Inventory != null && context.Database != null && context.HeldItemId >= 0)
            {
                var itemData = context.Database.GetItemByID(context.HeldItemId);

                // 检查是否为锁或钥匙
                if (itemData is KeyLockData keyLockData)
                {
                    if (keyLockData.keyLockType == KeyLockType.Lock)
                    {
                        // 尝试上锁
                        var result = TryLock(keyLockData.material);
                        switch (result)
                        {
                            case LockResult.Success:
                                // 消耗锁
                                context.Inventory.RemoveFromSlot(context.HeldSlotIndex, 1);
                                if (showDebugInfo)
                                    Debug.Log($"[ChestController] 上锁成功");
                                return;
                            case LockResult.AlreadyLocked:
                                if (showDebugInfo)
                                    Debug.Log($"[ChestController] 箱子已上锁");
                                // TODO: 显示UI提示
                                return;
                            case LockResult.MaterialMismatch:
                                if (showDebugInfo)
                                    Debug.Log($"[ChestController] 锁与箱子材质不匹配");
                                // TODO: 显示UI提示
                                return;
                        }
                    }
                    else if (keyLockData.keyLockType == KeyLockType.Key)
                    {
                        // 尝试解锁（仅对野外上锁箱子生效）
                        if (isLocked)
                        {
                            var result = TryUnlock(keyLockData.material);
                            switch (result)
                            {
                                case UnlockResult.Success:
                                    // 消耗钥匙
                                    context.Inventory.RemoveFromSlot(context.HeldSlotIndex, 1);
                                    if (showDebugInfo)
                                        Debug.Log($"[ChestController] 解锁成功");
                                    // 解锁后打开箱子
                                    break;
                                case UnlockResult.NotLocked:
                                    if (showDebugInfo)
                                        Debug.Log($"[ChestController] 箱子未上锁");
                                    // 直接打开
                                    break;
                                case UnlockResult.AlreadyOwned:
                                    if (showDebugInfo)
                                        Debug.Log($"[ChestController] 箱子已是玩家所有");
                                    // 直接打开
                                    break;
                                case UnlockResult.MaterialMismatch:
                                    if (showDebugInfo)
                                        Debug.Log($"[ChestController] 钥匙与箱子材质不匹配");
                                    // TODO: 显示UI提示
                                    return;
                            }
                        }
                    }
                }
            }

            // 打开箱子UI - 实例化对应的 UI Prefab
            OpenBoxUI();
        }

        /// <summary>
        /// 打开箱子 UI
        /// 🔥 修正：通过 PackagePanelTabsUI 在 PackagePanel 内部实例化 UI
        /// 🔥 优化：使用缓存引用，避免每次 Find
        /// </summary>
        private void OpenBoxUI()
        {
            // 检查是否已有打开的 BoxPanelUI
            if (BoxPanelUI.ActiveInstance != null && BoxPanelUI.ActiveInstance.IsOpen)
            {
                // 如果是同一个箱子，不重复打开
                if (BoxPanelUI.ActiveInstance.CurrentChest == this)
                {
                    if (showDebugInfo)
                        Debug.Log("[ChestController] 箱子 UI 已打开");
                    return;
                }
                // 关闭之前的 UI
                BoxPanelUI.ActiveInstance.Close();
            }

            // 检查 StorageData 是否配置了 UI Prefab
            if (storageData == null || storageData.boxUiPrefab == null)
            {
                Debug.LogError($"[ChestController] 箱子 {gameObject.name} 缺少 boxUiPrefab 配置！");
                return;
            }

            // 🔥 使用缓存引用（PackagePanelTabsUI 没有单例）
            if (_cachedPackagePanel == null)
                _cachedPackagePanel = FindFirstObjectByType<PackagePanelTabsUI>(FindObjectsInactive.Include);
            var packageTabs = _cachedPackagePanel;
            
            if (packageTabs != null)
            {
                var boxPanelUI = packageTabs.OpenBoxUI(storageData.boxUiPrefab);
                if (boxPanelUI != null)
                {
                    boxPanelUI.Open(this);
                    if (showDebugInfo)
                        Debug.Log($"[ChestController] 通过 PackagePanelTabsUI 打开箱子 UI: {storageData.boxUiPrefab.name}");
                    return;
                }
            }

            // 🔥 备用方案：直接在 Canvas 下实例化（不推荐）
            if (_cachedCanvas == null)
                _cachedCanvas = FindFirstObjectByType<Canvas>();
            
            if (_cachedCanvas == null)
            {
                Debug.LogError("[ChestController] 场景中没有 Canvas！");
                return;
            }

            var uiInstance = Instantiate(storageData.boxUiPrefab, _cachedCanvas.transform);
            var boxUI = uiInstance.GetComponent<BoxPanelUI>();

            if (boxUI == null)
            {
                Debug.LogError($"[ChestController] UI Prefab {storageData.boxUiPrefab.name} 缺少 BoxPanelUI 组件！");
                Destroy(uiInstance);
                return;
            }

            boxUI.Open(this);

            if (showDebugInfo)
                Debug.Log($"[ChestController] 直接实例化箱子 UI: {storageData.boxUiPrefab.name}");
        }

        /// <summary>
        /// 获取交互提示文本
        /// </summary>
        public string GetInteractionHint(InteractionContext context)
        {
            if (isLocked && ownership != ChestOwnership.Player)
                return "使用钥匙解锁";
            return "打开箱子";
        }

        #endregion

        #region 受击处理（旧接口 - 兼容）

        public bool OnHit(int damage, ToolType toolType, Vector2 attackerDirection)
        {
            PlayShakeEffect();

            if (toolType != ToolType.Pickaxe)
            {
                if (showDebugInfo)
                    Debug.Log($"[ChestController] 非镐子工具无法对箱子造成伤害: {toolType}");
                return false;
            }

            if (!IsEmpty)
            {
                TryPush(attackerDirection);
                return false;
            }

            currentHealth -= damage;
            if (showDebugInfo)
                Debug.Log($"[ChestController] 受到伤害: {damage}, 剩余血量: {currentHealth}");

            if (currentHealth <= 0)
            {
                OnDestroyed();
                return true;
            }
            return true;
        }

        private void OnDestroyed()
        {
            if (showDebugInfo)
                Debug.Log($"[ChestController] 箱子被销毁，生成掉落物");

            if (storageData != null && WorldSpawnService.Instance != null)
            {
                WorldSpawnService.Instance.SpawnWithAnimation(
                    storageData, 0, 1, transform.position, Vector3.up);
            }

            Destroy(gameObject);
        }

        #endregion
    }

}