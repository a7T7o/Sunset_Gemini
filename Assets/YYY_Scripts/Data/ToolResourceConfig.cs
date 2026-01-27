using UnityEngine;
using FarmGame.Combat;

namespace FarmGame.Data
{
    /// <summary>
    /// 工具-资源交互配置
    /// 定义特定工具对特定资源的交互行为
    /// </summary>
    [System.Serializable]
    public class ToolResourceConfig
    {
        [Header("匹配条件")]
        [Tooltip("工具类型")]
        public ToolType toolType;
        
        [Tooltip("资源标签（Tree, Rock 等）")]
        public string resourceTag;
        
        [Header("交互效果")]
        [Tooltip("是否造成伤害")]
        public bool dealsDamage = true;
        
        [Tooltip("是否消耗精力")]
        public bool consumesStamina = true;
        
        [Tooltip("是否播放抖动")]
        public bool playsShake = true;
        
        [Tooltip("是否生成粒子")]
        public bool spawnsParticles = true;
    }
    
    /// <summary>
    /// 工具-资源交互配置表
    /// 可作为 ScriptableObject 或直接在组件中配置
    /// </summary>
    [CreateAssetMenu(fileName = "ToolResourceInteractions", menuName = "Farm/Tool Resource Interactions")]
    public class ToolResourceInteractionTable : ScriptableObject
    {
        [Header("交互配置列表")]
        public ToolResourceConfig[] interactions;
        
        /// <summary>
        /// 查找匹配的配置
        /// </summary>
        public ToolResourceConfig FindConfig(ToolType toolType, string resourceTag)
        {
            if (interactions == null) return null;
            
            foreach (var config in interactions)
            {
                if (config.toolType == toolType && config.resourceTag == resourceTag)
                {
                    return config;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 检查工具是否对资源造成伤害
        /// </summary>
        public bool DoesDamage(ToolType toolType, string resourceTag)
        {
            var config = FindConfig(toolType, resourceTag);
            return config != null && config.dealsDamage;
        }
        
        /// <summary>
        /// 检查工具是否消耗精力
        /// </summary>
        public bool ConsumesStamina(ToolType toolType, string resourceTag)
        {
            var config = FindConfig(toolType, resourceTag);
            return config != null && config.consumesStamina;
        }
    }
}
