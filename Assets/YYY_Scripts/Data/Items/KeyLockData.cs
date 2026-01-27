using UnityEngine;

namespace FarmGame.Data
{
    /// <summary>
    /// 钥匙/锁数据类型
    /// </summary>
    public enum KeyLockType
    {
        Key = 0,    // 钥匙（用于解锁）
        Lock = 1    // 锁（用于上锁）
    }

    /// <summary>
    /// 钥匙和锁的数据 - 用于箱子上锁/解锁
    /// 
    /// 开锁概率系统（仅钥匙有效）：
    /// - 最终概率 = 钥匙开锁概率 + 箱子被开锁概率
    /// - 成功：钥匙保留
    /// - 失败：钥匙消耗
    /// </summary>
    [CreateAssetMenu(fileName = "KeyLock_New", menuName = "Farm/Items/KeyLock", order = 10)]
    public class KeyLockData : ItemData
    {
        [Header("=== 钥匙/锁专属属性 ===")]
        [Tooltip("类型：钥匙或锁")]
        public KeyLockType keyLockType = KeyLockType.Key;

        [Tooltip("材质类型（必须与箱子材质匹配）")]
        public ChestMaterial material = ChestMaterial.Wood;

        [Header("=== 开锁概率（仅钥匙有效）===")]
        [Tooltip("开锁概率 (0-1)，与箱子被开锁概率相加得到最终概率")]
        [Range(0f, 1f)]
        public float unlockChance = 0.1f;

        [Tooltip("是否为特殊钥匙/锁（用于特定箱子）")]
        public bool isSpecial = false;

        [Tooltip("特殊钥匙/锁对应的箱子ID（仅 isSpecial=true 时有效）")]
        public int targetChestId = -1;

        /// <summary>
        /// 根据材质获取默认开锁概率
        /// </summary>
        public static float GetDefaultUnlockChance(ChestMaterial chestMaterial)
        {
            return chestMaterial switch
            {
                ChestMaterial.Wood => 0.10f,
                ChestMaterial.Iron => 0.20f,
                ChestMaterial.Special => 0.40f,
                _ => 0.10f
            };
        }

        /// <summary>
        /// 根据材料等级获取默认开锁概率
        /// </summary>
        public static float GetDefaultUnlockChanceByTier(MaterialTier tier)
        {
            return tier switch
            {
                MaterialTier.Wood => 0.10f,
                MaterialTier.Stone => 0.15f,
                MaterialTier.Iron => 0.20f,
                MaterialTier.Brass => 0.25f,  // 黄铜 = 铜
                MaterialTier.Steel => 0.30f,
                MaterialTier.Gold => 0.40f,
                _ => 0.10f
            };
        }

        #region 验证

        protected override void OnValidate()
        {
            base.OnValidate();

            // 验证ID范围
            // 钥匙：1420-1429
            // 锁：1410-1419
            if (keyLockType == KeyLockType.Key)
            {
                if (itemID < 1420 || itemID >= 1500)
                {
                    Debug.LogWarning($"[{itemName}] 钥匙ID建议在1420-1499范围内！当前:{itemID}");
                }
            }
            else if (keyLockType == KeyLockType.Lock)
            {
                if (itemID < 1410 || itemID >= 1420)
                {
                    Debug.LogWarning($"[{itemName}] 锁ID建议在1410-1419范围内！当前:{itemID}");
                }
            }

            // 特殊钥匙/锁必须指定目标箱子
            if (isSpecial && targetChestId < 0)
            {
                Debug.LogWarning($"[{itemName}] 特殊钥匙/锁必须指定目标箱子ID！");
            }

            // 非特殊钥匙/锁不应该指定目标箱子
            if (!isSpecial && targetChestId >= 0)
            {
                targetChestId = -1;
                Debug.LogWarning($"[{itemName}] 非特殊钥匙/锁不应指定目标箱子ID，已清除");
            }
        }

        public override string GetTooltipText()
        {
            string text = base.GetTooltipText();
            
            string typeStr = keyLockType == KeyLockType.Key ? "钥匙" : "锁";
            string materialStr = material switch
            {
                ChestMaterial.Wood => "木质",
                ChestMaterial.Iron => "铁质",
                ChestMaterial.Special => "特殊",
                _ => "未知"
            };

            text += $"\n\n<color=cyan>类型: {materialStr}{typeStr}</color>";

            if (isSpecial)
            {
                text += $"\n<color=orange>特殊物品</color>";
            }

            return text;
        }

        #endregion
    }
}
