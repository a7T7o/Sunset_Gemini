using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FarmGame.Data;

/// <summary>
/// 配方详情面板
/// 显示配方的产物和材料列表
/// 
/// **Feature: ui-system**
/// **Validates: Requirements 1.4, 2.1**
/// </summary>
public class RecipeDetailPanel : MonoBehaviour
{
    [Header("产物显示")]
    [SerializeField] private Image resultIcon;
    [SerializeField] private Text resultName;
    [SerializeField] private Text resultAmount;
    [SerializeField] private Text descriptionText;
    
    [Header("材料列表")]
    [SerializeField] private Transform materialListContainer;
    [SerializeField] private GameObject materialItemPrefab;
    
    [Header("其他信息")]
    [SerializeField] private Text craftingTimeText;
    [SerializeField] private Text craftingExpText;
    [SerializeField] private Text requiredLevelText;
    
    private List<MaterialItemUI> materialItems = new List<MaterialItemUI>();
    private RecipeData currentRecipe;
    
    /// <summary>
    /// 显示配方详情
    /// </summary>
    public void ShowRecipe(RecipeData recipe, CraftingService service)
    {
        currentRecipe = recipe;
        
        if (recipe == null)
        {
            Clear();
            return;
        }
        
        var database = service?.Database;
        var resultItem = database?.GetItemByID(recipe.resultItemID);
        
        // 设置产物信息
        if (resultIcon != null)
        {
            resultIcon.sprite = resultItem?.icon;
            resultIcon.enabled = resultItem?.icon != null;
        }
        
        if (resultName != null)
        {
            resultName.text = resultItem?.itemName ?? recipe.recipeName;
        }
        
        if (resultAmount != null)
        {
            resultAmount.text = recipe.resultAmount > 1 ? $"x{recipe.resultAmount}" : "";
        }
        
        if (descriptionText != null)
        {
            descriptionText.text = recipe.description;
        }
        
        // 设置其他信息
        if (craftingTimeText != null)
        {
            craftingTimeText.text = recipe.craftingTime > 0 
                ? $"制作时间: {recipe.craftingTime}秒" 
                : "制作时间: 立即";
        }
        
        if (craftingExpText != null)
        {
            craftingExpText.text = $"经验: {recipe.craftingExp}";
        }
        
        if (requiredLevelText != null)
        {
            requiredLevelText.text = recipe.requiredLevel > 1 
                ? $"需要等级: {recipe.requiredLevel}" 
                : "";
        }
        
        // 刷新材料列表
        RefreshMaterials(recipe, service);
        
        gameObject.SetActive(true);
    }
    
    /// <summary>
    /// 刷新材料列表
    /// </summary>
    public void RefreshMaterials(RecipeData recipe, CraftingService service)
    {
        if (recipe == null || service == null) return;
        
        var materials = service.GetMaterialStatus(recipe);
        
        // 清理多余的材料项
        while (materialItems.Count > materials.Count)
        {
            var item = materialItems[materialItems.Count - 1];
            materialItems.RemoveAt(materialItems.Count - 1);
            Destroy(item.gameObject);
        }
        
        // 创建或更新材料项
        for (int i = 0; i < materials.Count; i++)
        {
            MaterialItemUI item;
            
            if (i < materialItems.Count)
            {
                item = materialItems[i];
            }
            else
            {
                var go = Instantiate(materialItemPrefab, materialListContainer);
                item = go.GetComponent<MaterialItemUI>();
                materialItems.Add(item);
            }
            
            item.Setup(materials[i]);
        }
    }
    
    /// <summary>
    /// 清空显示
    /// </summary>
    public void Clear()
    {
        currentRecipe = null;
        
        if (resultIcon != null) resultIcon.enabled = false;
        if (resultName != null) resultName.text = "";
        if (resultAmount != null) resultAmount.text = "";
        if (descriptionText != null) descriptionText.text = "选择一个配方查看详情";
        if (craftingTimeText != null) craftingTimeText.text = "";
        if (craftingExpText != null) craftingExpText.text = "";
        if (requiredLevelText != null) requiredLevelText.text = "";
        
        // 清理材料项
        foreach (var item in materialItems)
        {
            Destroy(item.gameObject);
        }
        materialItems.Clear();
    }
}
