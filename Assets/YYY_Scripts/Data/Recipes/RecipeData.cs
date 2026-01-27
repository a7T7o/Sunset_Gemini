using UnityEngine;
using System.Collections.Generic;

namespace FarmGame.Data
{
    /// <summary>
    /// 配方材料项
    /// </summary>
    [System.Serializable]
    public class RecipeIngredient
    {
        [Tooltip("所需材料ID")]
        public int itemID;

        [Tooltip("所需数量")]
        public int amount;
    }

    /// <summary>
    /// 配方数据 - 烹饪、炼金、制作等配方
    /// </summary>
    [CreateAssetMenu(fileName = "Recipe_New", menuName = "Farm/Recipes/Recipe", order = 10)]
    public class RecipeData : ScriptableObject
    {
        [Header("=== 配方基础信息 ===")]
        [Tooltip("配方ID")]
        public int recipeID;

        [Tooltip("配方名称")]
        public string recipeName;

        [Tooltip("配方描述")]
        [TextArea(2, 3)]
        public string description;

        [Header("=== 产物 ===")]
        [Tooltip("制作出的物品ID")]
        public int resultItemID;

        [Tooltip("产出数量")]
        [Range(1, 10)]
        public int resultAmount = 1;

        [Header("=== 所需材料 ===")]
        [Tooltip("材料列表")]
        public List<RecipeIngredient> ingredients = new List<RecipeIngredient>();

        [Header("=== 制作条件 ===")]
        [Tooltip("需要的制作设施")]
        public CraftingStation requiredStation = CraftingStation.None;

        [Tooltip("需要的玩家等级")]
        public int requiredLevel = 1;

        [Tooltip("制作时间（秒，0=立即完成）")]
        public float craftingTime = 0f;

        [Header("=== 解锁条件 ===")]
        [Tooltip("是否默认解锁")]
        public bool unlockedByDefault = true;

        [Tooltip("解锁所需技能类型")]
        public SkillType requiredSkillType = SkillType.Crafting;
        
        [Tooltip("解锁所需技能等级")]
        public int requiredSkillLevel = 1;
        
        [Tooltip("是否为隐藏配方（非等级解锁，通过特殊方式解锁）")]
        public bool isHiddenRecipe = false;

        [Tooltip("解锁条件描述")]
        public string unlockCondition = "";
        
        /// <summary>
        /// 运行时解锁状态（非序列化）
        /// </summary>
        [System.NonSerialized]
        public bool isUnlocked = false;

        [Header("=== 奖励 ===")]
        [Tooltip("制作获得的经验")]
        public int craftingExp = 10;

        /// <summary>
        /// 检查玩家是否有足够材料（需要传入背包系统）
        /// </summary>
        public bool HasRequiredMaterials(System.Func<int, int> getItemCountFunc)
        {
            foreach (var ingredient in ingredients)
            {
                int playerHas = getItemCountFunc(ingredient.itemID);
                if (playerHas < ingredient.amount)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 获取配方文本（用于UI显示）
        /// </summary>
        public string GetRecipeText(System.Func<int, string> getItemNameFunc)
        {
            string text = $"<b>{recipeName}</b>\n\n";
            text += $"{description}\n\n";
            text += "<color=yellow>所需材料:</color>\n";

            foreach (var ingredient in ingredients)
            {
                string itemName = getItemNameFunc(ingredient.itemID);
                text += $"  • {itemName} x{ingredient.amount}\n";
            }

            if (requiredStation != CraftingStation.None)
                text += $"\n<color=cyan>设施: {GetStationName()}</color>";

            return text;
        }

        private string GetStationName()
        {
            return requiredStation switch
            {
                CraftingStation.CookingPot => "烹饪锅",
                CraftingStation.Furnace => "熔炉",
                CraftingStation.MagicTower => "魔法塔",
                CraftingStation.AnvilForge => "铁砧",
                CraftingStation.Workbench => "工作台",
                CraftingStation.AlchemyTable => "制药台",
                CraftingStation.Grill => "烧烤架",
                _ => "无"
            };
        }

        private void OnValidate()
        {
            if (ingredients.Count == 0)
            {
                Debug.LogWarning($"[{recipeName}] 配方没有材料！");
            }

            if (resultItemID == 0)
            {
                Debug.LogWarning($"[{recipeName}] 配方没有设置产物！");
            }
        }
    }
}

