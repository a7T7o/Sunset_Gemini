using UnityEngine;

namespace FarmGame.Data
{
    /// <summary>
    /// 技能数据结构
    /// 存储单个技能的等级和经验值
    /// </summary>
    [System.Serializable]
    public class SkillData
    {
        [Tooltip("技能类型")]
        public SkillType skillType;
        
        [Tooltip("当前等级")]
        [Min(1)]
        public int level = 1;
        
        [Tooltip("当前经验值")]
        [Min(0)]
        public int currentExperience = 0;
        
        /// <summary>
        /// 获取升级所需经验
        /// 公式：100 * level^1.5
        /// </summary>
        public int GetExperienceToNextLevel()
        {
            return Mathf.RoundToInt(100 * Mathf.Pow(level, 1.5f));
        }
        
        /// <summary>
        /// 获取当前等级进度（0-1）
        /// </summary>
        public float GetProgress()
        {
            int required = GetExperienceToNextLevel();
            if (required <= 0) return 1f;
            return Mathf.Clamp01((float)currentExperience / required);
        }
        
        /// <summary>
        /// 获取技能名称（中文）
        /// </summary>
        public string GetSkillName()
        {
            return skillType switch
            {
                SkillType.Combat => "战斗",
                SkillType.Gathering => "采集",
                SkillType.Crafting => "制作",
                SkillType.Cooking => "烹饪",
                SkillType.Fishing => "钓鱼",
                _ => "未知"
            };
        }
    }
}
