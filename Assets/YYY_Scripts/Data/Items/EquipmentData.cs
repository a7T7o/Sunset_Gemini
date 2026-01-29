using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmGame.Data
{
    /// <summary>
    /// 装备数据类 - 继承 ItemData，添加装备专属属性
    /// 用于头盔、盔甲、裤子、鞋子、戒指等装备类物品
    /// </summary>
    [CreateAssetMenu(fileName = "Equipment_", menuName = "Game/Items/EquipmentData")]
    public class EquipmentData : ItemData
    {
        [Header("=== 装备专属属性 ===")]
        
        [Tooltip("装备类型（决定可装备的槽位）")]
        public new EquipmentType equipmentType = EquipmentType.None;
        
        [Tooltip("防御力")]
        [Range(0, 999)]
        public int defense = 0;
        
        [Tooltip("属性加成列表（预留给力量/敏捷等）")]
        public List<StatModifier> attributes = new List<StatModifier>();
        
        [Tooltip("装备模型（预留给纸娃娃系统）")]
        public GameObject equipmentModel;

        /// <summary>
        /// 获取装备的总防御力（基础 + 属性加成）
        /// </summary>
        public int GetTotalDefense()
        {
            int total = defense;
            foreach (var mod in attributes)
            {
                if (mod.statType == StatType.Defense)
                {
                    if (mod.modifierType == ModifierType.Flat)
                        total += Mathf.RoundToInt(mod.value);
                    else
                        total = Mathf.RoundToInt(total * (1 + mod.value / 100f));
                }
            }
            return total;
        }

        /// <summary>
        /// 获取指定属性的加成值
        /// </summary>
        public float GetStatBonus(StatType statType)
        {
            float flatBonus = 0f;
            float percentBonus = 0f;
            
            foreach (var mod in attributes)
            {
                if (mod.statType == statType)
                {
                    if (mod.modifierType == ModifierType.Flat)
                        flatBonus += mod.value;
                    else
                        percentBonus += mod.value;
                }
            }
            
            return flatBonus * (1 + percentBonus / 100f);
        }

        /// <summary>
        /// 验证装备数据完整性
        /// </summary>
        protected override void OnValidate()
        {
            base.OnValidate();
            
            // 装备类型不能为 None
            if (equipmentType == EquipmentType.None)
            {
                Debug.LogWarning($"[{itemName}] 装备类型为 None，请设置正确的装备类型！");
            }
            
            // 装备不可堆叠
            if (maxStackSize > 1)
            {
                maxStackSize = 1;
                Debug.LogWarning($"[{itemName}] 装备不可堆叠，已自动设置 maxStackSize = 1");
            }
            
            // 防御力不能为负
            if (defense < 0)
            {
                defense = 0;
                Debug.LogWarning($"[{itemName}] 防御力不能为负，已重置为 0");
            }
        }

        /// <summary>
        /// 获取装备的完整信息文本（用于Tooltip显示）
        /// </summary>
        public override string GetTooltipText()
        {
            string text = $"<b>{itemName}</b>\n";
            text += $"<color=#888888>{GetEquipmentTypeName()}</color>\n\n";
            text += description;
            
            // 显示防御力
            if (defense > 0)
            {
                text += $"\n\n<color=#4488ff>防御力: +{defense}</color>";
            }
            
            // 显示属性加成
            foreach (var mod in attributes)
            {
                string modText = mod.modifierType == ModifierType.Flat 
                    ? $"+{mod.value:F0}" 
                    : $"+{mod.value:F0}%";
                text += $"\n<color=#44ff88>{GetStatTypeName(mod.statType)}: {modText}</color>";
            }
            
            // 显示价格
            if (sellPrice > 0)
                text += $"\n\n<color=yellow>售价: {sellPrice}金币</color>";
            
            if (buyPrice > 0)
                text += $"\n<color=yellow>购买: {buyPrice}金币</color>";

            return text;
        }

        /// <summary>
        /// 获取装备类型名称（中文）
        /// </summary>
        private string GetEquipmentTypeName()
        {
            return equipmentType switch
            {
                EquipmentType.Helmet => "头盔",
                EquipmentType.Armor => "盔甲",
                EquipmentType.Pants => "裤子",
                EquipmentType.Shoes => "鞋子",
                EquipmentType.Ring => "戒指",
                EquipmentType.Accessory => "饰品",
                _ => "装备"
            };
        }

        /// <summary>
        /// 获取属性类型名称（中文）
        /// </summary>
        private string GetStatTypeName(StatType statType)
        {
            return statType switch
            {
                StatType.Strength => "力量",
                StatType.Agility => "敏捷",
                StatType.Intelligence => "智力",
                StatType.Vitality => "体力",
                StatType.Luck => "幸运",
                StatType.Defense => "防御",
                StatType.Attack => "攻击",
                StatType.MaxHealth => "生命上限",
                StatType.MaxMana => "魔力上限",
                StatType.CritChance => "暴击率",
                StatType.CritDamage => "暴击伤害",
                _ => "未知"
            };
        }
    }

    /// <summary>
    /// 属性修饰符 - 用于装备的属性加成
    /// </summary>
    [Serializable]
    public class StatModifier
    {
        [Tooltip("属性类型")]
        public StatType statType = StatType.Strength;
        
        [Tooltip("加成数值")]
        public float value = 0f;
        
        [Tooltip("加成类型（固定值/百分比）")]
        public ModifierType modifierType = ModifierType.Flat;
    }

    /// <summary>
    /// 属性类型枚举
    /// </summary>
    public enum StatType
    {
        Strength,       // 力量
        Agility,        // 敏捷
        Intelligence,   // 智力
        Vitality,       // 体力
        Luck,           // 幸运
        Defense,        // 防御
        Attack,         // 攻击
        MaxHealth,      // 生命上限
        MaxMana,        // 魔力上限
        CritChance,     // 暴击率
        CritDamage      // 暴击伤害
    }

    /// <summary>
    /// 加成类型枚举
    /// </summary>
    public enum ModifierType
    {
        Flat,       // 固定值加成
        Percent     // 百分比加成
    }
}
