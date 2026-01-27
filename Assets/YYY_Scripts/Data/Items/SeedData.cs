using UnityEngine;

namespace FarmGame.Data
{
    /// <summary>
    /// 种子数据 - 可种植的种子
    /// </summary>
    [CreateAssetMenu(fileName = "Seed_New", menuName = "Farm/Items/Seed", order = 1)]
    public class SeedData : ItemData
    {
        [Header("=== 种植专属属性 ===")]
        [Tooltip("生长所需天数")]
        [Range(1, 28)]
        public int growthDays = 4;

        [Tooltip("适合种植的季节")]
        public Season season = Season.Spring;

        [Tooltip("收获的作物ID")]
        public int harvestCropID;

        [Tooltip("收获数量范围")]
        public Vector2Int harvestAmountRange = new Vector2Int(1, 1);

        [Tooltip("是否可以重复收获（如草莓、蓝莓）")]
        public bool isReHarvestable = false;

        [Tooltip("重复收获间隔天数（仅当可重复收获时有效）")]
        [Range(1, 14)]
        public int reHarvestDays = 2;

        [Tooltip("总共可收获次数（0=无限次）")]
        public int maxHarvestCount = 0;

        [Header("=== 生长阶段Sprite ===")]
        [Tooltip("生长阶段图（按顺序：种子→小苗→成长→成熟）")]
        public Sprite[] growthStageSprites;

        [Header("=== 种植需求 ===")]
        [Tooltip("是否需要支架/棚架")]
        public bool needsTrellis = false;

        [Tooltip("需要保持湿润（否则生长停滞）")]
        public bool needsWatering = true;

        [Tooltip("种植经验值（种植时获得）")]
        public int plantingExp = 5;

        [Tooltip("收获经验值")]
        public int harvestingExp = 10;

        /// <summary>
        /// 验证种子数据
        /// </summary>
        protected override void OnValidate()
        {
            base.OnValidate();

            // 验证种子ID范围（10XX）
            if (itemID < 1000 || itemID >= 2000)
            {
                Debug.LogWarning($"[{itemName}] 种子ID应在1000-1999范围内！当前:{itemID}");
            }

            // 验证作物ID
            if (harvestCropID < 1100 || harvestCropID >= 1200)
            {
                Debug.LogWarning($"[{itemName}] 收获作物ID应在1100-1199范围内！当前:{harvestCropID}");
            }

            // 验证生长阶段Sprite
            if (growthStageSprites == null || growthStageSprites.Length < 3)
            {
                Debug.LogWarning($"[{itemName}] 至少需要3个生长阶段Sprite！");
            }

            // 验证收获范围
            if (harvestAmountRange.x > harvestAmountRange.y)
            {
                Debug.LogWarning($"[{itemName}] 收获数量范围错误！");
                harvestAmountRange.y = harvestAmountRange.x;
            }
        }

        public override string GetTooltipText()
        {
            string text = base.GetTooltipText();
            text += $"\n\n<color=green>生长周期: {growthDays}天</color>";
            text += $"\n<color=green>季节: {GetSeasonName(season)}</color>";
            
            if (isReHarvestable)
                text += $"\n<color=cyan>可重复收获（每{reHarvestDays}天）</color>";

            return text;
        }

        private string GetSeasonName(Season s)
        {
            return s switch
            {
                Season.Spring => "春季",
                Season.Summer => "夏季",
                Season.Fall => "秋季",
                Season.Winter => "冬季",
                Season.AllSeason => "全季节",
                _ => "未知"
            };
        }
    }
}

