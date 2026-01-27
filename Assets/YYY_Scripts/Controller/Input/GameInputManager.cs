using UnityEngine;
using UnityEngine.EventSystems;
using FarmGame.Data;
using FarmGame.UI;
using FarmGame.World;
using FarmGame.Farm;

public class GameInputManager : MonoBehaviour
{
    [SerializeField, HideInInspector] private PlayerMovement playerMovement;
    [SerializeField, HideInInspector] private PlayerInteraction playerInteraction;
    [SerializeField, HideInInspector] private PlayerToolController playerToolController;
    [SerializeField, HideInInspector] private PlayerAutoNavigator autoNavigator;

    [SerializeField, HideInInspector] private InventoryService inventory;
    [SerializeField, HideInInspector] private HotbarSelectionService hotbarSelection;
    [SerializeField, HideInInspector] private PackagePanelTabsUI packageTabs;
    
    private ItemDatabase database; // 从 InventoryService 获取

    [SerializeField] private bool useAxisForMovement = false;
    [SerializeField, HideInInspector] private Camera worldCamera;
    [Header("交互设置")]
    [SerializeField] private LayerMask interactableMask;
    [SerializeField] private string[] interactableTags = new string[0];
    [SerializeField] private bool blockNavOverUI = false;
    [SerializeField, Range(0f, 1.5f)] private float navClickDeadzone = 0.3f; // 以玩家为圆心的点击死区
    [SerializeField, Range(0.05f, 0.5f)] private float navClickCooldown = 0.15f; // 导航点击间隔，防抖
    [SerializeField, Range(0.2f, 2f)] private float minNavDistance = 0.5f; // 最小导航距离，防止连续点击同一位置
    [Header("调试开关")]
    [SerializeField, HideInInspector] private TimeManagerDebugger timeDebugger;
    [SerializeField] private bool enableTimeDebugKeys = false;
    [Header("UI自动激活")]
    [SerializeField] private bool autoActivateUIRoot = true;
    [SerializeField] private string uiRootName = "UI";

    private GameObject uiRootCache;
    private bool packageTabsInitialized = false;

    private static GameInputManager s_instance;
    private float lastNavClickTime = -1f;
    private Vector3 lastNavClickPos = Vector3.zero;

    void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            enabled = false;
            return;
        }
        s_instance = this;

        if (playerMovement == null) playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerInteraction == null) playerInteraction = FindFirstObjectByType<PlayerInteraction>();
        if (playerToolController == null) playerToolController = FindFirstObjectByType<PlayerToolController>();
        if (autoNavigator == null) autoNavigator = FindFirstObjectByType<PlayerAutoNavigator>();

        if (inventory == null) inventory = FindFirstObjectByType<InventoryService>();
        if (hotbarSelection == null) hotbarSelection = FindFirstObjectByType<HotbarSelectionService>();
        if (packageTabs == null) packageTabs = FindFirstObjectByType<PackagePanelTabsUI>(FindObjectsInactive.Include);

        // 从 InventoryService 获取 database(ItemDatabase 是 ScriptableObject,不能用 Find)
        if (inventory != null)
            database = inventory.Database;

        if (worldCamera == null) worldCamera = Camera.main;
    }

    void OnDestroy()
    {
        if (s_instance == this)
        {
            s_instance = null;
        }
    }

    void Start()
    {
        // 运行时自动激活UI根物体
        var uiRoot = ResolveUIRoot();
        if (autoActivateUIRoot)
        {
            if (uiRoot != null && !uiRoot.activeSelf)
            {
                uiRoot.SetActive(true);
            }
            else if (uiRoot == null)
            {
                Debug.LogError($"未找到名为 '{uiRootName}' 的UI根物体！");
            }
        }

        packageTabs = EnsurePackageTabs();
        if (packageTabs == null)
        {
            Debug.LogError("PackagePanelTabsUI 仍然为 null，无法初始化面板热键！");
        }
    }

    void Update()
    {
        HandlePanelHotkeys();
        HandleRunToggleWhileNav();
        HandleMovement();
        HandleHotbarSelection();
        HandleUseCurrentTool();
        HandleRightClickAutoNav();
        if (timeDebugger != null) timeDebugger.enableDebugKeys = enableTimeDebugKeys;
    }

    void HandleRunToggleWhileNav()
    {
        // ✅ Shift 逻辑已由 SprintStateManager 统一管理，这里不需要处理
        // 导航会自动从 SprintStateManager 获取疾跑状态
    }

    void HandleMovement()
    {
        // 背包或箱子UI打开时禁用移动输入
        bool uiOpen = IsAnyPanelOpen();
        if (uiOpen)
        {
            if (playerMovement != null) playerMovement.SetMovementInput(Vector2.zero, false);
            return;
        }
        
        Vector2 input;
        if (useAxisForMovement)
        {
            input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        }
        else
        {
            float x = (Input.GetKey(KeyCode.D) ? 1f : 0f) + (Input.GetKey(KeyCode.A) ? -1f : 0f);
            float y = (Input.GetKey(KeyCode.W) ? 1f : 0f) + (Input.GetKey(KeyCode.S) ? -1f : 0f);
            input = new Vector2(x, y);
        }
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // 检查是否处于工具动作锁定状态
        var lockManager = ToolActionLockManager.Instance;
        if (lockManager != null && lockManager.IsLocked)
        {
            // 锁定状态：缓存方向输入，不执行移动，也不传递给 PlayerMovement
            if (input.sqrMagnitude > 0.01f)
            {
                lockManager.CacheDirection(input);
            }
            // 重要：清空 PlayerMovement 的输入，防止朝向被更新
            if (playerMovement != null) playerMovement.SetMovementInput(Vector2.zero, false);
            return;
        }

        // 若自动导航激活：
        if (autoNavigator != null && autoNavigator.IsActive)
        {
            // 只要玩家有任意输入则打断导航；否则不要写入移动值，避免覆盖导航输入
            if (Mathf.Abs(input.x) > 0.01f || Mathf.Abs(input.y) > 0.01f)
            {
                autoNavigator.ForceCancel();  // 🔥 P0-1：使用 ForceCancel 替代 Cancel
                if (playerMovement != null) playerMovement.SetMovementInput(input, shift);
            }
            return;
        }

        // 非导航状态，正常写入移动
        if (playerMovement != null) playerMovement.SetMovementInput(input, shift);
    }

    static int s_lastScrollFrame = -1;
    static float s_lastScrollTime = -1f;
    const float ScrollCooldown = 0.08f; // 秒
    
    // 滚轮累积值（用于锁定状态下累积多次滚动）
    private int _accumulatedScrollSteps = 0;

    void HandleHotbarSelection()
    {
        // 面板打开或鼠标在UI上时，禁用滚轮切换工具栏
        bool uiOpen = IsAnyPanelOpen();
        bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        
        // 检查是否处于工具动作锁定状态
        var lockManager = ToolActionLockManager.Instance;
        bool isLocked = lockManager != null && lockManager.IsLocked;
        
        float scroll = (uiOpen || pointerOverUI) ? 0f : Input.mouseScrollDelta.y;
        
        // 滚轮处理
        if (scroll != 0f)
        {
            // 防抖：同一帧只处理一次；并加时间冷却避免一次滚动触发多帧事件
            bool shouldProcess = Time.frameCount != s_lastScrollFrame && 
                                 (Time.unscaledTime - s_lastScrollTime) >= ScrollCooldown;
            
            if (shouldProcess)
            {
                s_lastScrollFrame = Time.frameCount;
                s_lastScrollTime = Time.unscaledTime;
                
                // 计算滚动步数（支持高精度滚轮）
                int scrollSteps = scroll > 0 ? -1 : 1; // 向上滚 = -1（上一个），向下滚 = +1（下一个）
                
                if (isLocked)
                {
                    // 锁定状态：累积滚轮步数
                    _accumulatedScrollSteps += scrollSteps;
                    
                    // 计算目标索引（基于当前选中 + 累积步数）
                    int currentIndex = hotbarSelection != null ? hotbarSelection.selectedIndex : 0;
                    int targetIndex = (currentIndex + _accumulatedScrollSteps) % InventoryService.HotbarWidth;
                    if (targetIndex < 0) targetIndex += InventoryService.HotbarWidth;
                    
                    // 缓存最终目标索引
                    lockManager.CacheHotbarInput(targetIndex);
                }
                else
                {
                    // 正常切换：重置累积值
                    _accumulatedScrollSteps = 0;
                    
                    if (scrollSteps > 0) hotbarSelection?.SelectNext();
                    else hotbarSelection?.SelectPrev();
                }
            }
        }
        
        // 解锁时重置累积值
        if (!isLocked && _accumulatedScrollSteps != 0)
        {
            _accumulatedScrollSteps = 0;
        }

        // 数字键切换 - 面板打开时禁用
        if (uiOpen) return;
        
        int keyIndex = -1;
        if (Input.GetKeyDown(KeyCode.Alpha1)) keyIndex = 0;
        else if (Input.GetKeyDown(KeyCode.Alpha2)) keyIndex = 1;
        else if (Input.GetKeyDown(KeyCode.Alpha3)) keyIndex = 2;
        else if (Input.GetKeyDown(KeyCode.Alpha4)) keyIndex = 3;
        else if (Input.GetKeyDown(KeyCode.Alpha5)) keyIndex = 4;
        
        if (keyIndex >= 0)
        {
            if (isLocked)
            {
                // 锁定状态：缓存输入（数字键直接指定索引，重置累积值）
                _accumulatedScrollSteps = 0;
                lockManager.CacheHotbarInput(keyIndex);
            }
            else
            {
                // 正常切换
                hotbarSelection?.SelectIndex(keyIndex);
            }
        }
    }

    void HandlePanelHotkeys()
    {
        var tabs = EnsurePackageTabs();
        
        // ESC 键：优先关闭箱子UI，其次打开设置
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (BoxPanelUI.ActiveInstance != null && BoxPanelUI.ActiveInstance.IsOpen)
            {
                // 🔥 修正：使用 PackagePanelTabsUI 的统一关闭逻辑
                if (tabs != null)
                {
                    tabs.CloseBoxUI(false); // ESC 触发，不打开背包
                }
                else
                {
                    BoxPanelUI.ActiveInstance.Close(); // 兜底
                }
                return;
            }
            if (tabs != null) tabs.OpenSettings();
            return;
        }
        
        // 🔥 P0-1 修正：Tab 键特殊处理
        // Box 打开时按 Tab → 关闭 Box，打开背包
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (BoxPanelUI.ActiveInstance != null && BoxPanelUI.ActiveInstance.IsOpen)
            {
                if (tabs != null)
                {
                    tabs.CloseBoxUI(true); // Tab 触发，关闭 Box 后打开背包
                }
                return;
            }
            if (tabs != null) tabs.OpenProps();
            return;
        }
        
        // 🔥 修正：其他快捷键直接调用 PackagePanelTabsUI 的方法
        // 让 PackagePanelTabsUI 内部处理 Box UI 的关闭和状态恢复
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (tabs != null) tabs.OpenRecipes();
            return;
        }
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (tabs != null) tabs.OpenMap();
            return;
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (tabs != null) tabs.OpenEx();
            return;
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            if (tabs != null) tabs.OpenRelations();
            return;
        }
    }
    
    /// <summary>
    /// 检查是否有任何面板打开
    /// </summary>
    private bool IsAnyPanelOpen()
    {
        bool packageOpen = packageTabs != null && packageTabs.IsPanelOpen();
        bool boxOpen = BoxPanelUI.ActiveInstance != null && BoxPanelUI.ActiveInstance.IsOpen;
        return packageOpen || boxOpen;
    }
    
    /// <summary>
    /// 如果箱子面板打开则关闭
    /// </summary>
    private void CloseBoxPanelIfOpen()
    {
        if (BoxPanelUI.ActiveInstance != null && BoxPanelUI.ActiveInstance.IsOpen)
        {
            BoxPanelUI.ActiveInstance.Close();
        }
    }

    void HandleUseCurrentTool()
    {
        // 任何面板打开时禁用工具使用
        bool uiOpen = IsAnyPanelOpen();
        if (uiOpen) return;
        
        // 改为 GetMouseButton 支持长按连续使用
        // 但首次触发仍需要 GetMouseButtonDown，后续由 PlayerInteraction 处理连续
        bool isFirstPress = Input.GetMouseButtonDown(0);
        bool isHolding = Input.GetMouseButton(0);
        
        // 检查是否正在执行动作
        bool isPerformingAction = playerInteraction != null && playerInteraction.IsPerformingAction();
        
        // 首次按下时触发，或者动作完成后继续长按时由 PlayerInteraction 内部处理
        if (!isFirstPress)
        {
            // 非首次按下，如果正在执行动作则由 PlayerInteraction 处理连续
            return;
        }
        
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        
        // ★ 检查是否处于放置模式（优先 V3 → V2 → V1）
        if (PlacementManagerV3.Instance != null && PlacementManagerV3.Instance.IsPlacementMode)
        {
            PlacementManagerV3.Instance.OnLeftClick();
            return;
        }
        if (PlacementManagerV2.Instance != null && PlacementManagerV2.Instance.IsPlacementMode)
        {
            PlacementManagerV2.Instance.TryPlace();
            return;
        }
        if (PlacementManager.Instance != null && PlacementManager.Instance.IsPlacementMode)
        {
            PlacementManager.Instance.TryPlace();
            return;
        }
        
        if (inventory == null || database == null || hotbarSelection == null) return;
        
        // 如果正在执行动作，不重复触发
        if (isPerformingAction) return;

        int idx = Mathf.Clamp(hotbarSelection.selectedIndex, 0, InventoryService.HotbarWidth - 1);
        var slot = inventory.GetSlot(idx);
        if (slot.IsEmpty) return;

        var itemData = database.GetItemByID(slot.itemId);
        if (itemData == null) return;

        if (itemData is ToolData tool)
        {
            // ★ 农田工具特殊处理
            if (TryHandleFarmingTool(tool))
            {
                // 农田工具已处理，播放动画
                var action = ResolveAction(tool.toolType);
                playerInteraction?.RequestAction(action);
                return;
            }
            
            // 其他工具正常处理
            var toolAction = ResolveAction(tool.toolType);
            playerInteraction?.RequestAction(toolAction);
        }
        else if (itemData is SeedData seedData)
        {
            // ★ 种子种植处理
            TryPlantSeed(seedData);
        }
        else if (itemData is WeaponData weapon)
        {
            // 根据武器的动画动作类型决定人物动画
            var action = ResolveWeaponAction(weapon.animActionType);
            playerInteraction?.RequestAction(action);
        }
    }

    void HandleRightClickAutoNav()
    {
        if (!Input.GetMouseButtonDown(1)) return;
        
        // 任何面板打开时禁用右键导航
        bool uiOpen = IsAnyPanelOpen();
        bool boxOpen = BoxPanelUI.ActiveInstance != null && BoxPanelUI.ActiveInstance.IsOpen;
        
        // 🔥 P0-1 修复：Box 打开时，右键点击另一个箱子应该先关闭当前 Box，然后导航到新箱子
        // 但普通背包打开时，右键导航仍然禁用
        bool packageOpen = packageTabs != null && packageTabs.IsPanelOpen() && !boxOpen;
        
        if (packageOpen)
        {
            // 背包打开（非 Box 模式），禁用右键导航
            return;
        }
        
        // blockNavOverUI 只阻挡导航，不应该阻挡面板热键
        if (blockNavOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        
        // ★ 农田系统：尝试收获作物
        if (TryHarvestCropAtMouse())
        {
            return; // 收获成功，不继续导航逻辑
        }
        
        if (autoNavigator == null) return;

        // 防抖：点击间隔限制
        float currentTime = Time.unscaledTime;
        if (currentTime - lastNavClickTime < navClickCooldown)
        {
            return;
        }

        var cam = worldCamera != null ? worldCamera : Camera.main;
        if (cam == null) return;
        Vector3 mouse = Input.mousePosition;
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, 0f));
        world.z = 0f;

        // 点击死区：靠近玩家的区域忽略导航（使用Collider中心）
        Vector2 playerCenter = Vector2.zero;
        if (playerMovement != null)
        {
            var player = playerMovement.transform;
            var col = playerMovement.GetComponent<Collider2D>();
            playerCenter = col != null ? (Vector2)col.bounds.center : (Vector2)player.position;
            if (Vector2.Distance(playerCenter, world) <= navClickDeadzone)
            {
                return;
            }
        }

        // 防止连续点击同一位置（鬼畜问题）
        if (autoNavigator.IsActive && Vector3.Distance(world, lastNavClickPos) < minNavDistance)
        {
            // 如果已在导航且点击位置过近，忽略
            return;
        }

        // 更新点击记录
        lastNavClickTime = currentTime;
        lastNavClickPos = world;

        // 🔥 C3：优先使用 Sprite Bounds 检测 IResourceNode（箱子、树木等）
        // 因为这些物体的 Collider 只覆盖底部，但交互应该基于整个 Sprite
        var resourceNodes = ResourceNodeRegistry.Instance?.GetNodesInRange(world, 2f);
        if (resourceNodes != null)
        {
            foreach (var node in resourceNodes)
            {
                var bounds = node.GetBounds(); // SpriteRenderer.bounds
                if (bounds.Contains(world))
                {
                    // 点击在 Sprite 范围内，检查是否实现 IInteractable
                    var interactable = node as IInteractable;
                    if (interactable != null)
                    {
                        var nodeGO = (node as MonoBehaviour)?.gameObject;
                        if (nodeGO != null)
                        {
                            // 🔥 P0-1：如果 Box 打开，先关闭再导航
                            if (boxOpen)
                            {
                                CloseBoxPanelIfOpen();
                            }
                            HandleInteractable(interactable, nodeGO.transform, playerCenter);
                            return;
                        }
                    }
                }
            }
        }

        // 🔥 使用通用目标选择器，收集所有 IInteractable 并按优先级排序
        var hits = Physics2D.OverlapPointAll(world);
        var candidates = new System.Collections.Generic.List<(IInteractable interactable, Transform tr, float distance)>();
        
        foreach (var h in hits)
        {
            // 忽略自身碰撞
            if (playerMovement != null && (h.transform == playerMovement.transform || h.transform.IsChildOf(playerMovement.transform)))
                continue;
            
            // 🔥 关键：从碰撞体或其父级获取 IInteractable
            var interactable = h.GetComponent<IInteractable>();
            if (interactable == null)
                interactable = h.GetComponentInParent<IInteractable>();
            
            if (interactable == null) continue;
            
            float dist = Vector2.Distance(playerCenter, h.transform.position);
            // 稍微放宽范围，允许导航到目标附近
            if (dist > interactable.InteractionDistance * 2f) continue;
            
            candidates.Add((interactable, h.transform, dist));
        }
        
        // 🔥 如果有交互候选，按优先级排序选择目标
        if (candidates.Count > 0)
        {
            // 按优先级降序排序，同优先级时距离近的优先
            candidates.Sort((a, b) =>
            {
                int p = b.interactable.InteractionPriority.CompareTo(a.interactable.InteractionPriority);
                if (p != 0) return p;
                return a.distance.CompareTo(b.distance);
            });
            
            var best = candidates[0];
            
            // 🔥 P0-1：如果 Box 打开，先关闭再导航
            if (boxOpen)
            {
                CloseBoxPanelIfOpen();
            }
            HandleInteractable(best.interactable, best.tr, playerCenter);
            return;
        }
        
        // 🔥 没有 IInteractable，检查是否有其他可跟随的目标（通过 Tag/Layer）
        Transform found = null;
        foreach (var h in hits)
        {
            if (playerMovement != null && (h.transform == playerMovement.transform || h.transform.IsChildOf(playerMovement.transform)))
                continue;
            
            bool tagMatched = interactableTags != null && interactableTags.Length > 0 && HasAnyTag(h.transform, interactableTags);
            bool layerMatched = ((1 << h.gameObject.layer) & interactableMask.value) != 0;
            if (tagMatched || layerMatched)
            {
                found = h.transform;
                break;
            }
        }

        if (found != null)
        {
            // 🔥 P0-1：如果 Box 打开，先关闭再导航
            if (boxOpen)
            {
                CloseBoxPanelIfOpen();
            }
            autoNavigator.FollowTarget(found, 0.6f);
        }
        else
        {
            // 🔥 P0-1：纯导航（无目标）时，如果 Box 打开则禁用
            if (boxOpen)
            {
                return; // Box 打开时不允许纯导航
            }
            autoNavigator.SetDestination(world);
        }
    }
    
    /// <summary>
    /// 🔥 v4.0：统一处理可交互物体
    /// 使用 ClosestPoint 计算距离，确保从任何方向接近都是最短路径
    /// </summary>
    private void HandleInteractable(IInteractable interactable, Transform target, Vector2 playerCenter)
    {
        // 导航开始前取消 Held 状态
        var manager = InventoryInteractionManager.Instance;
        if (manager != null && manager.IsHolding)
        {
            manager.Cancel();
        }
        
        // 🔥 v4.0：使用 ClosestPoint 计算玩家到目标的最近距离
        Vector2 targetPos = GetTargetAnchor(target, playerCenter);
        float distance = Vector2.Distance(playerCenter, targetPos);
        float interactDist = interactable.InteractionDistance;
        
        if (showDebugInfo)
        {
            Debug.Log($"[GameInputManager] HandleInteractable: target={target.name}, distance={distance:F2}, interactDist={interactDist:F2}");
        }
        
        if (distance <= interactDist)
        {
            // 在交互距离内，直接交互
            TryInteract(interactable);
        }
        else
        {
            // 距离太远，导航到目标附近后交互
            if (autoNavigator != null)
            {
                autoNavigator.ForceCancel();
                
                autoNavigator.FollowTarget(target, interactDist * 0.8f, () =>
                {
                    // 到达后距离复核
                    TryInteractWithDistanceCheck(interactable, target);
                });
            }
        }
    }
    
    /// <summary>
    /// 🔥 v4.0：获取目标最近点（使用 ClosestPoint）
    /// 
    /// 核心思路：
    /// 1. 使用 Collider.ClosestPoint(playerPos) 计算玩家到目标的最近点
    /// 2. 这样从任何方向接近都是最短路径，不会绕路
    /// 3. 与 PlayerAutoNavigator 使用相同的距离计算方式
    /// </summary>
    private Vector2 GetTargetAnchor(Transform target, Vector2 playerPos)
    {
        // 尝试获取 Collider
        var collider = target.GetComponent<Collider2D>();
        if (collider == null)
            collider = target.GetComponentInChildren<Collider2D>();
        
        if (collider != null)
        {
            // 🔥 使用 ClosestPoint 计算玩家到 Collider 的最近点
            return collider.ClosestPoint(playerPos);
        }
        
        return target.position;
    }
    
    /// <summary>
    /// 🔥 v4.0：带距离复核的交互（使用 ClosestPoint）
    /// </summary>
    private void TryInteractWithDistanceCheck(IInteractable interactable, Transform target)
    {
        if (interactable == null || target == null) return;
        
        // 获取玩家位置
        Vector2 playerPos = GetPlayerCenter();
        
        // 🔥 v4.0：使用 ClosestPoint 计算距离
        Vector2 targetPos = GetTargetAnchor(target, playerPos);
        float distance = Vector2.Distance(playerPos, targetPos);
        float interactDist = interactable.InteractionDistance;
        
        // 允许 20% 容差
        if (distance > interactDist * 1.2f)
        {
            LogWarningOnce("DistanceTooFar", $"[GameInputManager] 距离过远，取消交互: {distance:F2} > {interactDist * 1.2f:F2}");
            return;
        }
        
        TryInteract(interactable);
    }
    
    /// <summary>
    /// 🔥 P0-1：获取玩家中心位置
    /// </summary>
    private Vector2 GetPlayerCenter()
    {
        if (playerMovement != null)
        {
            var col = playerMovement.GetComponent<Collider2D>();
            return col != null ? (Vector2)col.bounds.center : (Vector2)playerMovement.transform.position;
        }
        return Vector2.zero;
    }
    
    // 🔥 P0-1：警告去重
    private static System.Collections.Generic.HashSet<string> _loggedWarnings = new System.Collections.Generic.HashSet<string>();
    
    private void LogWarningOnce(string key, string message)
    {
        if (!_loggedWarnings.Contains(key))
        {
            _loggedWarnings.Add(key);
            Debug.LogWarning(message);
        }
    }
    
    // 🔥 P0-1：调试开关（默认关闭）
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = false;
    
    /// <summary>
    /// 尝试与可交互物体交互
    /// </summary>
    private void TryInteract(IInteractable interactable)
    {
        if (interactable == null) return;
        
        // 构建交互上下文
        var context = BuildInteractionContext();
        
        // 检查是否可以交互
        if (!interactable.CanInteract(context))
        {
            if (showDebugInfo)
                Debug.Log($"[GameInputManager] 当前无法交互");
            return;
        }
        
        // 执行交互
        interactable.OnInteract(context);
    }
    
    /// <summary>
    /// 构建交互上下文
    /// </summary>
    private InteractionContext BuildInteractionContext()
    {
        var context = new InteractionContext
        {
            Inventory = inventory,
            Database = database,
            Navigator = autoNavigator
        };
        
        // 获取玩家位置
        if (playerMovement != null)
        {
            var col = playerMovement.GetComponent<Collider2D>();
            context.PlayerPosition = col != null ? (Vector2)col.bounds.center : (Vector2)playerMovement.transform.position;
            context.PlayerTransform = playerMovement.transform;
        }
        
        // 获取手持物品信息
        if (inventory != null && hotbarSelection != null)
        {
            int idx = Mathf.Clamp(hotbarSelection.selectedIndex, 0, InventoryService.HotbarWidth - 1);
            var slot = inventory.GetSlot(idx);
            
            if (!slot.IsEmpty)
            {
                context.HeldItemId = slot.itemId;
                context.HeldItemQuality = slot.quality;
                context.HeldSlotIndex = idx;
            }
        }
        
        return context;
    }

    /// <summary>
    /// 根据工具类型解析对应的玩家动画状态
    /// 
    /// 映射规则：
    /// - Axe（斧头）→ Slice（挥砍）
    /// - Sickle（镰刀）→ Slice（挥砍）
    /// - Pickaxe（镐子）→ Crush（挖掘）
    /// - Hoe（锄头）→ Crush（挖掘）
    /// - WateringCan（洒水壶）→ Watering（浇水）
    /// - FishingRod（鱼竿）→ Fish（钓鱼）
    /// 
    /// 注意：Pierce（刺出）用于长剑等武器，不是工具
    /// </summary>
    PlayerAnimController.AnimState ResolveAction(ToolType type)
    {
        switch (type)
        {
            case ToolType.Axe: return PlayerAnimController.AnimState.Slice;      // 斧头 → 挥砍
            case ToolType.Sickle: return PlayerAnimController.AnimState.Slice;   // 镰刀 → 挥砍
            case ToolType.Pickaxe: return PlayerAnimController.AnimState.Crush;  // 镐子 → 挖掘
            case ToolType.Hoe: return PlayerAnimController.AnimState.Crush;      // 锄头 → 挖掘（修复：之前错误地映射到Pierce）
            case ToolType.WateringCan: return PlayerAnimController.AnimState.Watering; // 洒水壶 → 浇水
            case ToolType.FishingRod: return PlayerAnimController.AnimState.Fish;      // 鱼竿 → 钓鱼
            default: return PlayerAnimController.AnimState.Slice;
        }
    }

    /// <summary>
    /// 根据武器的动画动作类型解析对应的玩家动画状态
    /// 
    /// 映射规则：
    /// - Pierce → Pierce（刺出，长剑）
    /// - Slice → Slice（挥砍）
    /// - 其他 → Slice（默认）
    /// </summary>
    PlayerAnimController.AnimState ResolveWeaponAction(AnimActionType actionType)
    {
        switch (actionType)
        {
            case AnimActionType.Pierce: return PlayerAnimController.AnimState.Pierce;  // 刺出（长剑）
            case AnimActionType.Slice: return PlayerAnimController.AnimState.Slice;    // 挥砍
            case AnimActionType.Crush: return PlayerAnimController.AnimState.Crush;    // 挖掘（如果武器有这种类型）
            default: return PlayerAnimController.AnimState.Slice;
        }
    }

    #region 农田系统集成
    
    /// <summary>
    /// 尝试处理农田工具（锄头、水壶）
    /// </summary>
    /// <param name="tool">工具数据</param>
    /// <returns>是否已处理（true=农田工具已处理，false=非农田工具）</returns>
    private bool TryHandleFarmingTool(ToolData tool)
    {
        if (tool == null) return false;
        
        // 获取鼠标世界坐标
        Vector3 worldPos = GetMouseWorldPosition();
        
        switch (tool.toolType)
        {
            case ToolType.Hoe:
                // 锄头 → 锄地
                return TryTillSoil(worldPos);
                
            case ToolType.WateringCan:
                // 水壶 → 浇水
                return TryWaterTile(worldPos);
                
            default:
                return false;
        }
    }
    
    /// <summary>
    /// 尝试锄地
    /// 直接调用 FarmTileManager，不经过 FarmingManagerNew
    /// </summary>
    private bool TryTillSoil(Vector3 worldPosition)
    {
        // 直接使用 FarmTileManager
        var farmTileManager = FarmGame.Farm.FarmTileManager.Instance;
        if (farmTileManager == null)
        {
            if (showDebugInfo)
                Debug.Log("[GameInputManager] FarmTileManager 未初始化");
            return false;
        }
        
        // 获取当前楼层
        int layerIndex = farmTileManager.GetCurrentLayerIndex(worldPosition);
        var tilemaps = farmTileManager.GetLayerTilemaps(layerIndex);
        if (tilemaps == null)
        {
            if (showDebugInfo)
                Debug.Log($"[GameInputManager] 楼层 {layerIndex} 的 Tilemap 未配置");
            return false;
        }
        
        // 转换为格子坐标
        Vector3Int cellPosition = tilemaps.WorldToCell(worldPosition);
        
        // 检查是否可以耕作
        if (!farmTileManager.CanTillAt(layerIndex, cellPosition))
        {
            if (showDebugInfo)
                Debug.Log($"[GameInputManager] 无法在 {cellPosition} 耕作");
            return false;
        }
        
        // 创建耕地
        bool success = farmTileManager.CreateTile(layerIndex, cellPosition);
        
        if (showDebugInfo)
            Debug.Log($"[GameInputManager] 锄地{(success ? "成功" : "失败")}: {cellPosition}");
        
        return success;
    }
    
    /// <summary>
    /// 尝试浇水
    /// 直接调用 FarmTileManager，不经过 FarmingManagerNew
    /// </summary>
    private bool TryWaterTile(Vector3 worldPosition)
    {
        // 直接使用 FarmTileManager
        var farmTileManager = FarmGame.Farm.FarmTileManager.Instance;
        if (farmTileManager == null)
        {
            if (showDebugInfo)
                Debug.Log("[GameInputManager] FarmTileManager 未初始化");
            return false;
        }
        
        // 获取当前楼层
        int layerIndex = farmTileManager.GetCurrentLayerIndex(worldPosition);
        var tilemaps = farmTileManager.GetLayerTilemaps(layerIndex);
        if (tilemaps == null)
        {
            if (showDebugInfo)
                Debug.Log($"[GameInputManager] 楼层 {layerIndex} 的 Tilemap 未配置");
            return false;
        }
        
        // 转换为格子坐标
        Vector3Int cellPosition = tilemaps.WorldToCell(worldPosition);
        
        // 获取当前游戏时间
        float currentHour = TimeManager.Instance != null ? TimeManager.Instance.GetHour() : 0f;
        
        // 浇水
        bool success = farmTileManager.SetWatered(layerIndex, cellPosition, currentHour);
        
        if (showDebugInfo)
            Debug.Log($"[GameInputManager] 浇水{(success ? "成功" : "失败")}: {cellPosition}");
        
        return success;
    }
    
    /// <summary>
    /// 尝试种植种子
    /// 直接调用 CropManager 工厂实例化作物，不经过 FarmingManagerNew
    /// </summary>
    private bool TryPlantSeed(SeedData seedData)
    {
        if (seedData == null) return false;
        
        // 直接使用 FarmTileManager
        var farmTileManager = FarmGame.Farm.FarmTileManager.Instance;
        if (farmTileManager == null)
        {
            if (showDebugInfo)
                Debug.Log("[GameInputManager] FarmTileManager 未初始化");
            return false;
        }
        
        // 直接使用 CropManager 作为工厂
        var cropManager = FarmGame.Farm.CropManager.Instance;
        if (cropManager == null)
        {
            if (showDebugInfo)
                Debug.Log("[GameInputManager] CropManager 未初始化");
            return false;
        }
        
        // 获取鼠标世界坐标
        Vector3 worldPos = GetMouseWorldPosition();
        
        // 获取当前楼层
        int layerIndex = farmTileManager.GetCurrentLayerIndex(worldPos);
        var tilemaps = farmTileManager.GetLayerTilemaps(layerIndex);
        if (tilemaps == null || !tilemaps.IsValid())
        {
            if (showDebugInfo)
                Debug.Log($"[GameInputManager] 楼层 {layerIndex} 的 Tilemap 未配置");
            return false;
        }
        
        // 转换为格子坐标
        Vector3Int cellPosition = tilemaps.WorldToCell(worldPos);
        
        // 获取耕地数据
        var tileData = farmTileManager.GetTileData(layerIndex, cellPosition);
        if (tileData == null || !tileData.CanPlant())
        {
            if (showDebugInfo)
                Debug.Log($"[GameInputManager] 无法在此位置种植: {cellPosition}");
            return false;
        }
        
        // 检查季节
        var timeManager = TimeManager.Instance;
        if (timeManager != null && !IsCorrectSeason(seedData, timeManager))
        {
            if (showDebugInfo)
                Debug.Log($"[GameInputManager] {seedData.itemName} 不适合当前季节种植");
            return false;
        }
        
        // 从背包移除种子
        if (inventory != null)
        {
            if (!inventory.RemoveItem(seedData.itemID, -1, 1))
            {
                if (showDebugInfo)
                    Debug.Log($"[GameInputManager] 背包中没有足够的种子: {seedData.itemName}");
                return false;
            }
        }
        
        // 获取当前天数
        int currentDay = timeManager?.GetTotalDaysPassed() ?? 0;
        
        // 使用 CropManager 工厂创建作物
        Vector3 cropWorldPos = tilemaps.GetCellCenterWorld(cellPosition);
        Transform container = tilemaps.propsContainer;
        
        var controller = cropManager.CreateCrop(layerIndex, cellPosition, seedData, currentDay, cropWorldPos, container);
        if (controller == null)
        {
            // 创建失败，退还种子
            if (inventory != null)
            {
                inventory.AddItem(seedData.itemID, 0, 1);
            }
            return false;
        }
        
        // 更新耕地数据
        tileData.SetCropData(new FarmGame.Farm.CropInstanceData(seedData.itemID, currentDay));
        
        if (showDebugInfo)
            Debug.Log($"[GameInputManager] 种植成功: {seedData.itemName}, Layer={layerIndex}, Pos={cellPosition}");
        
        return true;
    }
    
    /// <summary>
    /// 检查种子是否适合当前季节
    /// </summary>
    private bool IsCorrectSeason(SeedData seedData, TimeManager timeManager)
    {
        if (timeManager == null) return true;
        
        // 全季节种子可以任何季节种植
        if (seedData.season == FarmGame.Data.Season.AllSeason)
            return true;
        
        SeasonManager.Season currentSeason = timeManager.GetSeason();
        return (int)seedData.season == (int)currentSeason;
    }
    
    /// <summary>
    /// 获取鼠标世界坐标
    /// </summary>
    private Vector3 GetMouseWorldPosition()
    {
        var cam = worldCamera != null ? worldCamera : Camera.main;
        if (cam == null) return Vector3.zero;
        
        Vector3 mousePos = Input.mousePosition;
        Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0f));
        worldPos.z = 0f;
        
        return worldPos;
    }
    
    /// <summary>
    /// 尝试在鼠标位置收获作物
    /// 直接访问 CropController 组件进行收获，不经过 FarmingManagerNew
    /// </summary>
    private bool TryHarvestCropAtMouse()
    {
        // 直接使用 FarmTileManager
        var farmTileManager = FarmGame.Farm.FarmTileManager.Instance;
        if (farmTileManager == null) return false;
        
        // 直接使用 CropManager
        var cropManager = FarmGame.Farm.CropManager.Instance;
        if (cropManager == null) return false;
        
        Vector3 worldPos = GetMouseWorldPosition();
        
        // 获取当前楼层
        int layerIndex = farmTileManager.GetCurrentLayerIndex(worldPos);
        var tilemaps = farmTileManager.GetLayerTilemaps(layerIndex);
        if (tilemaps == null || !tilemaps.IsValid())
        {
            return false;
        }
        
        // 转换为格子坐标
        Vector3Int cellPosition = tilemaps.WorldToCell(worldPos);
        
        // 获取耕地数据
        var tileData = farmTileManager.GetTileData(layerIndex, cellPosition);
        if (tileData == null || !tileData.HasCrop())
        {
            return false;
        }
        
        // 获取种子数据
        SeedData seedData = null;
        if (database != null && tileData.cropData != null)
        {
            seedData = database.GetItemByID(tileData.cropData.seedDataID) as SeedData;
        }
        
        // 尝试收获
        if (cropManager.TryHarvest(layerIndex, cellPosition, tileData, seedData, out int cropID, out int amount))
        {
            // 添加到背包
            if (cropID > 0 && amount > 0 && inventory != null)
            {
                inventory.AddItem(cropID, 0, amount);
            }
            
            if (showDebugInfo)
                Debug.Log($"[GameInputManager] 收获成功: CropID={cropID}, Amount={amount}");
            return true;
        }
        
        return false;
    }
    
    #endregion

    static bool HasAnyTag(Transform t, string[] tags)
    {
        if (t == null || tags == null) return false;
        foreach (var tag in tags)
        {
            if (!string.IsNullOrEmpty(tag) && t.CompareTag(tag)) return true;
        }
        var p = t.parent;
        while (p != null)
        {
            foreach (var tag in tags)
            {
                if (!string.IsNullOrEmpty(tag) && p.CompareTag(tag)) return true;
            }
            p = p.parent;
        }
        return false;
    }

    PackagePanelTabsUI EnsurePackageTabs()
    {
        if (packageTabs == null)
        {
            packageTabs = ResolvePackageTabs();
        }
        if (packageTabs != null && !packageTabsInitialized)
        {
            packageTabs.EnsureReady();
            packageTabsInitialized = true;
        }
        return packageTabs;
    }

    PackagePanelTabsUI ResolvePackageTabs()
    {
        var uiRoot = ResolveUIRoot();
        if (uiRoot != null)
        {
            var tabs = uiRoot.GetComponentInChildren<PackagePanelTabsUI>(true);
            if (tabs != null) return tabs;
        }
        return FindFirstObjectByType<PackagePanelTabsUI>(FindObjectsInactive.Include);
    }

    GameObject ResolveUIRoot()
    {
        if (uiRootCache != null) return uiRootCache;
        var scene = gameObject.scene;
        if (scene.IsValid())
        {
            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == uiRootName)
                {
                    uiRootCache = roots[i];
                    return uiRootCache;
                }
            }
        }
        var fallback = GameObject.Find(uiRootName);
        if (fallback != null) uiRootCache = fallback;
        return uiRootCache;
    }
}
