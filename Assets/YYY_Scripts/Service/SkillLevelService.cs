using UnityEngine;
using System;

namespace FarmGame.Data
{
    /// <summary>
    /// 技能等级服务
    /// 管理玩家的5种独立技能的经验获取和等级计算
    /// </summary>
    public class SkillLevelService : MonoBehaviour
    {
        #region 单例
        public static SkillLevelService Instance { get; private set; }
        #endregion
        
        #region 常量
        private const int SKILL_COUNT = 5;
        #endregion
        
        #region 序列化字段
        [Header("━━━━ 技能数据 ━━━━")]
        [Tooltip("5种技能的数据")]
        [SerializeField] private SkillData[] skills = new SkillData[SKILL_COUNT];
        
        [Header("━━━━ 配置 ━━━━")]
        [Tooltip("最大等级")]
        [SerializeField] private int maxLevel = 10;
        
        [Header("━━━━ 音效 ━━━━")]
        [Tooltip("升级音效")]
        [SerializeField] private AudioClip levelUpSound;
        
        [Tooltip("音效音量")]
        [Range(0f, 1f)]
        [SerializeField] private float soundVolume = 0.8f;
        
        [Header("━━━━ 调试 ━━━━")]
        [SerializeField] private bool showDebugInfo = false;
        #endregion
        
        #region 事件
        /// <summary>获得经验事件 (技能类型, 获得经验)</summary>
        public event Action<SkillType, int> OnExperienceGained;
        
        /// <summary>升级事件 (技能类型, 新等级)</summary>
        public event Action<SkillType, int> OnLevelUp;
        #endregion
        
        #region Unity 生命周期
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeSkills();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        #endregion
        
        #region 初始化
        /// <summary>
        /// 初始化技能数据
        /// </summary>
        private void InitializeSkills()
        {
            if (skills == null || skills.Length != SKILL_COUNT)
            {
                skills = new SkillData[SKILL_COUNT];
            }
            
            for (int i = 0; i < SKILL_COUNT; i++)
            {
                if (skills[i] == null)
                {
                    skills[i] = new SkillData
                    {
                        skillType = (SkillType)i,
                        level = 1,
                        currentExperience = 0
                    };
                }
                else
                {
                    // 确保技能类型正确
                    skills[i].skillType = (SkillType)i;
                }
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"<color=cyan>[SkillLevelService] 初始化完成，共 {SKILL_COUNT} 种技能</color>");
            }
        }
        #endregion
        
        #region 公共方法 - 经验操作
        /// <summary>
        /// 添加经验
        /// </summary>
        /// <param name="skillType">技能类型</param>
        /// <param name="amount">经验数量</param>
        public void AddExperience(SkillType skillType, int amount)
        {
            if (amount <= 0) return;
            
            var skill = GetSkillData(skillType);
            if (skill == null) return;
            
            // 已达最大等级
            if (skill.level >= maxLevel)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"<color=gray>[SkillLevelService] {skill.GetSkillName()} 已达最大等级 {maxLevel}</color>");
                }
                return;
            }
            
            skill.currentExperience += amount;
            OnExperienceGained?.Invoke(skillType, amount);
            
            if (showDebugInfo)
            {
                Debug.Log($"<color=yellow>[SkillLevelService] {skill.GetSkillName()} +{amount} 经验 ({skill.currentExperience}/{skill.GetExperienceToNextLevel()})</color>");
            }
            
            // 检查升级
            CheckLevelUp(skill);
        }
        
        /// <summary>
        /// 检查并处理升级
        /// </summary>
        private void CheckLevelUp(SkillData skill)
        {
            while (skill.currentExperience >= skill.GetExperienceToNextLevel() && skill.level < maxLevel)
            {
                // 扣除升级所需经验
                skill.currentExperience -= skill.GetExperienceToNextLevel();
                skill.level++;
                
                // 播放升级音效
                PlayLevelUpSound();
                
                // 触发升级事件
                OnLevelUp?.Invoke(skill.skillType, skill.level);
                
                Debug.Log($"<color=lime>[SkillLevelService] 🎉 {skill.GetSkillName()} 升级到 Lv.{skill.level}！</color>");
            }
        }
        #endregion
        
        #region 公共方法 - 查询
        /// <summary>
        /// 获取技能等级
        /// </summary>
        public int GetLevel(SkillType skillType)
        {
            var skill = GetSkillData(skillType);
            return skill?.level ?? 1;
        }
        
        /// <summary>
        /// 获取技能当前经验
        /// </summary>
        public int GetExperience(SkillType skillType)
        {
            var skill = GetSkillData(skillType);
            return skill?.currentExperience ?? 0;
        }
        
        /// <summary>
        /// 获取升级所需经验
        /// </summary>
        public int GetExperienceToNextLevel(SkillType skillType)
        {
            var skill = GetSkillData(skillType);
            return skill?.GetExperienceToNextLevel() ?? 100;
        }
        
        /// <summary>
        /// 获取技能进度（0-1）
        /// </summary>
        public float GetProgress(SkillType skillType)
        {
            var skill = GetSkillData(skillType);
            return skill?.GetProgress() ?? 0f;
        }
        
        /// <summary>
        /// 获取所有技能数据（只读）
        /// </summary>
        public SkillData[] GetAllSkills()
        {
            return skills;
        }
        #endregion
        
        #region 私有方法
        /// <summary>
        /// 获取技能数据
        /// </summary>
        private SkillData GetSkillData(SkillType skillType)
        {
            int index = (int)skillType;
            if (index >= 0 && index < skills.Length)
            {
                return skills[index];
            }
            
            Debug.LogWarning($"[SkillLevelService] 无效的技能类型: {skillType}");
            return null;
        }
        
        /// <summary>
        /// 播放升级音效
        /// </summary>
        private void PlayLevelUpSound()
        {
            if (levelUpSound != null && Camera.main != null)
            {
                AudioSource.PlayClipAtPoint(levelUpSound, Camera.main.transform.position, soundVolume);
            }
        }
        #endregion
        
        #region 编辑器调试
        #if UNITY_EDITOR
        [ContextMenu("调试 - 添加10点采集经验")]
        private void DEBUG_AddGatheringXP()
        {
            AddExperience(SkillType.Gathering, 10);
        }
        
        [ContextMenu("调试 - 添加100点采集经验")]
        private void DEBUG_AddGatheringXP100()
        {
            AddExperience(SkillType.Gathering, 100);
        }
        
        [ContextMenu("调试 - 显示所有技能状态")]
        private void DEBUG_ShowAllSkills()
        {
            foreach (var skill in skills)
            {
                Debug.Log($"[SkillLevelService] {skill.GetSkillName()} Lv.{skill.level} ({skill.currentExperience}/{skill.GetExperienceToNextLevel()})");
            }
        }
        #endif
        #endregion
    }

}