using UnityEngine;
using System.Collections.Generic;

namespace FarmGame.Data
{
    /// <summary>
    /// 单个掉落配置
    /// </summary>
    [System.Serializable]
    public class DropConfig
    {
        [Tooltip("掉落的物品")]
        public ItemData item;

        [Tooltip("最小掉落数量")]
        [Min(1)]
        public int minAmount = 1;

        [Tooltip("最大掉落数量")]
        [Min(1)]
        public int maxAmount = 3;

        [Tooltip("掉落概率 (0-1)")]
        [Range(0f, 1f)]
        public float dropChance = 1f;

        [Tooltip("品质 (0=普通, 1=铜, 2=银, 3=金, 4=彩)")]
        [Range(0, 4)]
        public int quality = 0;

        /// <summary>
        /// 计算实际掉落数量
        /// </summary>
        public int GetRandomAmount()
        {
            return Random.Range(minAmount, maxAmount + 1);
        }

        /// <summary>
        /// 检查是否触发掉落
        /// </summary>
        public bool RollDrop()
        {
            return Random.value <= dropChance;
        }
    }

    /// <summary>
    /// 掉落表 - 定义资源点的掉落规则
    /// </summary>
    [CreateAssetMenu(fileName = "DropTable_New", menuName = "Farm/Data/Drop Table", order = 50)]
    public class DropTable : ScriptableObject
    {
        [Header("掉落配置")]
        [Tooltip("掉落物品列表")]
        public List<DropConfig> drops = new List<DropConfig>();

        [Header("生成参数")]
        [Tooltip("掉落物品的散布半径")]
        [Range(0.1f, 2f)]
        public float spreadRadius = 0.5f;

        [Tooltip("是否保证至少掉落一个物品")]
        public bool guaranteeOneDrop = true;

        /// <summary>
        /// 生成掉落物品列表
        /// </summary>
        public List<DropResult> GenerateDrops()
        {
            var results = new List<DropResult>();
            bool hasDropped = false;

            foreach (var config in drops)
            {
                if (config.item == null) continue;

                if (config.RollDrop())
                {
                    results.Add(new DropResult
                    {
                        item = config.item,
                        amount = config.GetRandomAmount(),
                        quality = config.quality
                    });
                    hasDropped = true;
                }
            }

            // 保证至少掉落一个
            if (guaranteeOneDrop && !hasDropped && drops.Count > 0)
            {
                var firstValid = drops.Find(d => d.item != null);
                if (firstValid != null)
                {
                    results.Add(new DropResult
                    {
                        item = firstValid.item,
                        amount = firstValid.GetRandomAmount(),
                        quality = firstValid.quality
                    });
                }
            }

            return results;
        }
    }

    /// <summary>
    /// 掉落结果
    /// </summary>
    public struct DropResult
    {
        public ItemData item;
        public int amount;
        public int quality;
    }
}
