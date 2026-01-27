using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FarmGame.Data;

/// <summary>
/// 制作台 UI 主面板
/// 管理配方列表和制作交互
/// 
/// **Feature: ui-system**
/// **Validates: Requirements 1.1, 1.2, 1.3, 1.4, 3.1, 3.2, 3.3, 3.5**
/// </summary>
public class CraftingPanel : MonoBehaviour
{
    [Header("服务")]
    [SerializeField] private CraftingService craftingService;
    
    [Header("UI 引用")]
    [SerializeField] private Transform recipeListContainer;
    [SerializeField] private GameObject recipeSlotPrefab;
    [SerializeField] private RecipeDetailPanel detailPanel;
    [SerializeField] private Button craftButton;
    [SerializeField] private Text craftButtonText;
    [SerializeField] private Text titleText;
    [SerializeField] private Button closeButton;
    
    [Header("设置")]
    [SerializeField] private string defaultCraftButtonText = "制作";
    [SerializeField] private string cannotCraftText = "材料不足";
    
    [Header("音效")]
    [SerializeField] private AudioClip craftSuccessSound;
    [SerializeField] private AudioClip craftFailSound;
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 0.8f;
    
    [Header("视觉反馈")]
    [SerializeField] private Image craftSuccessFlash;
    [SerializeField] private float flashDuration = 0.3f;
    [SerializeField] private Color flashColor = new Color(1f, 1f, 0.5f, 0.5f);
    
    private List<RecipeSlotUI> recipeSlots = new List<RecipeSlotUI>();
    private RecipeData selectedRecipe;
    private RecipeSlotUI selectedSlot;

    #region 初始化

    private void Awake()
    {
        // 尝试自动获取服务
        if (craftingService == null)
        {
            craftingService = FindFirstObjectByType<CraftingService>();
        }
        
        // 绑定按钮事件
        if (craftButton != null)
        {
            craftButton.onClick.AddListener(OnCraftButtonClick);
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }
        
        // 初始化闪光效果
        if (craftSuccessFlash != null)
        {
            craftSuccessFlash.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        // 订阅事件
        if (craftingService != null)
        {
            craftingService.OnCraftSuccess += OnCraftSuccess;
            craftingService.OnCraftFailed += OnCraftFailed;
            craftingService.OnRecipeListChanged += RefreshRecipeList;
            craftingService.OnRecipeUnlocked += OnRecipeUnlocked;
        }
    }

    private void OnDisable()
    {
        // 取消订阅
        if (craftingService != null)
        {
            craftingService.OnCraftSuccess -= OnCraftSuccess;
            craftingService.OnCraftFailed -= OnCraftFailed;
            craftingService.OnRecipeListChanged -= RefreshRecipeList;
            craftingService.OnRecipeUnlocked -= OnRecipeUnlocked;
        }
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 打开制作面板
    /// </summary>
    public void Open(CraftingStation station)
    {
        if (craftingService == null)
        {
            Debug.LogError("[CraftingPanel] CraftingService 未设置");
            return;
        }
        
        craftingService.SetStation(station);
        
        // 设置标题
        if (titleText != null)
        {
            titleText.text = GetStationName(station);
        }
        
        RefreshRecipeList();
        
        // 清空选择
        selectedRecipe = null;
        selectedSlot = null;
        if (detailPanel != null)
        {
            detailPanel.Clear();
        }
        
        RefreshCraftButton();
        
        gameObject.SetActive(true);
        Debug.Log($"<color=cyan>[CraftingPanel] 打开: {station}</color>");
    }

    /// <summary>
    /// 关闭制作面板
    /// </summary>
    public void Close()
    {
        gameObject.SetActive(false);
        Debug.Log("<color=cyan>[CraftingPanel] 关闭</color>");
    }

    /// <summary>
    /// 选择配方
    /// </summary>
    public void SelectRecipe(RecipeData recipe)
    {
        // 取消之前的选择
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
        }
        
        selectedRecipe = recipe;
        
        // 找到对应的槽位并选中
        foreach (var slot in recipeSlots)
        {
            // 通过比较来找到对应槽位（简化处理）
            slot.SetSelected(false);
        }
        
        // 更新详情面板
        if (detailPanel != null)
        {
            detailPanel.ShowRecipe(recipe, craftingService);
        }
        
        RefreshCraftButton();
        
        Debug.Log($"<color=cyan>[CraftingPanel] 选择配方: {recipe?.recipeName}</color>");
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 刷新配方列表
    /// </summary>
    private void RefreshRecipeList()
    {
        if (craftingService == null) return;
        
        var recipes = craftingService.GetAvailableRecipes();
        var database = craftingService.Database;
        
        // 清理多余的槽位
        while (recipeSlots.Count > recipes.Count)
        {
            var slot = recipeSlots[recipeSlots.Count - 1];
            recipeSlots.RemoveAt(recipeSlots.Count - 1);
            Destroy(slot.gameObject);
        }
        
        // 创建或更新槽位
        for (int i = 0; i < recipes.Count; i++)
        {
            RecipeSlotUI slot;
            
            if (i < recipeSlots.Count)
            {
                slot = recipeSlots[i];
            }
            else
            {
                var go = Instantiate(recipeSlotPrefab, recipeListContainer);
                slot = go.GetComponent<RecipeSlotUI>();
                recipeSlots.Add(slot);
            }
            
            bool unlocked = craftingService.IsRecipeUnlocked(recipes[i]);
            slot.Setup(recipes[i], this, unlocked, database);
        }
    }

    /// <summary>
    /// 刷新材料状态
    /// </summary>
    private void RefreshMaterialStatus()
    {
        if (detailPanel != null && selectedRecipe != null)
        {
            detailPanel.RefreshMaterials(selectedRecipe, craftingService);
        }
        
        RefreshCraftButton();
    }

    /// <summary>
    /// 刷新制作按钮状态
    /// </summary>
    private void RefreshCraftButton()
    {
        if (craftButton == null) return;
        
        bool canCraft = selectedRecipe != null && craftingService != null && 
                        craftingService.CanCraft(selectedRecipe);
        
        craftButton.interactable = canCraft;
        
        if (craftButtonText != null)
        {
            craftButtonText.text = canCraft ? defaultCraftButtonText : cannotCraftText;
        }
    }

    /// <summary>
    /// 制作按钮点击
    /// </summary>
    private void OnCraftButtonClick()
    {
        if (selectedRecipe == null || craftingService == null) return;
        
        craftingService.TryCraft(selectedRecipe);
    }

    /// <summary>
    /// 制作成功回调
    /// </summary>
    private void OnCraftSuccess(RecipeData recipe, CraftResult result)
    {
        RefreshMaterialStatus();
        
        // 播放成功音效
        PlaySound(craftSuccessSound);
        
        // 播放成功视觉效果
        PlaySuccessFlash();
        
        Debug.Log($"<color=green>[CraftingPanel] 制作成功: {result.message}</color>");
    }

    /// <summary>
    /// 制作失败回调
    /// </summary>
    private void OnCraftFailed(RecipeData recipe, CraftResult result)
    {
        RefreshMaterialStatus();
        
        // 播放失败音效
        PlaySound(craftFailSound);
        
        Debug.Log($"<color=red>[CraftingPanel] 制作失败: {result.message}</color>");
    }
    
    /// <summary>
    /// 配方解锁回调
    /// </summary>
    private void OnRecipeUnlocked(RecipeData recipe)
    {
        // 刷新配方列表
        RefreshRecipeList();
        
        // 刷新所有槽位的显示状态
        foreach (var slot in recipeSlots)
        {
            slot.RefreshDisplay();
        }
        
        Debug.Log($"<color=lime>[CraftingPanel] 🔓 配方解锁: {recipe?.recipeName}</color>");
    }
    
    /// <summary>
    /// 播放音效
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, soundVolume);
        }
    }
    
    /// <summary>
    /// 播放成功闪光效果
    /// </summary>
    private void PlaySuccessFlash()
    {
        if (craftSuccessFlash != null)
        {
            StartCoroutine(FlashCoroutine());
        }
    }
    
    /// <summary>
    /// 闪光效果协程
    /// </summary>
    private IEnumerator FlashCoroutine()
    {
        craftSuccessFlash.gameObject.SetActive(true);
        craftSuccessFlash.color = flashColor;
        
        float elapsed = 0f;
        Color startColor = flashColor;
        Color endColor = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
        
        while (elapsed < flashDuration)
        {
            float t = elapsed / flashDuration;
            craftSuccessFlash.color = Color.Lerp(startColor, endColor, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        craftSuccessFlash.gameObject.SetActive(false);
    }

    /// <summary>
    /// 获取设施名称
    /// </summary>
    private string GetStationName(CraftingStation station)
    {
        return station switch
        {
            CraftingStation.CookingPot => "烹饪锅",
            CraftingStation.Furnace => "熔炉",
            CraftingStation.MagicTower => "魔法塔",
            CraftingStation.AnvilForge => "铁砧",
            CraftingStation.Workbench => "工作台",
            CraftingStation.AlchemyTable => "制药台",
            CraftingStation.Grill => "烧烤架",
            _ => "制作台"
        };
    }

    #endregion
}
