using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using FarmGame.Data;

/// <summary>
/// 配方槽位 UI 组件
/// 显示单个配方的图标和名称
/// 
/// **Feature: crafting-station-system**
/// **Validates: Requirements 3.1, 3.2, 3.3, 3.4**
/// </summary>
public class RecipeSlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI 引用")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Text nameText;
    [SerializeField] private Image lockOverlay;
    [SerializeField] private Image selectedBorder;
    [SerializeField] private Button button;
    [SerializeField] private Text levelRequirementText;  // 等级要求提示
    
    [Header("透明度设置")]
    [SerializeField] private float fullAlpha = 1.0f;      // 可制作
    [SerializeField] private float dimmedAlpha = 0.5f;    // 条件不足
    
    [Header("颜色")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(1f, 0.9f, 0.5f);
    [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] private Color levelRequirementColor = new Color(1f, 0.5f, 0.5f);
    
    private RecipeData recipe;
    private CraftingPanel panel;
    private CraftingService craftingService;
    private bool isUnlocked;
    private bool canCraft;
    private bool isSelected;
    
    /// <summary>
    /// 设置配方槽位
    /// </summary>
    public void Setup(RecipeData recipe, CraftingPanel panel, bool unlocked, ItemDatabase database)
    {
        this.recipe = recipe;
        this.panel = panel;
        this.isUnlocked = unlocked;
        
        // 获取 CraftingService 引用
        craftingService = FindFirstObjectByType<CraftingService>();
        
        // 检查是否可以制作
        canCraft = craftingService != null && craftingService.CanCraft(recipe);
        
        // 获取产物数据
        var resultItem = database?.GetItemByID(recipe.resultItemID);
        
        // 计算透明度
        float alpha = CalculateAlpha(unlocked, canCraft);
        
        // 设置图标
        if (iconImage != null)
        {
            iconImage.sprite = resultItem?.icon;
            SetImageAlpha(iconImage, alpha);
        }
        
        // 设置名称
        if (nameText != null)
        {
            nameText.text = recipe.recipeName;
            SetTextAlpha(nameText, alpha);
        }
        
        // 设置锁定遮罩
        if (lockOverlay != null)
        {
            lockOverlay.gameObject.SetActive(!unlocked);
        }
        
        // 设置等级要求提示
        UpdateLevelRequirement(recipe, unlocked);
        
        // 设置按钮交互（只有可制作时才可点击）
        if (button != null)
        {
            button.interactable = canCraft;
        }
        
        SetSelected(false);
    }
    
    /// <summary>
    /// 计算透明度
    /// **Property 4: 透明度与条件一致性**
    /// </summary>
    private float CalculateAlpha(bool unlocked, bool canCraft)
    {
        // 可制作 → 透明度 1.0
        if (canCraft) return fullAlpha;
        
        // 条件不足（材料不足或等级不足）→ 透明度 0.5
        return dimmedAlpha;
    }
    
    /// <summary>
    /// 设置图片透明度
    /// </summary>
    private void SetImageAlpha(Image image, float alpha)
    {
        if (image == null) return;
        var color = image.color;
        color.a = alpha;
        image.color = color;
    }
    
    /// <summary>
    /// 设置文本透明度
    /// </summary>
    private void SetTextAlpha(Text text, float alpha)
    {
        if (text == null) return;
        var color = text.color;
        color.a = alpha;
        text.color = color;
    }
    
    /// <summary>
    /// 更新等级要求提示
    /// </summary>
    private void UpdateLevelRequirement(RecipeData recipe, bool unlocked)
    {
        if (levelRequirementText == null) return;
        
        // 如果已解锁，隐藏等级提示
        if (unlocked)
        {
            levelRequirementText.gameObject.SetActive(false);
            return;
        }
        
        // 检查是否因为等级不足而锁定
        int requiredLevel = recipe.requiredSkillLevel;
        if (requiredLevel > 1)
        {
            levelRequirementText.gameObject.SetActive(true);
            string skillName = GetSkillName(recipe.requiredSkillType);
            levelRequirementText.text = $"需要{skillName}等级 {requiredLevel}";
            levelRequirementText.color = levelRequirementColor;
        }
        else if (recipe.requiredLevel > 1)
        {
            // 兼容旧的 requiredLevel 字段
            levelRequirementText.gameObject.SetActive(true);
            levelRequirementText.text = $"需要等级 {recipe.requiredLevel}";
            levelRequirementText.color = levelRequirementColor;
        }
        else
        {
            levelRequirementText.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 获取技能名称
    /// </summary>
    private string GetSkillName(SkillType skillType)
    {
        return skillType switch
        {
            SkillType.Combat => "战斗",
            SkillType.Gathering => "采集",
            SkillType.Crafting => "制作",
            SkillType.Cooking => "烹饪",
            SkillType.Fishing => "钓鱼",
            _ => "技能"
        };
    }
    
    /// <summary>
    /// 刷新显示状态（材料变化时调用）
    /// </summary>
    public void RefreshDisplay()
    {
        if (recipe == null || craftingService == null) return;
        
        // 重新检查是否可以制作
        canCraft = craftingService.CanCraft(recipe);
        isUnlocked = craftingService.IsRecipeUnlocked(recipe);
        
        // 更新透明度
        float alpha = CalculateAlpha(isUnlocked, canCraft);
        SetImageAlpha(iconImage, alpha);
        SetTextAlpha(nameText, alpha);
        
        // 更新按钮状态
        if (button != null)
        {
            button.interactable = canCraft;
        }
    }
    
    /// <summary>
    /// 设置选中状态
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        if (selectedBorder != null)
        {
            selectedBorder.gameObject.SetActive(selected);
        }
        
        if (iconImage != null && isUnlocked)
        {
            var color = selected ? selectedColor : normalColor;
            color.a = CalculateAlpha(isUnlocked, canCraft);
            iconImage.color = color;
        }
    }
    
    /// <summary>
    /// 点击事件
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (panel != null)
        {
            panel.SelectRecipe(recipe);
        }
    }
    
    /// <summary>
    /// 按钮点击（备用）
    /// </summary>
    public void OnClick()
    {
        if (panel != null)
        {
            panel.SelectRecipe(recipe);
        }
    }
}
