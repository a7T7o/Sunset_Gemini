using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using FarmGame.Data;

/// <summary>
/// 批量创建 ScriptableObject（物品数据）工具
/// - 支持从选中的 Sprite 批量创建各类型 ItemData（Tool/Seed/Crop/Food/Weapon/Material/Potion）
/// - 支持在窗口内对 Sprite 顺序进行二次调整（上下移动）
/// - 支持并行文本框输入：按行填写 ID/名称，自动与 Sprite 顺序一一对应
/// - 若仅填首个 ID，后续自动按 +1 递增
/// - 文件命名规范：{类型}_{id}_{物品名称}.asset，按类型放入对应 Data 目录
/// - 类型专属属性：统一在一个区域填写，批量应用到创建的所有资产
/// - 复用本项目编辑器工具的 UI 风格
/// </summary>
public class ItemSOBatchCreator : EditorWindow
{
    private enum SoType { Tool, Seed, Crop, Food, Weapon, Material, Potion }

    // 选中的 Sprite 列表（可在窗口内调整顺序）
    private List<Sprite> sprites = new List<Sprite>();
    private Vector2 scroll;
    private Vector2 idsScroll;
    private Vector2 namesScroll;

    // 基础配置
    private SoType createType = SoType.Tool;
    private string saveFolderOverride = ""; // 若为空则使用内置默认路径

    // 并行输入：ID/名称（行数与 sprites 对齐）
    private string inputIds = "";
    private string inputNames = "";

    // 通用字段（ItemData）
    private string commonDescription = "";
    private int commonBuyPrice = 0;
    private int commonSellPrice = 0;
    private int commonMaxStack = 99;
    private bool commonDiscardable = true;
    private bool commonIsQuest = false;
    // baseQuality 只适用于 Crop/Food/Potion，在各自的专属字段区域设置

    // ToolData 专属
    private ToolType tool_toolType = ToolType.Hoe;
    private int tool_energyCost = 2;
    private int tool_effectRadius = 1;
    private float tool_efficiencyMult = 1.0f;
    private bool tool_hasDurability = false;
    private int tool_maxDurability = 100;
    private AudioClip tool_useSound = null;
    // 动画配置（动画ID直接使用itemID，不需要单独字段）
    private int tool_animFrameCount = 8;  // 动画帧数
    private AnimActionType tool_animActionType = AnimActionType.Slice;  // 动画动作类型

    // WeaponData 专属
    // 注意：武器没有等级属性，品质通过后缀命名区分
    private WeaponType weapon_type = WeaponType.Sword;
    private int weapon_attackPower = 10;
    private float weapon_attackSpeed = 1.0f;
    private float weapon_critChance = 5f;
    private float weapon_critMult = 2.0f;
    private float weapon_attackRange = 1.5f;
    private float weapon_knockback = 2f;
    private int weapon_energyCostPerAttack = 1;
    private bool weapon_hasDurability = false;
    private int weapon_maxDurability = 200;
    private RuntimeAnimatorController weapon_animatorController = null;
    private int weapon_animationFrameCount = 8;
    private AnimActionType weapon_animActionType = AnimActionType.Pierce;
    private AudioClip weapon_attackSound = null;
    private AudioClip weapon_hitSound = null;
    // 注意：每个品质的武器都是独立 ItemID，动画直接使用 itemID

    // SeedData 专属
    private int seed_growthDays = 4;
    private Season seed_season = Season.Spring;
    private int seed_harvestCropId = 1101;
    private Vector2Int seed_harvestAmountRange = new Vector2Int(1, 1);
    private bool seed_isReHarvestable = false;
    private int seed_reHarvestDays = 2;
    private int seed_maxHarvestCount = 0;

    // CropData 专属
    private int crop_seedId = 1001;
    private int crop_harvestExp = 10;
    private bool crop_canBeCrafted = true;
    private string crop_usedInRecipes = "";
    private string crop_qualityInfo = "收获时随机判定品质，外观不变，UI显示星星";

    // FoodData 专属
    private int food_energyRestore = 30;
    private int food_healthRestore = 15;
    private float food_consumeTime = 1.0f;
    private BuffType food_buffType = BuffType.None;
    private float food_buffValue = 0f;
    private float food_buffDuration = 0f;
    private int food_recipeId = 0;

    // MaterialData 专属
    private MaterialSubType mat_subType = MaterialSubType.Natural;
    private string mat_source = "";
    private bool mat_canSmelt = false;
    private int mat_smeltResultId = 0;
    private int mat_smeltTime = 5;
    private string mat_craftingUse = "";

    // PotionData 专属
    private int potion_healthRestore = 50;
    private int potion_energyRestore = 0;
    private float potion_useTime = 0.5f;
    private BuffType potion_buffType = BuffType.None;
    private float potion_buffValue = 0f;
    private float potion_buffDuration = 300f;
    private int potion_recipeId = 0;
    private GameObject potion_useEffectPrefab = null;
    private AudioClip potion_useSound = null;

    [MenuItem("Farm/Items/批量创建物品数据 (SO)")]
    private static void ShowWindow()
    {
        var win = GetWindow<ItemSOBatchCreator>("批量创建物品数据");
        win.minSize = new Vector2(620, 720);
        win.Show();
    }

    private void OnEnable()
    {
        LoadSelectedSprites();
    }

    private void OnSelectionChange()
    {
        LoadSelectedSprites();
        Repaint();
    }

    private void LoadSelectedSprites()
    {
        sprites.Clear();
        foreach (var obj in Selection.objects)
        {
            if (obj is Sprite s) sprites.Add(s);
            else if (obj is Texture2D)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                foreach (var a in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (a is Sprite sub) sprites.Add(sub);
                }
            }
        }
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        DrawHeader();
        DrawSpriteList();
        DrawBasicSetup();
        DrawCommonItemFields();
        DrawTypeSpecificFields();
        DrawCreateArea();

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("🧰 批量创建 ScriptableObject（物品）", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("选择一个或多个 Sprite，按顺序批量生成各类 ItemData。可在窗口中调整顺序，并行输入 ID/名称。", MessageType.Info);
        EditorGUILayout.Space(6);
    }

    private void DrawSpriteList()
    {
        EditorGUILayout.LabelField("📦 选中的 Sprite（可调整顺序）", EditorStyles.boldLabel);

        if (sprites.Count == 0)
        {
            EditorGUILayout.HelpBox("请在 Project 中选择至少一个 Sprite 或 SpriteSheet。", MessageType.Warning);
            return;
        }

        int removeIndex = -1;
        for (int i = 0; i < sprites.Count; i++)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"{i + 1:00}. {sprites[i].name}");
            if (GUILayout.Button("▲", GUILayout.Width(28)))
            {
                if (i > 0) SwapSprites(i, i - 1);
            }
            if (GUILayout.Button("▼", GUILayout.Width(28)))
            {
                if (i < sprites.Count - 1) SwapSprites(i, i + 1);
            }
            if (GUILayout.Button("✖", GUILayout.Width(28)))
            {
                removeIndex = i;
            }
            EditorGUILayout.EndHorizontal();
        }
        if (removeIndex >= 0)
        {
            sprites.RemoveAt(removeIndex);
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField($"总计：{sprites.Count} 个 Sprite", EditorStyles.miniBoldLabel);
        EditorGUILayout.Space(6);
    }

    private void SwapSprites(int a, int b)
    {
        var tmp = sprites[a];
        sprites[a] = sprites[b];
        sprites[b] = tmp;
    }

    private void DrawBasicSetup()
    {
        EditorGUILayout.LabelField("⚙️ 基本设置", EditorStyles.boldLabel);

        createType = (SoType)EditorGUILayout.EnumPopup("创建类型", createType);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("保存目录", GUILayout.Width(80));
            EditorGUILayout.SelectableLabel(GetSaveFolderPreview(), GUILayout.Height(18));
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string basePath = Application.dataPath;
                string picked = EditorUtility.OpenFolderPanel("选择保存目录(建议在Assets内)", basePath, "");
                if (!string.IsNullOrEmpty(picked))
                {
                    if (picked.StartsWith(Application.dataPath))
                    {
                        saveFolderOverride = "Assets" + picked.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("提示", "请在项目 Assets 目录内选择路径。", "确定");
                    }
                }
            }
        }

        EditorGUILayout.Space(6);

        EditorGUILayout.LabelField("🧾 并行输入（与 Sprite 顺序一一对应）", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("逐行填写。若只填首个 ID，则后续自动按 +1 递增。若名称留空，将默认使用 Sprite 名。", MessageType.None);

        EditorGUILayout.LabelField($"ID（{sprites.Count} 行）");
        idsScroll = EditorGUILayout.BeginScrollView(idsScroll, GUILayout.Height(100));
        inputIds = EditorGUILayout.TextArea(inputIds, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        EditorGUILayout.LabelField($"名称（{sprites.Count} 行）");
        namesScroll = EditorGUILayout.BeginScrollView(namesScroll, GUILayout.Height(100));
        inputNames = EditorGUILayout.TextArea(inputNames, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("从Sprite名填充名称"))
            {
                inputNames = string.Join("\n", sprites.Select(s => s != null ? s.name : ""));
            }
            if (GUILayout.Button("清空输入", GUILayout.Width(80)))
            {
                inputIds = ""; inputNames = "";
            }
        }

        EditorGUILayout.Space(6);
    }

    private string GetSaveFolderPreview()
    {
        if (!string.IsNullOrEmpty(saveFolderOverride)) return saveFolderOverride;
        return GetDefaultFolderForType(createType);
    }

    private string GetDefaultFolderForType(SoType t)
    {
        switch (t)
        {
            case SoType.Tool: return "Assets/111_Data/Items/Tools";
            case SoType.Seed: return "Assets/111_Data/Items/Seeds";
            case SoType.Crop: return "Assets/111_Data/Items/Crops";
            case SoType.Food: return "Assets/111_Data/Items/Foods";
            case SoType.Weapon: return "Assets/111_Data/Items/Weapons";
            case SoType.Material: return "Assets/111_Data/Items/Materials";
            case SoType.Potion: return "Assets/111_Data/Items/Potions";
        }
        return "Assets/111_Data/Items";
    }

    private void DrawCommonItemFields()
    {
        EditorGUILayout.LabelField("📚 通用字段（ItemData）", EditorStyles.boldLabel);
        commonDescription = EditorGUILayout.TextField("描述", commonDescription);
        commonBuyPrice = EditorGUILayout.IntField("Buy Price", commonBuyPrice);
        commonSellPrice = EditorGUILayout.IntField("Sell Price", commonSellPrice);
        commonMaxStack = EditorGUILayout.IntSlider("Max Stack Size", commonMaxStack, 1, 999);
        commonDiscardable = EditorGUILayout.Toggle("Can Be Discarded", commonDiscardable);
        commonIsQuest = EditorGUILayout.Toggle("Is Quest Item", commonIsQuest);
        // baseQuality 只适用于 Crop/Food/Potion，在各自的专属字段区域设置
        EditorGUILayout.Space(6);
    }

    private void DrawTypeSpecificFields()
    {
        EditorGUILayout.LabelField("🔧 类型专属属性（批量共享）", EditorStyles.boldLabel);

        switch (createType)
        {
            case SoType.Tool:
                tool_toolType = (ToolType)EditorGUILayout.EnumPopup("Tool Type", tool_toolType);
                tool_energyCost = EditorGUILayout.IntSlider("Energy Cost", tool_energyCost, 1, 20);
                tool_effectRadius = EditorGUILayout.IntSlider("Effect Radius", tool_effectRadius, 1, 5);
                tool_efficiencyMult = EditorGUILayout.Slider("Efficiency Multiplier", tool_efficiencyMult, 0.5f, 5f);
                tool_hasDurability = EditorGUILayout.Toggle("Has Durability", tool_hasDurability);
                tool_maxDurability = EditorGUILayout.IntField("Max Durability", tool_maxDurability);
                tool_useSound = (AudioClip)EditorGUILayout.ObjectField("Use Sound", tool_useSound, typeof(AudioClip), false);
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("动画配置", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("动画状态名格式: {Action}_{Dir}_Clip_{itemID}_{quality}\n动画ID直接使用物品的itemID，同一工具不同品质使用相同ID", MessageType.Info);
                tool_animFrameCount = EditorGUILayout.IntSlider("动画帧数", tool_animFrameCount, 1, 30);
                tool_animActionType = (AnimActionType)EditorGUILayout.EnumPopup("动画动作类型", tool_animActionType);
                break;

            case SoType.Weapon:
                weapon_type = (WeaponType)EditorGUILayout.EnumPopup("Weapon Type", weapon_type);
                EditorGUILayout.HelpBox("武器没有等级属性，品质通过后缀命名区分", MessageType.Info);
                weapon_attackPower = EditorGUILayout.IntSlider("Attack Power", weapon_attackPower, 1, 200);
                weapon_attackSpeed = EditorGUILayout.Slider("Attack Speed", weapon_attackSpeed, 0.3f, 3f);
                weapon_critChance = EditorGUILayout.Slider("Critical Chance %", weapon_critChance, 0, 100);
                weapon_critMult = EditorGUILayout.Slider("Critical Damage Mult", weapon_critMult, 1.5f, 3f);
                weapon_attackRange = EditorGUILayout.FloatField("Attack Range", weapon_attackRange);
                weapon_knockback = EditorGUILayout.Slider("Knockback Force", weapon_knockback, 0, 10);
                weapon_energyCostPerAttack = EditorGUILayout.IntSlider("Energy Cost/Attack", weapon_energyCostPerAttack, 0, 10);
                weapon_hasDurability = EditorGUILayout.Toggle("Has Durability", weapon_hasDurability);
                weapon_maxDurability = EditorGUILayout.IntField("Max Durability", weapon_maxDurability);
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("=== 动画配置 ===", EditorStyles.boldLabel);
                weapon_animatorController = (RuntimeAnimatorController)EditorGUILayout.ObjectField("Animator Controller", weapon_animatorController, typeof(RuntimeAnimatorController), false);
                weapon_animationFrameCount = EditorGUILayout.IntSlider("Animation Frame Count", weapon_animationFrameCount, 1, 30);
                weapon_animActionType = (AnimActionType)EditorGUILayout.EnumPopup("Anim Action Type", weapon_animActionType);
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("=== 音效 ===", EditorStyles.boldLabel);
                weapon_attackSound = (AudioClip)EditorGUILayout.ObjectField("Attack Sound", weapon_attackSound, typeof(AudioClip), false);
                weapon_hitSound = (AudioClip)EditorGUILayout.ObjectField("Hit Sound", weapon_hitSound, typeof(AudioClip), false);
                // 注意：Quality ID Mapping 已移除，每个品质的武器都是独立 ItemID
                break;

            case SoType.Seed:
                seed_growthDays = EditorGUILayout.IntSlider("Growth Days", seed_growthDays, 1, 28);
                seed_season = (Season)EditorGUILayout.EnumPopup("Season", seed_season);
                seed_harvestCropId = EditorGUILayout.IntField("Harvest Crop ID", seed_harvestCropId);
                seed_harvestAmountRange = EditorGUILayout.Vector2IntField("Harvest Amount Range", seed_harvestAmountRange);
                seed_isReHarvestable = EditorGUILayout.Toggle("Re-Harvestable", seed_isReHarvestable);
                seed_reHarvestDays = EditorGUILayout.IntSlider("Re-Harvest Days", seed_reHarvestDays, 1, 14);
                seed_maxHarvestCount = EditorGUILayout.IntField("Max Harvest Count (0=∞)", seed_maxHarvestCount);
                break;

            case SoType.Crop:
                crop_seedId = EditorGUILayout.IntField("Seed ID", crop_seedId);
                crop_harvestExp = EditorGUILayout.IntField("Harvest Exp", crop_harvestExp);
                crop_canBeCrafted = EditorGUILayout.Toggle("Can Be Crafted", crop_canBeCrafted);
                crop_usedInRecipes = EditorGUILayout.TextField("Used In Recipes", crop_usedInRecipes);
                crop_qualityInfo = EditorGUILayout.TextField("Quality Info", crop_qualityInfo);
                break;

            case SoType.Food:
                food_energyRestore = EditorGUILayout.IntField("Energy Restore", food_energyRestore);
                food_healthRestore = EditorGUILayout.IntField("Health Restore", food_healthRestore);
                food_consumeTime = EditorGUILayout.FloatField("Consume Time", food_consumeTime);
                food_buffType = (BuffType)EditorGUILayout.EnumPopup("Buff Type", food_buffType);
                food_buffValue = EditorGUILayout.FloatField("Buff Value", food_buffValue);
                food_buffDuration = EditorGUILayout.FloatField("Buff Duration", food_buffDuration);
                food_recipeId = EditorGUILayout.IntField("Recipe ID", food_recipeId);
                break;

            case SoType.Material:
                mat_subType = (MaterialSubType)EditorGUILayout.EnumPopup("Material SubType", mat_subType);
                mat_source = EditorGUILayout.TextField("Source Description", mat_source);
                mat_canSmelt = EditorGUILayout.Toggle("Can Be Smelt", mat_canSmelt);
                mat_smeltResultId = EditorGUILayout.IntField("Smelt Result ID", mat_smeltResultId);
                mat_smeltTime = EditorGUILayout.IntField("Smelt Time (hrs)", mat_smeltTime);
                mat_craftingUse = EditorGUILayout.TextField("Crafting Use", mat_craftingUse);
                break;

            case SoType.Potion:
                potion_healthRestore = EditorGUILayout.IntField("Health Restore", potion_healthRestore);
                potion_energyRestore = EditorGUILayout.IntField("Energy Restore", potion_energyRestore);
                potion_useTime = EditorGUILayout.FloatField("Use Time", potion_useTime);
                potion_buffType = (BuffType)EditorGUILayout.EnumPopup("Buff Type", potion_buffType);
                potion_buffValue = EditorGUILayout.FloatField("Buff Value", potion_buffValue);
                potion_buffDuration = EditorGUILayout.FloatField("Buff Duration", potion_buffDuration);
                potion_recipeId = EditorGUILayout.IntField("Recipe ID", potion_recipeId);
                potion_useEffectPrefab = (GameObject)EditorGUILayout.ObjectField("Use Effect Prefab", potion_useEffectPrefab, typeof(GameObject), false);
                potion_useSound = (AudioClip)EditorGUILayout.ObjectField("Use Sound", potion_useSound, typeof(AudioClip), false);
                break;
        }

        EditorGUILayout.Space(6);
    }

    private void DrawCreateArea()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUI.enabled = sprites.Count > 0;
        if (GUILayout.Button("🚀 批量创建", GUILayout.Height(40), GUILayout.Width(200)))
        {
            CreateAssets();
        }
        GUI.enabled = true;
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(8);
    }

    private void CreateAssets()
    {
        if (sprites.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先选择至少一个 Sprite。", "确定");
            return;
        }

        // 解析 ID/名称
        var ids = ParseIds(sprites.Count, inputIds);
        var names = ParseNames(sprites.Count, inputNames, sprites);

        // 基本校验
        if (ids.Length != sprites.Count || names.Length != sprites.Count)
        {
            EditorUtility.DisplayDialog("错误", "ID 或 名称解析失败。", "确定");
            return;
        }

        string folder = string.IsNullOrEmpty(saveFolderOverride) ? GetDefaultFolderForType(createType) : saveFolderOverride;
        EnsureFolder(folder);

        int success = 0, skip = 0;
        for (int i = 0; i < sprites.Count; i++)
        {
            string typePrefix = GetTypePrefix(createType);
            string safeName = SanitizeFileName(names[i]);
            string assetPath = Path.Combine(folder, $"{typePrefix}_{ids[i]}_{safeName}.asset");
            assetPath = assetPath.Replace("\\", "/");

            if (File.Exists(assetPath))
            {
                Debug.LogWarning($"[跳过] 目标已存在: {assetPath}");
                skip++;
                continue;
            }

            bool ok = CreateSingleAsset(createType, assetPath, ids[i], names[i], sprites[i]);
            if (ok) success++; else skip++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 自动同步数据库
        string syncMessage = "";
        if (success > 0)
        {
            if (DatabaseSyncHelper.DatabaseExists())
            {
                int syncCount = DatabaseSyncHelper.AutoCollectAllItems();
                if (syncCount >= 0)
                {
                    syncMessage = $"\n\n✅ 数据库已自动同步（共 {syncCount} 个物品）";
                }
                else
                {
                    syncMessage = "\n\n⚠️ 数据库同步失败，请手动执行";
                }
            }
            else
            {
                syncMessage = "\n\n⚠️ 数据库不存在，请先创建 MasterItemDatabase";
            }
        }

        EditorUtility.DisplayDialog("完成", $"✅ 创建完成：成功 {success}，跳过 {skip}\n保存目录：{folder}{syncMessage}", "确定");
        Debug.Log($"<color=green>[ItemSO] 批量创建完成：成功 {success}，跳过 {skip}</color>");
    }

    private string GetTypePrefix(SoType t)
    {
        switch (t)
        {
            case SoType.Tool: return "Tool";
            case SoType.Seed: return "Seed";
            case SoType.Crop: return "Crop";
            case SoType.Food: return "Food";
            case SoType.Weapon: return "Weapon";
            case SoType.Material: return "Material";
            case SoType.Potion: return "Potion";
        }
        return "Item";
    }

    private bool CreateSingleAsset(SoType t, string assetPath, int id, string itemName, Sprite icon)
    {
        try
        {
            switch (t)
            {
                case SoType.Tool:
                {
                    var so = ScriptableObject.CreateInstance<ToolData>();
                    FillCommon(so, id, itemName, icon, ItemCategory.Tool);
                    so.maxStackSize = 1; // 工具不可堆叠
                    so.toolType = tool_toolType;
                    so.energyCost = tool_energyCost;
                    so.effectRadius = tool_effectRadius;
                    so.efficiencyMultiplier = tool_efficiencyMult;
                    so.hasDurability = tool_hasDurability;
                    so.maxDurability = tool_maxDurability;
                    so.useSound = tool_useSound;
                    // 动画ID直接使用itemID，不需要单独设置
                    so.animationFrameCount = tool_animFrameCount;
                    so.animActionType = tool_animActionType;
                    AssetDatabase.CreateAsset(so, assetPath);
                    break;
                }
                case SoType.Weapon:
                {
                    var so = ScriptableObject.CreateInstance<WeaponData>();
                    FillCommon(so, id, itemName, icon, ItemCategory.Tool); // 现有设计：武器也归 Tool 大类
                    so.maxStackSize = 1;
                    so.weaponType = weapon_type;
                    // 武器没有等级属性，品质通过后缀命名区分
                    so.attackPower = weapon_attackPower;
                    so.attackSpeed = weapon_attackSpeed;
                    so.criticalChance = weapon_critChance;
                    so.criticalDamageMultiplier = weapon_critMult;
                    so.attackRange = weapon_attackRange;
                    so.knockbackForce = weapon_knockback;
                    so.energyCostPerAttack = weapon_energyCostPerAttack;
                    so.hasDurability = weapon_hasDurability;
                    so.maxDurability = weapon_maxDurability;
                    so.animatorController = weapon_animatorController;
                    so.animationFrameCount = weapon_animationFrameCount;
                    so.animActionType = weapon_animActionType;
                    so.attackSound = weapon_attackSound;
                    so.hitSound = weapon_hitSound;
                    // 注意：useQualityIdMapping 和 animationDefaultId 已移除
                    // 每个品质的武器都是独立 ItemID，动画直接使用 itemID
                    AssetDatabase.CreateAsset(so, assetPath);
                    break;
                }
                case SoType.Seed:
                {
                    var so = ScriptableObject.CreateInstance<SeedData>();
                    FillCommon(so, id, itemName, icon, ItemCategory.Plant);
                    so.growthDays = seed_growthDays;
                    so.season = seed_season;
                    so.harvestCropID = seed_harvestCropId;
                    so.harvestAmountRange = seed_harvestAmountRange;
                    so.isReHarvestable = seed_isReHarvestable;
                    so.reHarvestDays = seed_reHarvestDays;
                    so.maxHarvestCount = seed_maxHarvestCount;
                    AssetDatabase.CreateAsset(so, assetPath);
                    break;
                }
                case SoType.Crop:
                {
                    var so = ScriptableObject.CreateInstance<CropData>();
                    FillCommon(so, id, itemName, icon, ItemCategory.Plant);
                    so.seedID = crop_seedId;
                    so.harvestExp = crop_harvestExp;
                    so.canBeCrafted = crop_canBeCrafted;
                    so.usedInRecipes = crop_usedInRecipes;
                    so.qualityInfo = crop_qualityInfo;
                    AssetDatabase.CreateAsset(so, assetPath);
                    break;
                }
                case SoType.Food:
                {
                    var so = ScriptableObject.CreateInstance<FoodData>();
                    FillCommon(so, id, itemName, icon, ItemCategory.Food);
                    so.energyRestore = food_energyRestore;
                    so.healthRestore = food_healthRestore;
                    so.consumeTime = food_consumeTime;
                    so.buffType = food_buffType;
                    so.buffValue = food_buffValue;
                    so.buffDuration = food_buffDuration;
                    so.recipeID = food_recipeId;
                    AssetDatabase.CreateAsset(so, assetPath);
                    break;
                }
                case SoType.Material:
                {
                    var so = ScriptableObject.CreateInstance<MaterialData>();
                    FillCommon(so, id, itemName, icon, ItemCategory.Material);
                    so.materialSubType = mat_subType;
                    so.sourceDescription = mat_source;
                    so.canBeSmelt = mat_canSmelt;
                    so.smeltResultID = mat_smeltResultId;
                    so.smeltTime = mat_smeltTime;
                    so.craftingUse = mat_craftingUse;
                    AssetDatabase.CreateAsset(so, assetPath);
                    break;
                }
                case SoType.Potion:
                {
                    var so = ScriptableObject.CreateInstance<PotionData>();
                    FillCommon(so, id, itemName, icon, ItemCategory.Consumable);
                    so.healthRestore = potion_healthRestore;
                    so.energyRestore = potion_energyRestore;
                    so.useTime = potion_useTime;
                    so.buffType = potion_buffType;
                    so.buffValue = potion_buffValue;
                    so.buffDuration = potion_buffDuration;
                    so.recipeID = potion_recipeId;
                    so.useEffectPrefab = potion_useEffectPrefab;
                    so.useSound = potion_useSound;
                    AssetDatabase.CreateAsset(so, assetPath);
                    break;
                }
            }

            Debug.Log($"[创建] {assetPath}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"创建失败: {assetPath}\n{e}");
            return false;
        }
    }

    private void FillCommon(ItemData so, int id, string name, Sprite icon, ItemCategory category)
    {
        so.itemID = id;
        so.itemName = name;
        so.description = commonDescription;
        so.category = category;
        so.icon = icon;
        so.buyPrice = commonBuyPrice;
        so.sellPrice = commonSellPrice;
        so.maxStackSize = commonMaxStack;
        so.canBeDiscarded = commonDiscardable;
        so.isQuestItem = commonIsQuest;
        // baseQuality 只在特定类型（Crop/Food/Potion）中设置，不在基类中
    }

    private void EnsureFolder(string folder)
    {
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
        }
    }

    private int[] ParseIds(int count, string multiLine)
    {
        var ids = new int[count];
        var lines = string.IsNullOrEmpty(multiLine) ? new string[0] : multiLine.Replace("\r", "").Split('\n');

        bool hasFirst = false;
        for (int i = 0; i < count; i++)
        {
            int parsed;
            if (i < lines.Length && int.TryParse(lines[i].Trim(), out parsed))
            {
                ids[i] = parsed;
                if (i == 0) hasFirst = true;
            }
            else
            {
                if (i == 0)
                {
                    // 首个未填，提示
                    if (!hasFirst)
                    {
                        // 尝试使用 0 作为基准，避免中断（也可中止）
                        ids[i] = 0;
                    }
                }
                else
                {
                    // 自增
                    ids[i] = ids[i - 1] + 1;
                }
            }
        }
        return ids;
    }

    private string[] ParseNames(int count, string multiLine, List<Sprite> fromSprites)
    {
        var names = new string[count];
        var lines = string.IsNullOrEmpty(multiLine) ? new string[0] : multiLine.Replace("\r", "").Split('\n');
        for (int i = 0; i < count; i++)
        {
            if (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]))
                names[i] = lines[i].Trim();
            else
                names[i] = fromSprites[i] != null ? fromSprites[i].name : $"Item_{i}";
        }
        return names;
    }

    private string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        foreach (var c in invalid) name = name.Replace(c, '_');
        return name;
    }
}
