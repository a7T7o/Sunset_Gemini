using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using FarmGame.Data;

/// <summary>
/// 批量修改物品 SO 工具
/// 对已存在的 SO 资产批量更新参数
/// 
/// 功能：
/// - 自动跟随 Project 窗口选择
/// - 勾选才修改（未勾选的参数保持原值）
/// - 根据 SO 类型显示专属字段
/// - 修改后自动同步数据库
/// 
/// **Feature: so-design-system**
/// **Validates: Requirements 1.1, 1.2, 1.3, 1.4, 1.5, 1.6**
/// </summary>
public class Tool_BatchItemSOModifier : EditorWindow
{
    #region 字段

    private Vector2 scrollPos;
    private Vector2 soListScrollPos;
    
    // 选中的 SO 列表
    private List<ItemData> selectedItems = new List<ItemData>();
    
    // 检测到的主要类型（用于显示专属字段）
    private System.Type detectedType = null;

    // === 通用属性修改标记 ===
    private bool modifyBuyPrice = false;
    private int newBuyPrice = 0;
    
    private bool modifySellPrice = false;
    private int newSellPrice = 0;
    
    private bool modifyMaxStack = false;
    private int newMaxStack = 99;
    
    private bool modifyDescription = false;
    private string newDescription = "";
    
    private bool modifyCanBeDiscarded = false;
    private bool newCanBeDiscarded = true;
    
    private bool modifyIsQuestItem = false;
    private bool newIsQuestItem = false;
    
    // === 清除 bagSprite 选项 ===
    private bool clearBagSprite = false;

    // === 工具专属修改标记 ===
    private bool modifyToolType = false;
    private ToolType newToolType = ToolType.Axe;
    
    private bool modifyEnergyCost = false;
    private int newEnergyCost = 2;
    
    private bool modifyEffectRadius = false;
    private int newEffectRadius = 1;
    
    private bool modifyEfficiencyMult = false;
    private float newEfficiencyMult = 1.0f;
    
    private bool modifyAnimFrameCount = false;
    private int newAnimFrameCount = 8;
    
    private bool modifyAnimActionType = false;
    private AnimActionType newAnimActionType = AnimActionType.Slice;

    // === 武器专属修改标记 ===
    private bool modifyWeaponType = false;
    private WeaponType newWeaponType = WeaponType.Sword;
    
    private bool modifyAttackPower = false;
    private int newAttackPower = 10;
    
    private bool modifyAttackSpeed = false;
    private float newAttackSpeed = 1.0f;
    
    private bool modifyCritChance = false;
    private float newCritChance = 5f;
    
    private bool modifyKnockback = false;
    private float newKnockback = 2f;

    #endregion

    [MenuItem("Tools/📝 批量修改物品 SO")]
    public static void ShowWindow()
    {
        var window = GetWindow<Tool_BatchItemSOModifier>("批量修改物品SO");
        window.minSize = new Vector2(480, 600);
        window.Show();
    }

    private void OnEnable()
    {
        RefreshSelection();
        Selection.selectionChanged += OnSelectionChanged;
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= OnSelectionChanged;
    }

    private void OnSelectionChanged()
    {
        RefreshSelection();
        Repaint();
    }

    /// <summary>
    /// 刷新选中的 SO 列表
    /// **Property 1: SO 类型识别正确性**
    /// </summary>
    private void RefreshSelection()
    {
        selectedItems.Clear();
        detectedType = null;
        
        foreach (var obj in Selection.objects)
        {
            if (obj is ItemData item)
            {
                if (!selectedItems.Contains(item))
                    selectedItems.Add(item);
            }
        }
        
        // 检测主要类型
        if (selectedItems.Count > 0)
        {
            // 统计各类型数量
            var typeCounts = selectedItems
                .GroupBy(i => i.GetType())
                .OrderByDescending(g => g.Count())
                .ToList();
            
            detectedType = typeCounts.First().Key;
        }
        
        // 按名称排序
        selectedItems = selectedItems.OrderBy(i => i.itemName).ToList();
    }

    private void OnGUI()
    {
        DrawHeader();
        
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        DrawSOSelection();
        DrawLine();
        DrawCommonModifyFields();
        DrawLine();
        DrawTypeSpecificFields();
        DrawLine();
        DrawApplyButton();
        
        EditorGUILayout.EndScrollView();
    }

    #region UI 绘制

    private void DrawHeader()
    {
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("📝 批量修改物品 SO", style, GUILayout.Height(30));
    }

    private void DrawSOSelection()
    {
        EditorGUILayout.LabelField("🖼️ 选中的 SO（自动跟随 Project 选择）", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        if (selectedItems.Count == 0)
        {
            EditorGUILayout.LabelField("⚠️ 请在 Project 窗口选择 ItemData 资产", EditorStyles.miniLabel);
        }
        else
        {
            string typeInfo = detectedType != null ? $"（主要类型: {detectedType.Name}）" : "";
            EditorGUILayout.LabelField($"✓ 已选择 {selectedItems.Count} 个 SO {typeInfo}", EditorStyles.boldLabel);
        }
        
        if (GUILayout.Button("🔄 刷新", GUILayout.Width(60)))
        {
            RefreshSelection();
        }
        EditorGUILayout.EndHorizontal();

        // 显示选中的 SO 列表
        if (selectedItems.Count > 0)
        {
            soListScrollPos = EditorGUILayout.BeginScrollView(soListScrollPos, 
                EditorStyles.helpBox, GUILayout.Height(Mathf.Min(selectedItems.Count * 22 + 10, 150)));
            
            foreach (var item in selectedItems)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"[{item.itemID}] {item.itemName}", GUILayout.Width(200));
                EditorGUILayout.LabelField($"({item.GetType().Name})", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawCommonModifyFields()
    {
        EditorGUILayout.LabelField("⚙️ 通用属性（勾选才修改）", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("只有勾选的参数会被修改，未勾选的保持原值不变", MessageType.Info);
        
        // 价格
        DrawModifyInt(ref modifyBuyPrice, ref newBuyPrice, "购买价格", 0, 99999);
        DrawModifyInt(ref modifySellPrice, ref newSellPrice, "出售价格", 0, 99999);
        
        // 堆叠
        DrawModifyInt(ref modifyMaxStack, ref newMaxStack, "最大堆叠数", 1, 999);
        
        // 描述
        EditorGUILayout.BeginHorizontal();
        modifyDescription = EditorGUILayout.Toggle(modifyDescription, GUILayout.Width(20));
        EditorGUI.BeginDisabledGroup(!modifyDescription);
        EditorGUILayout.LabelField("描述", GUILayout.Width(80));
        newDescription = EditorGUILayout.TextField(newDescription);
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
        
        // 功能标记
        DrawModifyBool(ref modifyCanBeDiscarded, ref newCanBeDiscarded, "可丢弃");
        DrawModifyBool(ref modifyIsQuestItem, ref newIsQuestItem, "任务物品");
        
        // 清除 bagSprite（背包图标现在使用 icon + 旋转）
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        clearBagSprite = EditorGUILayout.Toggle(clearBagSprite, GUILayout.Width(20));
        EditorGUILayout.LabelField("清除 bagSprite（使用 icon + 45° 旋转）", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawTypeSpecificFields()
    {
        if (detectedType == null) return;
        
        // 工具专属
        if (detectedType == typeof(ToolData) || selectedItems.Any(i => i is ToolData))
        {
            DrawToolModifyFields();
        }
        
        // 武器专属
        if (detectedType == typeof(WeaponData) || selectedItems.Any(i => i is WeaponData))
        {
            DrawWeaponModifyFields();
        }
    }

    private void DrawToolModifyFields()
    {
        EditorGUILayout.LabelField("🔧 工具专属（检测到 ToolData）", EditorStyles.boldLabel);
        
        // 工具类型
        EditorGUILayout.BeginHorizontal();
        modifyToolType = EditorGUILayout.Toggle(modifyToolType, GUILayout.Width(20));
        EditorGUI.BeginDisabledGroup(!modifyToolType);
        newToolType = (ToolType)EditorGUILayout.EnumPopup("工具类型", newToolType);
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
        
        DrawModifyInt(ref modifyEnergyCost, ref newEnergyCost, "精力消耗", 1, 20);
        DrawModifyInt(ref modifyEffectRadius, ref newEffectRadius, "作用范围", 1, 5);
        DrawModifyFloat(ref modifyEfficiencyMult, ref newEfficiencyMult, "效率倍率", 0.5f, 5f);
        DrawModifyInt(ref modifyAnimFrameCount, ref newAnimFrameCount, "动画帧数", 1, 30);
        
        // 动画动作类型
        EditorGUILayout.BeginHorizontal();
        modifyAnimActionType = EditorGUILayout.Toggle(modifyAnimActionType, GUILayout.Width(20));
        EditorGUI.BeginDisabledGroup(!modifyAnimActionType);
        newAnimActionType = (AnimActionType)EditorGUILayout.EnumPopup("动画动作", newAnimActionType);
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawWeaponModifyFields()
    {
        EditorGUILayout.LabelField("⚔️ 武器专属（检测到 WeaponData）", EditorStyles.boldLabel);
        
        // 武器类型
        EditorGUILayout.BeginHorizontal();
        modifyWeaponType = EditorGUILayout.Toggle(modifyWeaponType, GUILayout.Width(20));
        EditorGUI.BeginDisabledGroup(!modifyWeaponType);
        newWeaponType = (WeaponType)EditorGUILayout.EnumPopup("武器类型", newWeaponType);
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
        
        DrawModifyInt(ref modifyAttackPower, ref newAttackPower, "攻击力", 1, 200);
        DrawModifyFloat(ref modifyAttackSpeed, ref newAttackSpeed, "攻击速度", 0.3f, 3f);
        DrawModifyFloat(ref modifyCritChance, ref newCritChance, "暴击率 (%)", 0f, 100f);
        DrawModifyFloat(ref modifyKnockback, ref newKnockback, "击退力度", 0f, 10f);
    }

    private void DrawApplyButton()
    {
        EditorGUILayout.Space(10);
        
        // 统计要修改的字段数
        int modifyCount = CountModifyFlags();
        
        GUI.enabled = selectedItems.Count > 0 && modifyCount > 0;
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
        
        if (GUILayout.Button($"🚀 应用修改到 {selectedItems.Count} 个 SO（{modifyCount} 个字段）", GUILayout.Height(45)))
        {
            ApplyModifications();
        }
        
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;
        
        if (selectedItems.Count == 0)
        {
            EditorGUILayout.HelpBox("请先在 Project 窗口选择 ItemData 资产", MessageType.Warning);
        }
        else if (modifyCount == 0)
        {
            EditorGUILayout.HelpBox("请至少勾选一个要修改的字段", MessageType.Warning);
        }
    }

    #endregion

    #region 辅助方法

    private void DrawModifyInt(ref bool enabled, ref int value, string label, int min, int max)
    {
        EditorGUILayout.BeginHorizontal();
        enabled = EditorGUILayout.Toggle(enabled, GUILayout.Width(20));
        EditorGUI.BeginDisabledGroup(!enabled);
        value = EditorGUILayout.IntSlider(label, value, min, max);
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawModifyFloat(ref bool enabled, ref float value, string label, float min, float max)
    {
        EditorGUILayout.BeginHorizontal();
        enabled = EditorGUILayout.Toggle(enabled, GUILayout.Width(20));
        EditorGUI.BeginDisabledGroup(!enabled);
        value = EditorGUILayout.Slider(label, value, min, max);
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawModifyBool(ref bool enabled, ref bool value, string label)
    {
        EditorGUILayout.BeginHorizontal();
        enabled = EditorGUILayout.Toggle(enabled, GUILayout.Width(20));
        EditorGUI.BeginDisabledGroup(!enabled);
        value = EditorGUILayout.Toggle(label, value);
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawLine()
    {
        EditorGUILayout.Space(5);
        Rect rect = EditorGUILayout.GetControlRect(false, 2);
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        EditorGUILayout.Space(5);
    }

    private int CountModifyFlags()
    {
        int count = 0;
        // 通用
        if (modifyBuyPrice) count++;
        if (modifySellPrice) count++;
        if (modifyMaxStack) count++;
        if (modifyDescription) count++;
        if (modifyCanBeDiscarded) count++;
        if (modifyIsQuestItem) count++;
        // 工具
        if (modifyToolType) count++;
        if (modifyEnergyCost) count++;
        if (modifyEffectRadius) count++;
        if (modifyEfficiencyMult) count++;
        if (modifyAnimFrameCount) count++;
        if (modifyAnimActionType) count++;
        // 武器
        if (modifyWeaponType) count++;
        if (modifyAttackPower) count++;
        if (modifyAttackSpeed) count++;
        if (modifyCritChance) count++;
        if (modifyKnockback) count++;
        // 清除 bagSprite
        if (clearBagSprite) count++;
        return count;
    }

    #endregion

    #region 应用修改

    /// <summary>
    /// 应用修改到所有选中的 SO
    /// **Property 2: 参数修改隔离性**
    /// *For any* SO 资产和修改标记集合，应用修改后，只有标记为 enabled=true 的字段值发生变化
    /// </summary>
    private void ApplyModifications()
    {
        if (selectedItems.Count == 0) return;
        
        int modifiedCount = 0;
        
        foreach (var item in selectedItems)
        {
            bool modified = false;
            
            // 通用属性
            if (modifyBuyPrice) { item.buyPrice = newBuyPrice; modified = true; }
            if (modifySellPrice) { item.sellPrice = newSellPrice; modified = true; }
            if (modifyMaxStack) { item.maxStackSize = newMaxStack; modified = true; }
            if (modifyDescription) { item.description = newDescription; modified = true; }
            if (modifyCanBeDiscarded) { item.canBeDiscarded = newCanBeDiscarded; modified = true; }
            if (modifyIsQuestItem) { item.isQuestItem = newIsQuestItem; modified = true; }
            
            // 清除 bagSprite（背包图标现在使用 icon + 45° 旋转）
            if (clearBagSprite && item.bagSprite != null)
            {
                item.bagSprite = null;
                modified = true;
                Debug.Log($"<color=yellow>[批量修改] 清除 bagSprite: {item.itemName}</color>");
            }
            
            // 工具专属
            if (item is ToolData tool)
            {
                if (modifyToolType) { tool.toolType = newToolType; modified = true; }
                if (modifyEnergyCost) { tool.energyCost = newEnergyCost; modified = true; }
                if (modifyEffectRadius) { tool.effectRadius = newEffectRadius; modified = true; }
                if (modifyEfficiencyMult) { tool.efficiencyMultiplier = newEfficiencyMult; modified = true; }
                if (modifyAnimFrameCount) { tool.animationFrameCount = newAnimFrameCount; modified = true; }
                if (modifyAnimActionType) { tool.animActionType = newAnimActionType; modified = true; }
            }
            
            // 武器专属
            if (item is WeaponData weapon)
            {
                if (modifyWeaponType) { weapon.weaponType = newWeaponType; modified = true; }
                if (modifyAttackPower) { weapon.attackPower = newAttackPower; modified = true; }
                if (modifyAttackSpeed) { weapon.attackSpeed = newAttackSpeed; modified = true; }
                if (modifyCritChance) { weapon.criticalChance = newCritChance; modified = true; }
                if (modifyKnockback) { weapon.knockbackForce = newKnockback; modified = true; }
            }
            
            if (modified)
            {
                EditorUtility.SetDirty(item);
                modifiedCount++;
                Debug.Log($"<color=cyan>[批量修改] 已修改: {item.itemName}</color>");
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        // 自动同步数据库
        string syncMessage = "";
        if (modifiedCount > 0 && DatabaseSyncHelper.DatabaseExists())
        {
            int syncCount = DatabaseSyncHelper.AutoCollectAllItems();
            if (syncCount >= 0)
            {
                syncMessage = $"\n\n✅ 数据库已自动同步（共 {syncCount} 个物品）";
            }
        }
        
        EditorUtility.DisplayDialog("完成",
            $"成功修改 {modifiedCount} 个 SO{syncMessage}", "确定");
        
        Debug.Log($"<color=green>[批量修改] ✅ 完成！共修改 {modifiedCount} 个物品</color>");
    }

    #endregion
}
