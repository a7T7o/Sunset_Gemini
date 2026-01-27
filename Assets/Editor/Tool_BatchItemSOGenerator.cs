using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using FarmGame.Data;

/// <summary>
/// 批量生成物品 SO 工具 V2
/// 采用大类+小类的层级分类结构
/// 
/// 大类：
/// - 工具装备：工具、武器
/// - 种植类：种子、作物
/// - 可放置：树苗、工作台、存储、交互展示、简单事件
/// - 消耗品：食物、药水
/// - 材料：矿石、锭、自然材料、怪物掉落
/// - 其他：基础物品、家具、特殊物品
/// </summary>
public class Tool_BatchItemSOGenerator : EditorWindow
{
    #region 枚举定义

    /// <summary>
    /// 物品大类
    /// </summary>
    private enum ItemMainCategory
    {
        ToolEquipment = 0,  // 工具装备
        Planting = 1,       // 种植类
        Placeable = 2,      // 可放置
        Consumable = 3,     // 消耗品
        Material = 4,       // 材料
        Other = 5           // 其他
    }

    /// <summary>
    /// 物品 SO 类型（扩展版）
    /// </summary>
    private enum ItemSOType
    {
        // 工具装备
        ToolData = 0,
        WeaponData = 1,
        KeyData = 2,        // 钥匙
        LockData = 3,       // 锁
        
        // 种植类
        SeedData = 10,
        CropData = 11,
        
        // 可放置
        SaplingData = 20,
        WorkstationData = 21,
        StorageData = 22,
        InteractiveDisplayData = 23,
        SimpleEventData = 24,
        
        // 消耗品
        FoodData = 30,
        PotionData = 31,
        
        // 材料
        MaterialData = 40,
        
        // 其他
        ItemData = 50,
        FurnitureData = 51,
        SpecialData = 52
    }

    #endregion

    #region 静态映射

    private static readonly Dictionary<ItemMainCategory, ItemSOType[]> CategoryToSubTypes = new()
    {
        { ItemMainCategory.ToolEquipment, new[] { ItemSOType.ToolData, ItemSOType.WeaponData, ItemSOType.KeyData, ItemSOType.LockData } },
        { ItemMainCategory.Planting, new[] { ItemSOType.SeedData, ItemSOType.CropData } },
        { ItemMainCategory.Placeable, new[] { ItemSOType.SaplingData, ItemSOType.WorkstationData, ItemSOType.StorageData, ItemSOType.InteractiveDisplayData, ItemSOType.SimpleEventData } },
        { ItemMainCategory.Consumable, new[] { ItemSOType.FoodData, ItemSOType.PotionData } },
        { ItemMainCategory.Material, new[] { ItemSOType.MaterialData } },
        { ItemMainCategory.Other, new[] { ItemSOType.ItemData, ItemSOType.FurnitureData, ItemSOType.SpecialData } }
    };

    private static readonly Dictionary<ItemMainCategory, string> CategoryNames = new()
    {
        { ItemMainCategory.ToolEquipment, "工具装备" },
        { ItemMainCategory.Planting, "种植类" },
        { ItemMainCategory.Placeable, "可放置" },
        { ItemMainCategory.Consumable, "消耗品" },
        { ItemMainCategory.Material, "材料" },
        { ItemMainCategory.Other, "其他" }
    };

    private static readonly Dictionary<ItemMainCategory, Color> CategoryColors = new()
    {
        { ItemMainCategory.ToolEquipment, new Color(1f, 0.8f, 0.3f) },
        { ItemMainCategory.Planting, new Color(0.5f, 0.9f, 0.5f) },
        { ItemMainCategory.Placeable, new Color(0.4f, 0.8f, 0.9f) },
        { ItemMainCategory.Consumable, new Color(1f, 0.6f, 0.8f) },
        { ItemMainCategory.Material, new Color(0.7f, 0.6f, 0.9f) },
        { ItemMainCategory.Other, new Color(0.7f, 0.7f, 0.7f) }
    };

    private static readonly Dictionary<ItemSOType, string> SubTypeNames = new()
    {
        { ItemSOType.ToolData, "工具" },
        { ItemSOType.WeaponData, "武器" },
        { ItemSOType.KeyData, "钥匙" },
        { ItemSOType.LockData, "锁" },
        { ItemSOType.SeedData, "种子" },
        { ItemSOType.CropData, "作物" },
        { ItemSOType.SaplingData, "树苗" },
        { ItemSOType.WorkstationData, "工作台" },
        { ItemSOType.StorageData, "存储" },
        { ItemSOType.InteractiveDisplayData, "交互展示" },
        { ItemSOType.SimpleEventData, "简单事件" },
        { ItemSOType.FoodData, "食物" },
        { ItemSOType.PotionData, "药水" },
        { ItemSOType.MaterialData, "材料" },
        { ItemSOType.ItemData, "基础物品" },
        { ItemSOType.FurnitureData, "家具" },
        { ItemSOType.SpecialData, "特殊物品" }
    };

    private static readonly Dictionary<ItemSOType, int> SubTypeStartIDs = new()
    {
        { ItemSOType.ToolData, 0 },
        { ItemSOType.WeaponData, 200 },
        { ItemSOType.KeyData, 1420 },
        { ItemSOType.LockData, 1410 },
        { ItemSOType.SeedData, 1000 },
        { ItemSOType.CropData, 1100 },
        { ItemSOType.SaplingData, 1200 },
        { ItemSOType.WorkstationData, 1300 },
        { ItemSOType.StorageData, 1400 },
        { ItemSOType.InteractiveDisplayData, 1500 },
        { ItemSOType.SimpleEventData, 1600 },
        { ItemSOType.FoodData, 5000 },
        { ItemSOType.PotionData, 4000 },
        { ItemSOType.MaterialData, 3200 },
        { ItemSOType.ItemData, 0 },
        { ItemSOType.FurnitureData, 6000 },
        { ItemSOType.SpecialData, 7000 }
    };

    private static readonly Dictionary<ItemSOType, string> SubTypeOutputFolders = new()
    {
        { ItemSOType.ToolData, "Assets/111_Data/Items/Tools" },
        { ItemSOType.WeaponData, "Assets/111_Data/Items/Weapons" },
        { ItemSOType.KeyData, "Assets/111_Data/Items/Keys" },
        { ItemSOType.LockData, "Assets/111_Data/Items/Locks" },
        { ItemSOType.SeedData, "Assets/111_Data/Items/Seeds" },
        { ItemSOType.CropData, "Assets/111_Data/Items/Crops" },
        { ItemSOType.SaplingData, "Assets/111_Data/Items/Placeable/Saplings" },
        { ItemSOType.WorkstationData, "Assets/111_Data/Items/Placeable/Workstations" },
        { ItemSOType.StorageData, "Assets/111_Data/Items/Placeable/Storage" },
        { ItemSOType.InteractiveDisplayData, "Assets/111_Data/Items/Placeable/Displays" },
        { ItemSOType.SimpleEventData, "Assets/111_Data/Items/Placeable/Events" },
        { ItemSOType.FoodData, "Assets/111_Data/Items/Foods" },
        { ItemSOType.PotionData, "Assets/111_Data/Items/Potions" },
        { ItemSOType.MaterialData, "Assets/111_Data/Items/Materials" },
        { ItemSOType.ItemData, "Assets/111_Data/Items" },
        { ItemSOType.FurnitureData, "Assets/111_Data/Items/Furniture" },
        { ItemSOType.SpecialData, "Assets/111_Data/Items/Special" }
    };

    #endregion

    #region 字段

    private Vector2 scrollPos;
    private Vector2 spriteListScrollPos;
    private List<Sprite> selectedSprites = new List<Sprite>();

    // === 数据库设置 ===
    private ItemDatabase databaseAsset;
    private string databasePath = "";

    // === 分类设置 ===
    private ItemMainCategory mainCategory = ItemMainCategory.Other;
    private ItemSOType soType = ItemSOType.ItemData;
    private string outputFolder = "Assets/111_Data/Items";

    // === ID 设置 ===
    private bool useSequentialID = true;
    private int startID = 0;

    // === 通用属性 ===
    private bool setPrice = false;
    private int defaultBuyPrice = 0;
    private int defaultSellPrice = 0;
    private bool setMaxStack = false;
    private int defaultMaxStack = 99;
    private bool setDisplaySize = false;
    private int displayPixelSize = 32;

    // === 工具专属 ===
    private ToolType toolType = ToolType.Axe;
    private bool setToolEnergy = false;
    private int toolEnergyCost = 2;
    private bool setToolRadius = false;
    private int toolEffectRadius = 1;
    private bool setToolAnimFrames = false;
    private int toolAnimFrameCount = 8;

    // === 武器专属 ===
    private WeaponType weaponType = WeaponType.Sword;
    private bool setWeaponAttack = false;
    private int weaponAttackPower = 10;
    private bool setWeaponSpeed = false;
    private float weaponAttackSpeed = 1.0f;
    private bool setWeaponCrit = false;
    private float weaponCritChance = 5f;

    // === 种子专属 ===
    private Season seedSeason = Season.Spring;
    private bool setSeedGrowth = false;
    private int seedGrowthDays = 4;
    private bool setSeedHarvest = false;
    private int seedHarvestCropID = 1100;

    // === 树苗专属 ===
    private GameObject saplingTreePrefab;
    private bool setSaplingExp = false;
    private int saplingPlantingExp = 5;

    // === 作物专属 ===
    private bool setCropSeedID = false;
    private int cropSeedID = 1000;
    private bool setCropExp = false;
    private int cropHarvestExp = 10;

    // === 食物专属 ===
    private bool setFoodEnergy = false;
    private int foodEnergyRestore = 30;
    private bool setFoodHealth = false;
    private int foodHealthRestore = 15;
    private BuffType foodBuffType = BuffType.None;

    // === 材料专属 ===
    private MaterialSubType materialSubType = MaterialSubType.Natural;
    private bool setMaterialSmelt = false;
    private bool materialCanSmelt = false;
    private int materialSmeltResultID = 0;

    // === 药水专属 ===
    private bool setPotionHealth = false;
    private int potionHealthRestore = 50;
    private bool setPotionEnergy = false;
    private int potionEnergyRestore = 0;
    private BuffType potionBuffType = BuffType.None;

    // === 工作台专属 ===
    private WorkstationType workstationType = WorkstationType.Crafting;
    private bool workstationRequiresFuel = false;
    private int workstationFuelSlots = 1;

    // === 存储专属 ===
    private int storageCapacity = 20;
    private bool storageIsLockable = false;

    // === 交互展示专属 ===
    private string displayTitle = "";
    private string displayContent = "";
    private float displayDuration = 0f;

    // === 简单事件专属 ===
    private SimpleEventType simpleEventType = SimpleEventType.ShowMessage;
    private bool eventIsOneTime = false;
    private float eventCooldown = 0f;

    // === 钥匙专属 ===
    private MaterialTier keyMaterial = MaterialTier.Wood;
    private float keyUnlockChance = 0.1f;

    // === 锁专属 ===
    private ChestMaterial lockMaterial = ChestMaterial.Wood;

    #endregion

    [MenuItem("Tools/📦 批量生成物品 SO")]
    public static void ShowWindow()
    {
        var window = GetWindow<Tool_BatchItemSOGenerator>("批量生成物品SO");
        window.minSize = new Vector2(520, 800);
        window.Show();
    }

    private void OnEnable()
    {
        LoadSettings();
    }

    private void OnDisable()
    {
        SaveSettings();
    }

    private void GetSelectedSprites()
    {
        selectedSprites.Clear();
        
        foreach (var obj in Selection.objects)
        {
            if (obj is Sprite sprite)
            {
                if (!selectedSprites.Contains(sprite))
                    selectedSprites.Add(sprite);
            }
            else if (obj is Texture2D texture)
            {
                string path = AssetDatabase.GetAssetPath(texture);
                var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>();
                foreach (var s in sprites)
                {
                    if (!selectedSprites.Contains(s))
                        selectedSprites.Add(s);
                }
            }
            else if (obj is DefaultAsset)
            {
                string folderPath = AssetDatabase.GetAssetPath(obj);
                if (AssetDatabase.IsValidFolder(folderPath))
                {
                    var spritesInFolder = GetAllSpritesInFolder(folderPath);
                    foreach (var s in spritesInFolder)
                    {
                        if (!selectedSprites.Contains(s))
                            selectedSprites.Add(s);
                    }
                }
            }
        }

        selectedSprites = selectedSprites.OrderBy(s => s.name).ToList();
        Repaint();
    }

    private List<Sprite> GetAllSpritesInFolder(string folderPath)
    {
        var result = new List<Sprite>();
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>();
            result.AddRange(sprites);
        }
        
        return result;
    }

    private void OnGUI()
    {
        DrawHeader();
        
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        DrawDatabaseSettings();
        DrawLine();
        DrawSpriteSelection();
        DrawLine();
        DrawCategorySelection();
        DrawLine();
        DrawIDSettings();
        DrawLine();
        DrawCommonSettings();
        DrawLine();
        DrawTypeSpecificSettings();
        DrawLine();
        DrawOutputSettings();
        DrawLine();
        DrawGenerateButton();
        
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
        EditorGUILayout.LabelField("📦 批量生成物品 SO", style, GUILayout.Height(30));
    }

    private void DrawDatabaseSettings()
    {
        EditorGUILayout.LabelField("🗄️ 数据库设置", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        databaseAsset = (ItemDatabase)EditorGUILayout.ObjectField("主数据库", databaseAsset, typeof(ItemDatabase), false);
        
        if (EditorGUI.EndChangeCheck() && databaseAsset != null)
        {
            databasePath = AssetDatabase.GetAssetPath(databaseAsset);
            DatabaseSyncHelper.SetDatabasePath(databasePath);
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("路径", GUILayout.Width(40));
        GUI.enabled = false;
        EditorGUILayout.TextField(databasePath);
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        
        if (string.IsNullOrEmpty(databasePath) || databaseAsset == null)
        {
            EditorGUILayout.HelpBox("⚠️ 请拖入 MasterItemDatabase 资产", MessageType.Warning);
        }
        else if (!DatabaseSyncHelper.DatabaseExists())
        {
            EditorGUILayout.HelpBox($"❌ 数据库文件不存在: {databasePath}", MessageType.Error);
        }
        else
        {
            EditorGUILayout.HelpBox($"✓ 数据库已配置，生成后将自动同步", MessageType.None);
        }
    }

    private void DrawSpriteSelection()
    {
        EditorGUILayout.LabelField("🖼️ 选中的 Sprite", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.HelpBox("在 Project 窗口选择 Sprite、Texture 或文件夹", MessageType.None);
        if (GUILayout.Button("🔍 获取选中项", GUILayout.Width(100), GUILayout.Height(38)))
        {
            GetSelectedSprites();
        }
        EditorGUILayout.EndHorizontal();

        if (selectedSprites.Count == 0)
        {
            EditorGUILayout.HelpBox("⚠️ 未选择任何 Sprite", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"✓ 已选择 {selectedSprites.Count} 个 Sprite", EditorStyles.boldLabel);
            
            spriteListScrollPos = EditorGUILayout.BeginScrollView(spriteListScrollPos, 
                GUILayout.Height(Mathf.Min(selectedSprites.Count * 26 + 5, 140)));
            
            int showCount = Mathf.Min(selectedSprites.Count, 10);
            for (int i = 0; i < showCount; i++)
            {
                var sprite = selectedSprites[i];
                EditorGUILayout.BeginHorizontal();
                
                var rect = GUILayoutUtility.GetRect(22, 22, GUILayout.Width(22));
                if (sprite != null && sprite.texture != null)
                {
                    GUI.DrawTextureWithTexCoords(rect, sprite.texture, 
                        new Rect(sprite.rect.x / sprite.texture.width, sprite.rect.y / sprite.texture.height,
                                 sprite.rect.width / sprite.texture.width, sprite.rect.height / sprite.texture.height));
                }
                
                int predictedID = useSequentialID ? startID + i : startID;
                EditorGUILayout.LabelField($"{sprite.name}", GUILayout.Width(180));
                EditorGUILayout.LabelField($"→ ID: {predictedID}", EditorStyles.miniLabel, GUILayout.Width(80));
                
                EditorGUILayout.EndHorizontal();
            }
            
            if (selectedSprites.Count > 10)
                EditorGUILayout.LabelField($"... 还有 {selectedSprites.Count - 10} 项", EditorStyles.miniLabel);
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawCategorySelection()
    {
        EditorGUILayout.LabelField("📋 物品类型", EditorStyles.boldLabel);
        
        // 大类按钮
        EditorGUILayout.LabelField("大类：", EditorStyles.miniLabel);
        EditorGUILayout.BeginHorizontal();
        foreach (ItemMainCategory cat in System.Enum.GetValues(typeof(ItemMainCategory)))
        {
            GUI.backgroundColor = mainCategory == cat ? CategoryColors[cat] : Color.white;
            if (GUILayout.Button(CategoryNames[cat], GUILayout.Height(28)))
            {
                mainCategory = cat;
                // 切换大类时自动选中第一个小类
                var subTypes = CategoryToSubTypes[cat];
                if (subTypes.Length > 0)
                {
                    soType = subTypes[0];
                    AutoSetStartIDAndFolder();
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        
        // 小类按钮
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("小类：", EditorStyles.miniLabel);
        var currentSubTypes = CategoryToSubTypes[mainCategory];
        
        // 自动换行显示小类按钮
        int buttonsPerRow = 5;
        for (int i = 0; i < currentSubTypes.Length; i += buttonsPerRow)
        {
            EditorGUILayout.BeginHorizontal();
            for (int j = i; j < Mathf.Min(i + buttonsPerRow, currentSubTypes.Length); j++)
            {
                var subType = currentSubTypes[j];
                GUI.backgroundColor = soType == subType ? CategoryColors[mainCategory] : new Color(0.85f, 0.85f, 0.85f);
                if (GUILayout.Button(SubTypeNames[subType], GUILayout.Height(26)))
                {
                    soType = subType;
                    AutoSetStartIDAndFolder();
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        
        GUI.backgroundColor = Color.white;
        
        // 类型说明
        EditorGUILayout.HelpBox(GetTypeDescription(), MessageType.Info);
    }

    private void AutoSetStartIDAndFolder()
    {
        if (SubTypeStartIDs.TryGetValue(soType, out int id))
            startID = id;
        if (SubTypeOutputFolders.TryGetValue(soType, out string folder))
            outputFolder = folder;
    }

    private string GetTypeDescription()
    {
        string catName = CategoryNames[mainCategory];
        string subName = SubTypeNames[soType];
        int id = SubTypeStartIDs.GetValueOrDefault(soType, 0);
        
        string desc = soType switch
        {
            ItemSOType.ToolData => "锄头、斧头、镐子、水壶等农具和采集工具",
            ItemSOType.WeaponData => "剑、弓、法杖等战斗装备",
            ItemSOType.KeyData => "用于开锁野外上锁箱子的钥匙",
            ItemSOType.SeedData => "可种植的种子",
            ItemSOType.CropData => "收获的农作物",
            ItemSOType.SaplingData => "可放置的树苗，种下后成为树木",
            ItemSOType.WorkstationData => "工作台、熔炉、制作设施等",
            ItemSOType.StorageData => "箱子等存储容器",
            ItemSOType.InteractiveDisplayData => "告示牌等交互展示物品",
            ItemSOType.SimpleEventData => "传送点等触发事件的物品",
            ItemSOType.FoodData => "可食用的料理",
            ItemSOType.PotionData => "HP药水、精力药水等",
            ItemSOType.MaterialData => "矿石、木材、怪物掉落等",
            ItemSOType.ItemData => "通用基础物品",
            ItemSOType.FurnitureData => "装饰家具",
            ItemSOType.SpecialData => "特殊物品",
            _ => ""
        };
        
        return $"{catName} > {subName}\n{desc}\nID 范围：{id}XX";
    }

    private void DrawIDSettings()
    {
        EditorGUILayout.LabelField("🔢 ID 设置", EditorStyles.boldLabel);
        
        useSequentialID = EditorGUILayout.Toggle("连续 ID 模式", useSequentialID);
        
        string idHint = useSequentialID 
            ? $"按 Sprite 名称排序后依次递增：{startID} ~ {startID + Mathf.Max(0, selectedSprites.Count - 1)}"
            : "所有物品使用相同 ID（需手动修改）";
        EditorGUILayout.HelpBox(idHint, useSequentialID ? MessageType.Info : MessageType.Warning);
        
        startID = EditorGUILayout.IntField("起始 ID", startID);
    }

    private void DrawCommonSettings()
    {
        EditorGUILayout.LabelField("⚙️ 通用属性（可选，不勾选则留空）", EditorStyles.boldLabel);
        
        // 价格设置
        EditorGUILayout.BeginHorizontal();
        setPrice = EditorGUILayout.Toggle(setPrice, GUILayout.Width(20));
        EditorGUI.BeginDisabledGroup(!setPrice);
        EditorGUILayout.LabelField("价格", GUILayout.Width(40));
        defaultBuyPrice = EditorGUILayout.IntField("买", defaultBuyPrice, GUILayout.Width(80));
        defaultSellPrice = EditorGUILayout.IntField("卖", defaultSellPrice, GUILayout.Width(80));
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
        
        // 堆叠设置
        bool canStack = soType != ItemSOType.ToolData && soType != ItemSOType.WeaponData;
        
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = canStack;
        setMaxStack = canStack && EditorGUILayout.Toggle(setMaxStack, GUILayout.Width(20));
        EditorGUI.BeginDisabledGroup(!setMaxStack || !canStack);
        defaultMaxStack = EditorGUILayout.IntField("最大堆叠数", defaultMaxStack);
        EditorGUI.EndDisabledGroup();
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        
        if (!canStack)
            EditorGUILayout.HelpBox("工具和武器不可堆叠，固定为 1", MessageType.None);
        
        // 显示尺寸设置
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        setDisplaySize = EditorGUILayout.Toggle(setDisplaySize, GUILayout.Width(20));
        EditorGUI.BeginDisabledGroup(!setDisplaySize);
        displayPixelSize = EditorGUILayout.IntSlider("世界显示尺寸 (像素)", displayPixelSize, 8, 128);
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
        
        if (setDisplaySize)
            EditorGUILayout.HelpBox($"世界物品将等比例缩放至 {displayPixelSize}×{displayPixelSize} 像素方框内", MessageType.Info);
    }

    private void DrawTypeSpecificSettings()
    {
        switch (soType)
        {
            case ItemSOType.ToolData: DrawToolSettings(); break;
            case ItemSOType.WeaponData: DrawWeaponSettings(); break;
            case ItemSOType.KeyData: DrawKeySettings(); break;
            case ItemSOType.LockData: DrawLockSettings(); break;
            case ItemSOType.SeedData: DrawSeedSettings(); break;
            case ItemSOType.SaplingData: DrawSaplingSettings(); break;
            case ItemSOType.CropData: DrawCropSettings(); break;
            case ItemSOType.FoodData: DrawFoodSettings(); break;
            case ItemSOType.MaterialData: DrawMaterialSettings(); break;
            case ItemSOType.PotionData: DrawPotionSettings(); break;
            case ItemSOType.WorkstationData: DrawWorkstationSettings(); break;
            case ItemSOType.StorageData: DrawStorageSettings(); break;
            case ItemSOType.InteractiveDisplayData: DrawInteractiveDisplaySettings(); break;
            case ItemSOType.SimpleEventData: DrawSimpleEventSettings(); break;
        }
    }

    private void DrawToolSettings()
    {
        EditorGUILayout.LabelField("🔧 工具专属设置", EditorStyles.boldLabel);
        
        toolType = (ToolType)EditorGUILayout.EnumPopup("工具类型", toolType);
        
        AnimActionType autoAnimType = GetAnimActionType(toolType);
        GUI.enabled = false;
        EditorGUILayout.EnumPopup("动画动作（自动）", autoAnimType);
        GUI.enabled = true;
        
        EditorGUILayout.HelpBox("工具品质通过后缀命名区分（如 Axe_0, Axe_1）", MessageType.Info);
        
        DrawOptionalInt(ref setToolEnergy, ref toolEnergyCost, "精力消耗", 1, 20);
        DrawOptionalInt(ref setToolRadius, ref toolEffectRadius, "作用范围", 1, 5);
        DrawOptionalInt(ref setToolAnimFrames, ref toolAnimFrameCount, "动画帧数", 1, 30);
    }

    private AnimActionType GetAnimActionType(ToolType type)
    {
        return type switch
        {
            ToolType.Axe => AnimActionType.Slice,
            ToolType.Sickle => AnimActionType.Slice,
            ToolType.Pickaxe => AnimActionType.Crush,
            ToolType.Hoe => AnimActionType.Crush,
            ToolType.FishingRod => AnimActionType.Fish,
            ToolType.WateringCan => AnimActionType.Watering,
            _ => AnimActionType.Slice
        };
    }

    private void DrawWeaponSettings()
    {
        EditorGUILayout.LabelField("⚔️ 武器专属设置", EditorStyles.boldLabel);
        
        weaponType = (WeaponType)EditorGUILayout.EnumPopup("武器类型", weaponType);
        EditorGUILayout.HelpBox("武器品质通过后缀命名区分", MessageType.Info);
        
        DrawOptionalInt(ref setWeaponAttack, ref weaponAttackPower, "攻击力", 1, 200);
        DrawOptionalFloat(ref setWeaponSpeed, ref weaponAttackSpeed, "攻击速度", 0.3f, 3.0f);
        DrawOptionalFloat(ref setWeaponCrit, ref weaponCritChance, "暴击率 (%)", 0f, 100f);
    }

    private void DrawKeySettings()
    {
        EditorGUILayout.LabelField("🔑 钥匙专属设置", EditorStyles.boldLabel);
        
        EditorGUI.BeginChangeCheck();
        keyMaterial = (MaterialTier)EditorGUILayout.EnumPopup("钥匙材质", keyMaterial);
        if (EditorGUI.EndChangeCheck())
        {
            // 根据材质自动设置默认开锁概率
            keyUnlockChance = KeyLockData.GetDefaultUnlockChanceByTier(keyMaterial);
        }
        
        keyUnlockChance = EditorGUILayout.Slider("开锁概率", keyUnlockChance, 0f, 1f);
        
        // 显示概率参考表
        EditorGUILayout.HelpBox(
            "钥匙开锁概率参考：\n" +
            "木: 10%  石: 15%  铁: 20%\n" +
            "铜: 25%  钢: 30%  金: 40%\n\n" +
            "最终概率 = 钥匙概率 + 箱子概率\n" +
            "成功保留钥匙，失败消耗钥匙", 
            MessageType.Info);
    }

    private void DrawLockSettings()
    {
        EditorGUILayout.LabelField("🔒 锁专属设置", EditorStyles.boldLabel);
        
        lockMaterial = (ChestMaterial)EditorGUILayout.EnumPopup("锁材质", lockMaterial);
        
        // 显示锁的使用说明
        EditorGUILayout.HelpBox(
            "锁的使用规则：\n" +
            "• 必须与箱子材质匹配才能上锁\n" +
            "• 使用后箱子变为上锁状态\n" +
            "• 锁不可取下\n" +
            "• 所有上过锁的箱子不能再次上锁\n\n" +
            "锁的ID范围：1410-1419\n" +
            "木锁: 1410  铁锁: 1411  特殊锁: 1412+", 
            MessageType.Info);
    }

    private void DrawSeedSettings()
    {
        EditorGUILayout.LabelField("🌱 种子专属设置", EditorStyles.boldLabel);
        
        seedSeason = (Season)EditorGUILayout.EnumPopup("适合季节", seedSeason);
        DrawOptionalInt(ref setSeedGrowth, ref seedGrowthDays, "生长天数", 1, 28);
        DrawOptionalInt(ref setSeedHarvest, ref seedHarvestCropID, "收获作物 ID", 1100, 1199);
    }

    private void DrawSaplingSettings()
    {
        EditorGUILayout.LabelField("🌳 树苗专属设置", EditorStyles.boldLabel);
        
        EditorGUILayout.HelpBox("树苗只需设置关联的树木预制体，季节样式由 TreeControllerV2 自动处理\n冬季无法种植树苗", MessageType.Info);
        
        saplingTreePrefab = (GameObject)EditorGUILayout.ObjectField("树木预制体", saplingTreePrefab, typeof(GameObject), false);
        
        if (saplingTreePrefab != null)
        {
            var treeController = saplingTreePrefab.GetComponentInChildren<TreeControllerV2>();
            if (treeController == null)
                EditorGUILayout.HelpBox("⚠️ 预制体缺少 TreeControllerV2 组件！", MessageType.Error);
            else
                EditorGUILayout.HelpBox("✓ 预制体包含 TreeControllerV2 组件", MessageType.None);
        }
        else
        {
            EditorGUILayout.HelpBox("请选择树木预制体（如 M1.prefab）", MessageType.Warning);
        }
        
        DrawOptionalInt(ref setSaplingExp, ref saplingPlantingExp, "种植经验", 1, 50);
    }

    private void DrawCropSettings()
    {
        EditorGUILayout.LabelField("🌾 作物专属设置", EditorStyles.boldLabel);
        
        DrawOptionalInt(ref setCropSeedID, ref cropSeedID, "对应种子 ID", 1000, 1099);
        DrawOptionalInt(ref setCropExp, ref cropHarvestExp, "收获经验", 1, 100);
    }

    private void DrawFoodSettings()
    {
        EditorGUILayout.LabelField("🍳 食物专属设置", EditorStyles.boldLabel);
        
        DrawOptionalInt(ref setFoodEnergy, ref foodEnergyRestore, "恢复精力", 0, 200);
        DrawOptionalInt(ref setFoodHealth, ref foodHealthRestore, "恢复 HP", 0, 200);
        foodBuffType = (BuffType)EditorGUILayout.EnumPopup("Buff 类型", foodBuffType);
    }

    private void DrawMaterialSettings()
    {
        EditorGUILayout.LabelField("🪨 材料专属设置", EditorStyles.boldLabel);
        
        materialSubType = (MaterialSubType)EditorGUILayout.EnumPopup("材料子类", materialSubType);
        
        string subTypeHint = materialSubType switch
        {
            MaterialSubType.Ore => "矿石 - 推荐 ID: 30XX",
            MaterialSubType.Ingot => "锭 - 推荐 ID: 31XX",
            MaterialSubType.Natural => "自然材料 - 推荐 ID: 32XX",
            MaterialSubType.Monster => "怪物掉落 - 推荐 ID: 33XX",
            _ => ""
        };
        EditorGUILayout.HelpBox(subTypeHint, MessageType.None);
        
        if (materialSubType == MaterialSubType.Ore)
        {
            EditorGUILayout.BeginHorizontal();
            setMaterialSmelt = EditorGUILayout.Toggle(setMaterialSmelt, GUILayout.Width(20));
            EditorGUI.BeginDisabledGroup(!setMaterialSmelt);
            materialCanSmelt = EditorGUILayout.Toggle("可熔炼", materialCanSmelt);
            if (materialCanSmelt)
                materialSmeltResultID = EditorGUILayout.IntField("产物 ID", materialSmeltResultID);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawPotionSettings()
    {
        EditorGUILayout.LabelField("🧪 药水专属设置", EditorStyles.boldLabel);
        
        DrawOptionalInt(ref setPotionHealth, ref potionHealthRestore, "恢复 HP", 0, 500);
        DrawOptionalInt(ref setPotionEnergy, ref potionEnergyRestore, "恢复精力", 0, 200);
        potionBuffType = (BuffType)EditorGUILayout.EnumPopup("Buff 类型", potionBuffType);
    }

    private void DrawWorkstationSettings()
    {
        EditorGUILayout.LabelField("🏭 工作台专属设置", EditorStyles.boldLabel);
        
        workstationType = (WorkstationType)EditorGUILayout.EnumPopup("工作台类型", workstationType);
        workstationRequiresFuel = EditorGUILayout.Toggle("需要燃料", workstationRequiresFuel);
        
        if (workstationRequiresFuel)
        {
            workstationFuelSlots = EditorGUILayout.IntSlider("燃料槽数量", workstationFuelSlots, 1, 4);
        }
        
        EditorGUILayout.HelpBox("工作台放置后可进行制作操作", MessageType.Info);
    }

    private void DrawStorageSettings()
    {
        EditorGUILayout.LabelField("📦 存储专属设置", EditorStyles.boldLabel);
        
        storageCapacity = EditorGUILayout.IntSlider("存储容量", storageCapacity, 4, 100);
        storageIsLockable = EditorGUILayout.Toggle("可上锁", storageIsLockable);
        
        EditorGUILayout.HelpBox("存储容器放置后可存放物品", MessageType.Info);
    }

    private void DrawInteractiveDisplaySettings()
    {
        EditorGUILayout.LabelField("📋 交互展示专属设置", EditorStyles.boldLabel);
        
        displayTitle = EditorGUILayout.TextField("显示标题", displayTitle);
        EditorGUILayout.LabelField("显示内容：");
        displayContent = EditorGUILayout.TextArea(displayContent, GUILayout.Height(60));
        displayDuration = EditorGUILayout.Slider("显示时长 (0=手动关闭)", displayDuration, 0f, 30f);
        
        EditorGUILayout.HelpBox("交互后显示配置的文本内容", MessageType.Info);
    }

    private void DrawSimpleEventSettings()
    {
        EditorGUILayout.LabelField("⚡ 简单事件专属设置", EditorStyles.boldLabel);
        
        simpleEventType = (SimpleEventType)EditorGUILayout.EnumPopup("事件类型", simpleEventType);
        eventIsOneTime = EditorGUILayout.Toggle("一次性触发", eventIsOneTime);
        eventCooldown = EditorGUILayout.Slider("冷却时间 (秒)", eventCooldown, 0f, 60f);
        
        EditorGUILayout.HelpBox("交互后触发配置的事件", MessageType.Info);
    }

    private void DrawOptionalInt(ref bool enabled, ref int value, string label, int min, int max)
    {
        EditorGUILayout.BeginHorizontal();
        enabled = EditorGUILayout.Toggle(enabled, GUILayout.Width(20));
        EditorGUI.BeginDisabledGroup(!enabled);
        value = EditorGUILayout.IntSlider(label, value, min, max);
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawOptionalFloat(ref bool enabled, ref float value, string label, float min, float max)
    {
        EditorGUILayout.BeginHorizontal();
        enabled = EditorGUILayout.Toggle(enabled, GUILayout.Width(20));
        EditorGUI.BeginDisabledGroup(!enabled);
        value = EditorGUILayout.Slider(label, value, min, max);
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawOutputSettings()
    {
        EditorGUILayout.LabelField("📁 输出设置", EditorStyles.boldLabel);
        
        string autoFolder = SubTypeOutputFolders.GetValueOrDefault(soType, "Assets/111_Data/Items");
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("输出文件夹", GUILayout.Width(80));
        outputFolder = EditorGUILayout.TextField(outputFolder);
        if (GUILayout.Button("自动", GUILayout.Width(45)))
        {
            outputFolder = autoFolder;
        }
        if (GUILayout.Button("选择", GUILayout.Width(45)))
        {
            string path = EditorUtility.OpenFolderPanel("选择输出文件夹", "Assets", "");
            if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
            {
                outputFolder = "Assets" + path.Substring(Application.dataPath.Length);
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.HelpBox($"推荐路径：{autoFolder}", MessageType.None);
    }

    private void DrawGenerateButton()
    {
        EditorGUILayout.Space(10);
        
        GUI.enabled = selectedSprites.Count > 0;
        GUI.backgroundColor = new Color(0.3f, 0.9f, 0.3f);
        
        string typeName = SubTypeNames.GetValueOrDefault(soType, "物品");
        if (GUILayout.Button($"🚀 生成 {selectedSprites.Count} 个 {typeName} SO", GUILayout.Height(45)))
        {
            GenerateItemSOs();
        }
        
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;
        
        if (selectedSprites.Count == 0)
        {
            EditorGUILayout.HelpBox("请先在 Project 窗口选择 Sprite", MessageType.Warning);
        }
    }

    private void DrawLine()
    {
        EditorGUILayout.Space(5);
        Rect rect = EditorGUILayout.GetControlRect(false, 2);
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        EditorGUILayout.Space(5);
    }

    #endregion

    #region 生成逻辑

    private void GenerateItemSOs()
    {
        EnsureFolderExists(outputFolder);

        int successCount = 0;
        List<string> createdFiles = new List<string>();

        for (int i = 0; i < selectedSprites.Count; i++)
        {
            var sprite = selectedSprites[i];
            int itemID = useSequentialID ? startID + i : startID;
            string itemName = sprite.name;

            ScriptableObject so = CreateItemSO(sprite, itemID, itemName);
            if (so != null)
            {
                string prefix = GetFilePrefix();
                string fileName = $"{prefix}_{itemID}_{itemName}.asset";
                string assetPath = $"{outputFolder}/{fileName}";

                if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath) != null)
                {
                    if (!EditorUtility.DisplayDialog("文件已存在", $"文件 {fileName} 已存在，是否覆盖？", "覆盖", "跳过"))
                        continue;
                    AssetDatabase.DeleteAsset(assetPath);
                }

                AssetDatabase.CreateAsset(so, assetPath);
                createdFiles.Add(assetPath);
                successCount++;
                
                Debug.Log($"<color=green>[批量生成] 创建: {assetPath}</color>");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (createdFiles.Count > 0)
        {
            var assets = createdFiles.Select(p => AssetDatabase.LoadAssetAtPath<Object>(p)).ToArray();
            Selection.objects = assets;
        }

        string syncMessage = "";
        if (successCount > 0 && DatabaseSyncHelper.DatabaseExists())
        {
            int syncCount = DatabaseSyncHelper.AutoCollectAllItems();
            syncMessage = syncCount >= 0 
                ? $"\n\n✅ 数据库已自动同步（共 {syncCount} 个物品）"
                : "\n\n⚠️ 数据库同步失败，请手动执行";
        }

        string typeName = SubTypeNames.GetValueOrDefault(soType, "物品");
        EditorUtility.DisplayDialog("完成", $"成功创建 {successCount} 个 {typeName} SO\n保存位置：{outputFolder}{syncMessage}", "确定");
        Debug.Log($"<color=green>[批量生成] ✅ 完成！共创建 {successCount} 个物品</color>");
    }

    private void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;
        
        string[] folders = folderPath.Split('/');
        string currentPath = folders[0];
        
        for (int i = 1; i < folders.Length; i++)
        {
            string newPath = currentPath + "/" + folders[i];
            if (!AssetDatabase.IsValidFolder(newPath))
                AssetDatabase.CreateFolder(currentPath, folders[i]);
            currentPath = newPath;
        }
    }

    private string GetFilePrefix()
    {
        return soType switch
        {
            ItemSOType.ToolData => "Tool",
            ItemSOType.WeaponData => "Weapon",
            ItemSOType.KeyData => "Key",
            ItemSOType.SeedData => "Seed",
            ItemSOType.SaplingData => "Sapling",
            ItemSOType.CropData => "Crop",
            ItemSOType.FoodData => "Food",
            ItemSOType.MaterialData => "Material",
            ItemSOType.PotionData => "Potion",
            ItemSOType.WorkstationData => "Workstation",
            ItemSOType.StorageData => "Storage",
            ItemSOType.InteractiveDisplayData => "Display",
            ItemSOType.SimpleEventData => "Event",
            ItemSOType.FurnitureData => "Furniture",
            ItemSOType.SpecialData => "Special",
            _ => "Item"
        };
    }

    private ScriptableObject CreateItemSO(Sprite sprite, int itemID, string itemName)
    {
        return soType switch
        {
            ItemSOType.ToolData => CreateToolData(sprite, itemID, itemName),
            ItemSOType.WeaponData => CreateWeaponData(sprite, itemID, itemName),
            ItemSOType.KeyData => CreateKeyData(sprite, itemID, itemName),
            ItemSOType.LockData => CreateLockData(sprite, itemID, itemName),
            ItemSOType.SeedData => CreateSeedData(sprite, itemID, itemName),
            ItemSOType.SaplingData => CreateSaplingData(sprite, itemID, itemName),
            ItemSOType.CropData => CreateCropData(sprite, itemID, itemName),
            ItemSOType.FoodData => CreateFoodData(sprite, itemID, itemName),
            ItemSOType.MaterialData => CreateMaterialData(sprite, itemID, itemName),
            ItemSOType.PotionData => CreatePotionData(sprite, itemID, itemName),
            ItemSOType.WorkstationData => CreateWorkstationData(sprite, itemID, itemName),
            ItemSOType.StorageData => CreateStorageData(sprite, itemID, itemName),
            ItemSOType.InteractiveDisplayData => CreateInteractiveDisplayData(sprite, itemID, itemName),
            ItemSOType.SimpleEventData => CreateSimpleEventData(sprite, itemID, itemName),
            _ => CreateBaseItemData(sprite, itemID, itemName)
        };
    }

    private ItemData CreateBaseItemData(Sprite sprite, int itemID, string itemName)
    {
        var data = ScriptableObject.CreateInstance<ItemData>();
        SetCommonProperties(data, sprite, itemID, itemName, ItemCategory.Special);
        if (setMaxStack) data.maxStackSize = defaultMaxStack;
        return data;
    }

    private ToolData CreateToolData(Sprite sprite, int itemID, string itemName)
    {
        var data = ScriptableObject.CreateInstance<ToolData>();
        SetCommonProperties(data, sprite, itemID, itemName, ItemCategory.Tool);
        data.maxStackSize = 1;
        data.toolType = toolType;
        data.animActionType = GetAnimActionType(toolType);
        if (setToolEnergy) data.energyCost = toolEnergyCost;
        if (setToolRadius) data.effectRadius = toolEffectRadius;
        if (setToolAnimFrames) data.animationFrameCount = toolAnimFrameCount;
        return data;
    }

    private WeaponData CreateWeaponData(Sprite sprite, int itemID, string itemName)
    {
        var data = ScriptableObject.CreateInstance<WeaponData>();
        SetCommonProperties(data, sprite, itemID, itemName, ItemCategory.Tool);
        data.maxStackSize = 1;
        data.weaponType = weaponType;
        if (setWeaponAttack) data.attackPower = weaponAttackPower;
        if (setWeaponSpeed) data.attackSpeed = weaponAttackSpeed;
        if (setWeaponCrit) data.criticalChance = weaponCritChance;
        return data;
    }

    private KeyLockData CreateKeyData(Sprite sprite, int itemID, string itemName)
    {
        var data = ScriptableObject.CreateInstance<KeyLockData>();
        SetCommonProperties(data, sprite, itemID, itemName, ItemCategory.Tool);
        data.maxStackSize = setMaxStack ? defaultMaxStack : 99;
        data.keyLockType = KeyLockType.Key;
        // 将 MaterialTier 转换为 ChestMaterial
        data.material = keyMaterial switch
        {
            MaterialTier.Wood => ChestMaterial.Wood,
            MaterialTier.Stone => ChestMaterial.Wood,  // 石质钥匙对应木箱
            MaterialTier.Iron => ChestMaterial.Iron,
            MaterialTier.Brass => ChestMaterial.Iron,  // 铜质钥匙对应铁箱
            MaterialTier.Steel => ChestMaterial.Iron,
            MaterialTier.Gold => ChestMaterial.Special,
            _ => ChestMaterial.Wood
        };
        data.unlockChance = keyUnlockChance;
        return data;
    }

    private KeyLockData CreateLockData(Sprite sprite, int itemID, string itemName)
    {
        var data = ScriptableObject.CreateInstance<KeyLockData>();
        SetCommonProperties(data, sprite, itemID, itemName, ItemCategory.Tool);
        data.maxStackSize = setMaxStack ? defaultMaxStack : 99;
        data.keyLockType = KeyLockType.Lock;
        data.material = lockMaterial;
        data.unlockChance = 0f;  // 锁不需要开锁概率
        return data;
    }

    private SeedData CreateSeedData(Sprite sprite, int itemID, string itemName)
    {
        var data = ScriptableObject.CreateInstance<SeedData>();
        SetCommonProperties(data, sprite, itemID, itemName, ItemCategory.Plant);
        data.maxStackSize = setMaxStack ? defaultMaxStack : 99;
        data.season = seedSeason;
        if (setSeedGrowth) data.growthDays = seedGrowthDays;
        if (setSeedHarvest) data.harvestCropID = seedHarvestCropID;
        return data;
    }

    private SaplingData CreateSaplingData(Sprite sprite, int itemID, string itemName)
    {
        var data = ScriptableObject.CreateInstance<SaplingData>();
        SetCommonProperties(data, sprite, itemID, itemName, ItemCategory.Plant);
        data.maxStackSize = setMaxStack ? defaultMaxStack : 99;
        data.treePrefab = saplingTreePrefab;
        if (setSaplingExp) data.plantingExp = saplingPlantingExp;
        return data;
    }

    private CropData CreateCropData(Sprite sprite, int itemID, string itemName)
    {
        var data = ScriptableObject.CreateInstance<CropData>();
        SetCommonProperties(data, sprite, itemID, itemName, ItemCategory.Plant);
        data.maxStackSize = setMaxStack ? defaultMaxStack : 99;
        if (setCropSeedID) data.seedID = cropSeedID;
        if (setCropExp) data.harvestExp = cropHarvestExp;
        return data;
    }

    private FoodData CreateFoodData(Sprite sprite, int itemID, string itemName)
    {
        var data = ScriptableObject.CreateInstance<FoodData>();
        SetCommonProperties(data, sprite, itemID, itemName, ItemCategory.Food);
        data.maxStackSize = setMaxStack ? defaultMaxStack : 20;
        if (setFoodEnergy) data.energyRestore = foodEnergyRestore;
        if (setFoodHealth) data.healthRestore = foodHealthRestore;
        data.buffType = foodBuffType;
        return data;
    }

    private MaterialData CreateMaterialData(Sprite sprite, int itemID, string itemName)
    {
        var data = ScriptableObject.CreateInstance<MaterialData>();
        SetCommonProperties(data, sprite, itemID, itemName, ItemCategory.Material);
        data.maxStackSize = setMaxStack ? defaultMaxStack : 99;
        data.materialSubType = materialSubType;
        if (setMaterialSmelt && materialSubType == MaterialSubType.Ore)
        {
            data.canBeSmelt = materialCanSmelt;
            if (materialCanSmelt) data.smeltResultID = materialSmeltResultID;
        }
        return data;
    }

    private PotionData CreatePotionData(Sprite sprite, int itemID, string itemName)
    {
        var data = ScriptableObject.CreateInstance<PotionData>();
        SetCommonProperties(data, sprite, itemID, itemName, ItemCategory.Consumable);
        data.maxStackSize = setMaxStack ? defaultMaxStack : 20;
        if (setPotionHealth) data.healthRestore = potionHealthRestore;
        if (setPotionEnergy) data.energyRestore = potionEnergyRestore;
        data.buffType = potionBuffType;
        return data;
    }

    private WorkstationData CreateWorkstationData(Sprite sprite, int itemID, string itemName)
    {
        var data = ScriptableObject.CreateInstance<WorkstationData>();
        SetCommonProperties(data, sprite, itemID, itemName, ItemCategory.Special);
        data.maxStackSize = 1;
        data.workstationType = workstationType;
        data.requiresFuel = workstationRequiresFuel;
        if (workstationRequiresFuel) data.fuelSlotCount = workstationFuelSlots;
        return data;
    }

    private StorageData CreateStorageData(Sprite sprite, int itemID, string itemName)
    {
        var data = ScriptableObject.CreateInstance<StorageData>();
        SetCommonProperties(data, sprite, itemID, itemName, ItemCategory.Special);
        data.maxStackSize = 1;
        data.storageCapacity = storageCapacity;
        data.isLockable = storageIsLockable;
        return data;
    }

    private InteractiveDisplayData CreateInteractiveDisplayData(Sprite sprite, int itemID, string itemName)
    {
        var data = ScriptableObject.CreateInstance<InteractiveDisplayData>();
        SetCommonProperties(data, sprite, itemID, itemName, ItemCategory.Special);
        data.maxStackSize = 1;
        data.displayTitle = displayTitle;
        data.displayContent = displayContent;
        data.displayDuration = displayDuration;
        return data;
    }

    private SimpleEventData CreateSimpleEventData(Sprite sprite, int itemID, string itemName)
    {
        var data = ScriptableObject.CreateInstance<SimpleEventData>();
        SetCommonProperties(data, sprite, itemID, itemName, ItemCategory.Special);
        data.maxStackSize = 1;
        data.eventType = simpleEventType;
        data.isOneTime = eventIsOneTime;
        data.cooldownTime = eventCooldown;
        return data;
    }

    private void SetCommonProperties(ItemData data, Sprite sprite, int itemID, string itemName, ItemCategory category)
    {
        data.itemID = itemID;
        data.itemName = itemName;
        data.description = "";
        data.category = category;
        data.icon = sprite;
        data.bagSprite = null;
        data.worldPrefab = null;
        
        if (setPrice)
        {
            data.buyPrice = defaultBuyPrice;
            data.sellPrice = defaultSellPrice;
        }
        
        if (setDisplaySize)
        {
            data.useCustomDisplaySize = true;
            data.displayPixelSize = displayPixelSize;
        }
    }

    #endregion

    #region 设置保存/加载

    private void LoadSettings()
    {
        databasePath = DatabaseSyncHelper.DatabasePath;
        if (!string.IsNullOrEmpty(databasePath))
            databaseAsset = AssetDatabase.LoadAssetAtPath<ItemDatabase>(databasePath);
        
        mainCategory = (ItemMainCategory)EditorPrefs.GetInt("BatchItemSO_MainCat", 5);
        soType = (ItemSOType)EditorPrefs.GetInt("BatchItemSO_SubType", 50);
        useSequentialID = EditorPrefs.GetBool("BatchItemSO_SeqID", true);
        startID = EditorPrefs.GetInt("BatchItemSO_StartID", 0);
        outputFolder = EditorPrefs.GetString("BatchItemSO_Output", "Assets/111_Data/Items");
        
        // 通用
        setPrice = EditorPrefs.GetBool("BatchItemSO_SetPrice", false);
        defaultBuyPrice = EditorPrefs.GetInt("BatchItemSO_BuyPrice", 0);
        defaultSellPrice = EditorPrefs.GetInt("BatchItemSO_SellPrice", 0);
        setMaxStack = EditorPrefs.GetBool("BatchItemSO_SetStack", false);
        defaultMaxStack = EditorPrefs.GetInt("BatchItemSO_MaxStack", 99);
        setDisplaySize = EditorPrefs.GetBool("BatchItemSO_SetDisplaySize", false);
        displayPixelSize = EditorPrefs.GetInt("BatchItemSO_DisplaySize", 32);
        
        // 工具
        toolType = (ToolType)EditorPrefs.GetInt("BatchItemSO_ToolType", 0);
        setToolEnergy = EditorPrefs.GetBool("BatchItemSO_SetToolEnergy", false);
        toolEnergyCost = EditorPrefs.GetInt("BatchItemSO_ToolEnergy", 2);
        setToolRadius = EditorPrefs.GetBool("BatchItemSO_SetToolRadius", false);
        toolEffectRadius = EditorPrefs.GetInt("BatchItemSO_ToolRadius", 1);
        setToolAnimFrames = EditorPrefs.GetBool("BatchItemSO_SetToolAnimFrames", false);
        toolAnimFrameCount = EditorPrefs.GetInt("BatchItemSO_ToolAnimFrames", 8);
        
        // 武器
        weaponType = (WeaponType)EditorPrefs.GetInt("BatchItemSO_WeaponType", 0);
        setWeaponAttack = EditorPrefs.GetBool("BatchItemSO_SetWeaponAtk", false);
        weaponAttackPower = EditorPrefs.GetInt("BatchItemSO_WeaponAtk", 10);
        setWeaponSpeed = EditorPrefs.GetBool("BatchItemSO_SetWeaponSpeed", false);
        weaponAttackSpeed = EditorPrefs.GetFloat("BatchItemSO_WeaponSpeed", 1.0f);
        setWeaponCrit = EditorPrefs.GetBool("BatchItemSO_SetWeaponCrit", false);
        weaponCritChance = EditorPrefs.GetFloat("BatchItemSO_WeaponCrit", 5f);
        
        // 种子
        seedSeason = (Season)EditorPrefs.GetInt("BatchItemSO_SeedSeason", 0);
        setSeedGrowth = EditorPrefs.GetBool("BatchItemSO_SetSeedGrowth", false);
        seedGrowthDays = EditorPrefs.GetInt("BatchItemSO_SeedGrowth", 4);
        setSeedHarvest = EditorPrefs.GetBool("BatchItemSO_SetSeedHarvest", false);
        seedHarvestCropID = EditorPrefs.GetInt("BatchItemSO_SeedHarvestID", 1100);
        
        // 树苗
        string saplingPrefabPath = EditorPrefs.GetString("BatchItemSO_SaplingPrefab", "");
        if (!string.IsNullOrEmpty(saplingPrefabPath))
            saplingTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(saplingPrefabPath);
        setSaplingExp = EditorPrefs.GetBool("BatchItemSO_SetSaplingExp", false);
        saplingPlantingExp = EditorPrefs.GetInt("BatchItemSO_SaplingExp", 5);
        
        // 作物
        setCropSeedID = EditorPrefs.GetBool("BatchItemSO_SetCropSeedID", false);
        cropSeedID = EditorPrefs.GetInt("BatchItemSO_CropSeedID", 1000);
        setCropExp = EditorPrefs.GetBool("BatchItemSO_SetCropExp", false);
        cropHarvestExp = EditorPrefs.GetInt("BatchItemSO_CropExp", 10);
        
        // 食物
        setFoodEnergy = EditorPrefs.GetBool("BatchItemSO_SetFoodEnergy", false);
        foodEnergyRestore = EditorPrefs.GetInt("BatchItemSO_FoodEnergy", 30);
        setFoodHealth = EditorPrefs.GetBool("BatchItemSO_SetFoodHealth", false);
        foodHealthRestore = EditorPrefs.GetInt("BatchItemSO_FoodHealth", 15);
        foodBuffType = (BuffType)EditorPrefs.GetInt("BatchItemSO_FoodBuff", 0);
        
        // 材料
        materialSubType = (MaterialSubType)EditorPrefs.GetInt("BatchItemSO_MatSubType", 2);
        setMaterialSmelt = EditorPrefs.GetBool("BatchItemSO_SetMatSmelt", false);
        materialCanSmelt = EditorPrefs.GetBool("BatchItemSO_MatCanSmelt", false);
        materialSmeltResultID = EditorPrefs.GetInt("BatchItemSO_MatSmeltID", 0);
        
        // 药水
        setPotionHealth = EditorPrefs.GetBool("BatchItemSO_SetPotionHealth", false);
        potionHealthRestore = EditorPrefs.GetInt("BatchItemSO_PotionHealth", 50);
        setPotionEnergy = EditorPrefs.GetBool("BatchItemSO_SetPotionEnergy", false);
        potionEnergyRestore = EditorPrefs.GetInt("BatchItemSO_PotionEnergy", 0);
        potionBuffType = (BuffType)EditorPrefs.GetInt("BatchItemSO_PotionBuff", 0);
        
        // 工作台
        workstationType = (WorkstationType)EditorPrefs.GetInt("BatchItemSO_WorkstationType", 5);
        workstationRequiresFuel = EditorPrefs.GetBool("BatchItemSO_WorkstationFuel", false);
        workstationFuelSlots = EditorPrefs.GetInt("BatchItemSO_WorkstationFuelSlots", 1);
        
        // 存储
        storageCapacity = EditorPrefs.GetInt("BatchItemSO_StorageCapacity", 20);
        storageIsLockable = EditorPrefs.GetBool("BatchItemSO_StorageLockable", false);
        
        // 交互展示
        displayTitle = EditorPrefs.GetString("BatchItemSO_DisplayTitle", "");
        displayContent = EditorPrefs.GetString("BatchItemSO_DisplayContent", "");
        displayDuration = EditorPrefs.GetFloat("BatchItemSO_DisplayDuration", 0f);
        
        // 简单事件
        simpleEventType = (SimpleEventType)EditorPrefs.GetInt("BatchItemSO_EventType", 6);
        eventIsOneTime = EditorPrefs.GetBool("BatchItemSO_EventOneTime", false);
        eventCooldown = EditorPrefs.GetFloat("BatchItemSO_EventCooldown", 0f);
        
        // 钥匙
        keyMaterial = (MaterialTier)EditorPrefs.GetInt("BatchItemSO_KeyMaterial", 0);
        keyUnlockChance = EditorPrefs.GetFloat("BatchItemSO_KeyUnlockChance", 0.1f);
    }

    private void SaveSettings()
    {
        EditorPrefs.SetInt("BatchItemSO_MainCat", (int)mainCategory);
        EditorPrefs.SetInt("BatchItemSO_SubType", (int)soType);
        EditorPrefs.SetBool("BatchItemSO_SeqID", useSequentialID);
        EditorPrefs.SetInt("BatchItemSO_StartID", startID);
        EditorPrefs.SetString("BatchItemSO_Output", outputFolder);
        
        // 通用
        EditorPrefs.SetBool("BatchItemSO_SetPrice", setPrice);
        EditorPrefs.SetInt("BatchItemSO_BuyPrice", defaultBuyPrice);
        EditorPrefs.SetInt("BatchItemSO_SellPrice", defaultSellPrice);
        EditorPrefs.SetBool("BatchItemSO_SetStack", setMaxStack);
        EditorPrefs.SetInt("BatchItemSO_MaxStack", defaultMaxStack);
        EditorPrefs.SetBool("BatchItemSO_SetDisplaySize", setDisplaySize);
        EditorPrefs.SetInt("BatchItemSO_DisplaySize", displayPixelSize);
        
        // 工具
        EditorPrefs.SetInt("BatchItemSO_ToolType", (int)toolType);
        EditorPrefs.SetBool("BatchItemSO_SetToolEnergy", setToolEnergy);
        EditorPrefs.SetInt("BatchItemSO_ToolEnergy", toolEnergyCost);
        EditorPrefs.SetBool("BatchItemSO_SetToolRadius", setToolRadius);
        EditorPrefs.SetInt("BatchItemSO_ToolRadius", toolEffectRadius);
        EditorPrefs.SetBool("BatchItemSO_SetToolAnimFrames", setToolAnimFrames);
        EditorPrefs.SetInt("BatchItemSO_ToolAnimFrames", toolAnimFrameCount);
        
        // 武器
        EditorPrefs.SetInt("BatchItemSO_WeaponType", (int)weaponType);
        EditorPrefs.SetBool("BatchItemSO_SetWeaponAtk", setWeaponAttack);
        EditorPrefs.SetInt("BatchItemSO_WeaponAtk", weaponAttackPower);
        EditorPrefs.SetBool("BatchItemSO_SetWeaponSpeed", setWeaponSpeed);
        EditorPrefs.SetFloat("BatchItemSO_WeaponSpeed", weaponAttackSpeed);
        EditorPrefs.SetBool("BatchItemSO_SetWeaponCrit", setWeaponCrit);
        EditorPrefs.SetFloat("BatchItemSO_WeaponCrit", weaponCritChance);
        
        // 种子
        EditorPrefs.SetInt("BatchItemSO_SeedSeason", (int)seedSeason);
        EditorPrefs.SetBool("BatchItemSO_SetSeedGrowth", setSeedGrowth);
        EditorPrefs.SetInt("BatchItemSO_SeedGrowth", seedGrowthDays);
        EditorPrefs.SetBool("BatchItemSO_SetSeedHarvest", setSeedHarvest);
        EditorPrefs.SetInt("BatchItemSO_SeedHarvestID", seedHarvestCropID);
        
        // 树苗
        if (saplingTreePrefab != null)
            EditorPrefs.SetString("BatchItemSO_SaplingPrefab", AssetDatabase.GetAssetPath(saplingTreePrefab));
        else
            EditorPrefs.SetString("BatchItemSO_SaplingPrefab", "");
        EditorPrefs.SetBool("BatchItemSO_SetSaplingExp", setSaplingExp);
        EditorPrefs.SetInt("BatchItemSO_SaplingExp", saplingPlantingExp);
        
        // 作物
        EditorPrefs.SetBool("BatchItemSO_SetCropSeedID", setCropSeedID);
        EditorPrefs.SetInt("BatchItemSO_CropSeedID", cropSeedID);
        EditorPrefs.SetBool("BatchItemSO_SetCropExp", setCropExp);
        EditorPrefs.SetInt("BatchItemSO_CropExp", cropHarvestExp);
        
        // 食物
        EditorPrefs.SetBool("BatchItemSO_SetFoodEnergy", setFoodEnergy);
        EditorPrefs.SetInt("BatchItemSO_FoodEnergy", foodEnergyRestore);
        EditorPrefs.SetBool("BatchItemSO_SetFoodHealth", setFoodHealth);
        EditorPrefs.SetInt("BatchItemSO_FoodHealth", foodHealthRestore);
        EditorPrefs.SetInt("BatchItemSO_FoodBuff", (int)foodBuffType);
        
        // 材料
        EditorPrefs.SetInt("BatchItemSO_MatSubType", (int)materialSubType);
        EditorPrefs.SetBool("BatchItemSO_SetMatSmelt", setMaterialSmelt);
        EditorPrefs.SetBool("BatchItemSO_MatCanSmelt", materialCanSmelt);
        EditorPrefs.SetInt("BatchItemSO_MatSmeltID", materialSmeltResultID);
        
        // 药水
        EditorPrefs.SetBool("BatchItemSO_SetPotionHealth", setPotionHealth);
        EditorPrefs.SetInt("BatchItemSO_PotionHealth", potionHealthRestore);
        EditorPrefs.SetBool("BatchItemSO_SetPotionEnergy", setPotionEnergy);
        EditorPrefs.SetInt("BatchItemSO_PotionEnergy", potionEnergyRestore);
        EditorPrefs.SetInt("BatchItemSO_PotionBuff", (int)potionBuffType);
        
        // 工作台
        EditorPrefs.SetInt("BatchItemSO_WorkstationType", (int)workstationType);
        EditorPrefs.SetBool("BatchItemSO_WorkstationFuel", workstationRequiresFuel);
        EditorPrefs.SetInt("BatchItemSO_WorkstationFuelSlots", workstationFuelSlots);
        
        // 存储
        EditorPrefs.SetInt("BatchItemSO_StorageCapacity", storageCapacity);
        EditorPrefs.SetBool("BatchItemSO_StorageLockable", storageIsLockable);
        
        // 交互展示
        EditorPrefs.SetString("BatchItemSO_DisplayTitle", displayTitle);
        EditorPrefs.SetString("BatchItemSO_DisplayContent", displayContent);
        EditorPrefs.SetFloat("BatchItemSO_DisplayDuration", displayDuration);
        
        // 简单事件
        EditorPrefs.SetInt("BatchItemSO_EventType", (int)simpleEventType);
        EditorPrefs.SetBool("BatchItemSO_EventOneTime", eventIsOneTime);
        EditorPrefs.SetFloat("BatchItemSO_EventCooldown", eventCooldown);
        
        // 钥匙
        EditorPrefs.SetInt("BatchItemSO_KeyMaterial", (int)keyMaterial);
        EditorPrefs.SetFloat("BatchItemSO_KeyUnlockChance", keyUnlockChance);
    }

    #endregion
}
