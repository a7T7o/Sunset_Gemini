using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace FarmGame.Data
{
    /// <summary>
    /// 物品数据库 - 统一管理所有物品数据
    /// 这是一个单例ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "MasterItemDatabase", menuName = "Farm/Database/Item Database", order = 100)]
    public class ItemDatabase : ScriptableObject
    {
        [Header("=== 所有物品数据 ===")]
        [Tooltip("手动拖入所有物品数据，或使用下方按钮自动收集")]
        public List<ItemData> allItems = new List<ItemData>();

        [Header("=== 配方数据 ===")]
        public List<RecipeData> allRecipes = new List<RecipeData>();

        // 运行时缓存（加速查询）
        private Dictionary<int, ItemData> itemDictionary;
        private Dictionary<int, RecipeData> recipeDictionary;

        /// <summary>
        /// 初始化数据库（游戏启动时调用）
        /// </summary>
        public void Initialize()
        {
            // 构建字典缓存
            itemDictionary = new Dictionary<int, ItemData>();
            foreach (var item in allItems)
            {
                if (item == null) continue;

                if (itemDictionary.ContainsKey(item.itemID))
                {
                    Debug.LogError($"物品ID冲突！ID {item.itemID} 被多个物品使用：{itemDictionary[item.itemID].itemName} 和 {item.itemName}");
                    continue;
                }

                itemDictionary.Add(item.itemID, item);
            }

            recipeDictionary = new Dictionary<int, RecipeData>();
            foreach (var recipe in allRecipes)
            {
                if (recipe == null) continue;
                if (!recipeDictionary.ContainsKey(recipe.recipeID))
                {
                    recipeDictionary.Add(recipe.recipeID, recipe);
                }
            }

            Debug.Log($"[ItemDatabase] 已加载 {itemDictionary.Count} 个物品，{recipeDictionary.Count} 个配方");
        }

        /// <summary>
        /// 通过ID获取物品数据
        /// </summary>
        public ItemData GetItemByID(int id)
        {
            // -1 表示空槽位，不需要警告
            if (id < 0)
                return null;

            if (itemDictionary == null)
                Initialize();

            if (itemDictionary.TryGetValue(id, out ItemData item))
                return item;

            Debug.LogWarning($"[ItemDatabase] 找不到ID为 {id} 的物品！");
            return null;
        }

        /// <summary>
        /// 通过ID获取配方数据
        /// </summary>
        public RecipeData GetRecipeByID(int id)
        {
            if (recipeDictionary == null)
                Initialize();

            if (recipeDictionary.TryGetValue(id, out RecipeData recipe))
                return recipe;

            Debug.LogWarning($"[ItemDatabase] 找不到ID为 {id} 的配方！");
            return null;
        }

        /// <summary>
        /// 获取指定类型的所有物品
        /// </summary>
        public List<ItemData> GetItemsByCategory(ItemCategory category)
        {
            return allItems.Where(item => item != null && item.category == category).ToList();
        }

        /// <summary>
        /// 获取所有种子数据
        /// </summary>
        public List<SeedData> GetAllSeeds()
        {
            return allItems.OfType<SeedData>().ToList();
        }

        /// <summary>
        /// 获取指定季节的种子
        /// </summary>
        public List<SeedData> GetSeedsBySeason(Season season)
        {
            return allItems.OfType<SeedData>()
                .Where(s => s.season == season || s.season == Season.AllSeason)
                .ToList();
        }

        /// <summary>
        /// 获取所有工具
        /// </summary>
        public List<ToolData> GetAllTools()
        {
            return allItems.OfType<ToolData>().ToList();
        }

        /// <summary>
        /// 获取所有武器
        /// </summary>
        public List<WeaponData> GetAllWeapons()
        {
            return allItems.OfType<WeaponData>().ToList();
        }

        /// <summary>
        /// 通过产物ID获取配方
        /// </summary>
        public List<RecipeData> GetRecipesByResult(int resultItemID)
        {
            return allRecipes.Where(r => r != null && r.resultItemID == resultItemID).ToList();
        }

        /// <summary>
        /// 获取在指定设施制作的所有配方
        /// </summary>
        public List<RecipeData> GetRecipesByStation(CraftingStation station)
        {
            return allRecipes.Where(r => r != null && r.requiredStation == station).ToList();
        }

#if UNITY_EDITOR
        [ContextMenu("自动收集所有物品数据")]
        private void AutoCollectAllItems()
        {
            allItems.Clear();

            // 搜索所有ItemData资产
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemData");
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                ItemData item = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (item != null)
                {
                    allItems.Add(item);
                }
            }

            // 按ID排序
            allItems = allItems.OrderBy(item => item.itemID).ToList();

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[ItemDatabase] 自动收集完成！共找到 {allItems.Count} 个物品");
        }

        [ContextMenu("自动收集所有配方数据")]
        private void AutoCollectAllRecipes()
        {
            allRecipes.Clear();

            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:RecipeData");
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                RecipeData recipe = UnityEditor.AssetDatabase.LoadAssetAtPath<RecipeData>(path);
                if (recipe != null)
                {
                    allRecipes.Add(recipe);
                }
            }

            allRecipes = allRecipes.OrderBy(r => r.recipeID).ToList();

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[ItemDatabase] 自动收集完成！共找到 {allRecipes.Count} 个配方");
        }

        [ContextMenu("验证所有数据完整性")]
        private void ValidateDatabase()
        {
            int errorCount = 0;
            HashSet<int> idSet = new HashSet<int>();

            // 检查ID冲突
            foreach (var item in allItems)
            {
                if (item == null) continue;

                if (idSet.Contains(item.itemID))
                {
                    Debug.LogError($"ID冲突！{item.itemID} - {item.itemName}");
                    errorCount++;
                }
                else
                {
                    idSet.Add(item.itemID);
                }
            }

            Debug.Log(errorCount == 0 
                ? "[ItemDatabase] 验证通过！✓" 
                : $"[ItemDatabase] 发现 {errorCount} 个错误！");
        }
#endif
    }
}

