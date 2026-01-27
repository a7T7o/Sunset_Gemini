using UnityEngine;
using UnityEditor;
using FarmGame.Data;
using System.IO;

/// <summary>
/// 自动创建物品数据库的编辑器工具
/// </summary>
public class AutoCreateItemDatabase : MonoBehaviour
{
    [MenuItem("Farm/Setup/创建主物品数据库", false, 1)]
    public static void CreateMasterDatabase()
    {
        string path = "Assets/111_Data/Database/MasterItemDatabase.asset";
        
        // 检查是否已存在
        ItemDatabase existing = AssetDatabase.LoadAssetAtPath<ItemDatabase>(path);
        if (existing != null)
        {
            Debug.LogWarning("[自动创建] 主数据库已存在，无需重复创建！");
            Selection.activeObject = existing;
            EditorGUIUtility.PingObject(existing);
            return;
        }

        // 确保文件夹存在
        string directory = Path.GetDirectoryName(path);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // 创建数据库实例
        ItemDatabase database = ScriptableObject.CreateInstance<ItemDatabase>();
        
        // 保存为Asset文件
        AssetDatabase.CreateAsset(database, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 选中新创建的数据库
        Selection.activeObject = database;
        EditorGUIUtility.PingObject(database);

        Debug.Log($"[自动创建] ✅ 成功创建主物品数据库: {path}");
        Debug.Log("[自动创建] 💡 提示：右键数据库可以使用'自动收集所有物品数据'功能");
    }

    [MenuItem("Farm/Setup/创建测试物品数据（5个示例）", false, 2)]
    public static void CreateTestItems()
    {
        int createdCount = 0;

        // 1. 创建铜锄头
        createdCount += CreateToolIfNotExists(
            "Assets/111_Data/Items/Tools/Tool_CopperHoe.asset",
            1, "铜锄头", "基础的农业工具，可以翻土",
            ToolType.Hoe, 2, 50
        );

        // 2. 创建番茄种子
        createdCount += CreateSeedIfNotExists(
            "Assets/111_Data/Items/Seeds/Seed_Tomato.asset",
            1001, "番茄种子", "春季作物，4天成熟",
            Season.Spring, 4, 1101, 50, 10
        );

        // 3. 创建番茄
        createdCount += CreateCropIfNotExists(
            "Assets/111_Data/Items/Crops/Crop_Tomato.asset",
            1101, "番茄", "新鲜的红番茄",
            1001, 80
        );

        // 4. 创建木剑
        createdCount += CreateWeaponIfNotExists(
            "Assets/111_Data/Items/Weapons/Weapon_WoodenSword.asset",
            201, "木剑", "简陋的武器，总比没有强",
            WeaponType.Sword, 10, 100
        );

        // 5. 创建史莱姆胶
        createdCount += CreateMaterialIfNotExists(
            "Assets/111_Data/Items/Materials/Material_SlimeGoo.asset",
            3301, "史莱姆胶", "黏糊糊的胶状物",
            MaterialSubType.Monster, 10
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (createdCount > 0)
        {
            Debug.Log($"[自动创建] ✅ 成功创建 {createdCount} 个测试物品");
            Debug.Log("[自动创建] 💡 现在可以运行'自动收集所有物品数据'将它们添加到数据库");
        }
        else
        {
            Debug.Log("[自动创建] ℹ️ 所有测试物品已存在，无需重复创建");
        }
    }

    private static int CreateToolIfNotExists(string path, int id, string name, string desc, 
        ToolType toolType, int energyCost, int sellPrice)
    {
        if (File.Exists(path))
        {
            Debug.Log($"[跳过] {name} 已存在");
            return 0;
        }

        EnsureDirectoryExists(path);

        ToolData tool = ScriptableObject.CreateInstance<ToolData>();
        tool.itemID = id;
        tool.itemName = name;
        tool.description = desc;
        tool.category = ItemCategory.Tool;
        tool.sellPrice = sellPrice;
        tool.maxStackSize = 1;
        tool.toolType = toolType;
        // toolLevel 已移除，工具品质通过 toolAnimId 和运行时 quality 参数控制
        tool.energyCost = energyCost;

        AssetDatabase.CreateAsset(tool, path);
        Debug.Log($"[创建] ✅ {name}");
        return 1;
    }

    private static int CreateSeedIfNotExists(string path, int id, string name, string desc,
        Season season, int growthDays, int harvestCropID, int buyPrice, int sellPrice)
    {
        if (File.Exists(path))
        {
            Debug.Log($"[跳过] {name} 已存在");
            return 0;
        }

        EnsureDirectoryExists(path);

        SeedData seed = ScriptableObject.CreateInstance<SeedData>();
        seed.itemID = id;
        seed.itemName = name;
        seed.description = desc;
        seed.category = ItemCategory.Plant;
        seed.buyPrice = buyPrice;
        seed.sellPrice = sellPrice;
        seed.maxStackSize = 99;
        seed.season = season;
        seed.growthDays = growthDays;
        seed.harvestCropID = harvestCropID;
        seed.harvestAmountRange = new Vector2Int(1, 3);

        AssetDatabase.CreateAsset(seed, path);
        Debug.Log($"[创建] ✅ {name}");
        return 1;
    }

    private static int CreateCropIfNotExists(string path, int id, string name, string desc,
        int seedID, int sellPrice)
    {
        if (File.Exists(path))
        {
            Debug.Log($"[跳过] {name} 已存在");
            return 0;
        }

        EnsureDirectoryExists(path);

        CropData crop = ScriptableObject.CreateInstance<CropData>();
        crop.itemID = id;
        crop.itemName = name;
        crop.description = desc;
        crop.category = ItemCategory.Plant;
        crop.sellPrice = sellPrice;
        crop.maxStackSize = 99;
        crop.seedID = seedID;

        AssetDatabase.CreateAsset(crop, path);
        Debug.Log($"[创建] ✅ {name}");
        return 1;
    }

    private static int CreateWeaponIfNotExists(string path, int id, string name, string desc,
        WeaponType weaponType, int attackPower, int sellPrice)
    {
        if (File.Exists(path))
        {
            Debug.Log($"[跳过] {name} 已存在");
            return 0;
        }

        EnsureDirectoryExists(path);

        WeaponData weapon = ScriptableObject.CreateInstance<WeaponData>();
        weapon.itemID = id;
        weapon.itemName = name;
        weapon.description = desc;
        weapon.category = ItemCategory.Tool;
        weapon.sellPrice = sellPrice;
        weapon.maxStackSize = 1;
        weapon.weaponType = weaponType;
        // 武器没有等级属性，品质通过后缀命名区分
        weapon.attackPower = attackPower;
        weapon.attackSpeed = 1.0f;
        weapon.criticalChance = 5f;

        AssetDatabase.CreateAsset(weapon, path);
        Debug.Log($"[创建] ✅ {name}");
        return 1;
    }

    private static int CreateMaterialIfNotExists(string path, int id, string name, string desc,
        MaterialSubType subType, int sellPrice)
    {
        if (File.Exists(path))
        {
            Debug.Log($"[跳过] {name} 已存在");
            return 0;
        }

        EnsureDirectoryExists(path);

        MaterialData material = ScriptableObject.CreateInstance<MaterialData>();
        material.itemID = id;
        material.itemName = name;
        material.description = desc;
        material.category = ItemCategory.Material;
        material.sellPrice = sellPrice;
        material.maxStackSize = 99;
        material.materialSubType = subType;
        material.sourceDescription = "击败怪物掉落";

        AssetDatabase.CreateAsset(material, path);
        Debug.Log($"[创建] ✅ {name}");
        return 1;
    }

    private static void EnsureDirectoryExists(string filePath)
    {
        string directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    [MenuItem("Farm/Setup/完整初始化（推荐）", false, 0)]
    public static void FullSetup()
    {
        Debug.Log("========================================");
        Debug.Log("[完整初始化] 开始自动配置物品系统...");
        Debug.Log("========================================");

        // 步骤1：创建主数据库
        Debug.Log("\n[步骤1/3] 创建主数据库...");
        CreateMasterDatabase();

        // 步骤2：创建测试物品
        Debug.Log("\n[步骤2/3] 创建测试物品...");
        CreateTestItems();

        // 步骤3：自动收集到数据库
        Debug.Log("\n[步骤3/3] 收集物品到数据库...");
        string dbPath = "Assets/111_Data/Database/MasterItemDatabase.asset";
        ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(dbPath);
        if (database != null)
        {
            // 调用自动收集功能（需要等待一帧让Asset完全加载）
            EditorApplication.delayCall += () =>
            {
                // 通过反射调用私有方法
                var method = typeof(ItemDatabase).GetMethod("AutoCollectAllItems", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(database, null);
                    EditorUtility.SetDirty(database);
                    AssetDatabase.SaveAssets();
                    Debug.Log("\n========================================");
                    Debug.Log("[完整初始化] ✅ 全部完成！");
                    Debug.Log("[完整初始化] 💡 请在Project窗口查看 Assets/111_Data/");
                    Debug.Log("========================================");
                }
            };
        }
    }
}

