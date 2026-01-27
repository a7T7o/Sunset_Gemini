using UnityEngine;

namespace FarmGame.Data
{
    /// <summary>
    /// 简单事件数据 - 交互后触发简单事件
    /// 包括：传送点、开关、触发器等
    /// </summary>
    [CreateAssetMenu(fileName = "SimpleEvent_New", menuName = "Farm/Placeable/Simple Event", order = 5)]
    public class SimpleEventData : PlaceableItemData
    {
        [Header("=== 事件配置 ===")]
        [Tooltip("事件类型")]
        public SimpleEventType eventType = SimpleEventType.ShowMessage;

        [Tooltip("事件参数（根据类型不同含义不同）")]
        [TextArea(2, 5)]
        public string eventParameter = "";

        [Header("=== 触发配置 ===")]
        [Tooltip("是否一次性触发")]
        public bool isOneTime = false;

        [Tooltip("冷却时间（秒）")]
        [Range(0f, 60f)]
        public float cooldownTime = 0f;

        [Tooltip("触发音效")]
        public AudioClip triggerSound;

        [Header("=== 预制体 ===")]
        [Tooltip("事件物预制体")]
        public GameObject eventPrefab;

        #region PlaceableItemData 实现

        public override PlacementType GetPlacementType() => PlacementType.SimpleEvent;

        public override GameObject GetPlacementPrefab() => eventPrefab;

        public override void OnPlaced(Vector3 position, GameObject instance)
        {
            base.OnPlaced(position, instance);
            
            // TODO: 初始化事件组件
            Debug.Log($"[SimpleEventData] 简单事件放置成功: {itemName} ({eventType})");
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 获取事件类型名称（中文）
        /// </summary>
        public string GetEventTypeName()
        {
            return eventType switch
            {
                SimpleEventType.PlaySound => "播放音效",
                SimpleEventType.PlayAnimation => "播放动画",
                SimpleEventType.Teleport => "传送",
                SimpleEventType.Unlock => "解锁",
                SimpleEventType.GiveItem => "给予物品",
                SimpleEventType.TriggerQuest => "触发任务",
                SimpleEventType.ShowMessage => "显示消息",
                _ => "未知事件"
            };
        }

        /// <summary>
        /// 获取触发描述
        /// </summary>
        public string GetTriggerDescription()
        {
            if (isOneTime)
                return "一次性触发";
            if (cooldownTime > 0)
                return $"冷却 {cooldownTime}秒";
            return "可重复触发";
        }

        #endregion

        #region 验证

        protected override void OnValidate()
        {
            base.OnValidate();

            // 验证简单事件ID范围（16XX - 简单事件类）
            if (itemID < 1600 || itemID >= 1700)
            {
                Debug.LogWarning($"[{itemName}] 简单事件ID建议在1600-1699范围内！当前:{itemID}");
            }

            // 验证预制体
            if (eventPrefab == null)
            {
                Debug.LogWarning($"[{itemName}] 缺少事件物预制体！");
            }

            // 验证事件参数
            if (RequiresParameter() && string.IsNullOrEmpty(eventParameter))
            {
                Debug.LogWarning($"[{itemName}] 事件类型 {eventType} 需要参数！");
            }
        }

        /// <summary>
        /// 检查事件类型是否需要参数
        /// </summary>
        private bool RequiresParameter()
        {
            return eventType switch
            {
                SimpleEventType.Teleport => true,      // 需要目标位置
                SimpleEventType.GiveItem => true,      // 需要物品ID和数量
                SimpleEventType.TriggerQuest => true,  // 需要任务ID
                SimpleEventType.ShowMessage => true,   // 需要消息内容
                _ => false
            };
        }

        public override string GetTooltipText()
        {
            string text = base.GetTooltipText();
            text += $"\n\n<color=magenta>事件: {GetEventTypeName()}</color>";
            text += $"\n<color=gray>{GetTriggerDescription()}</color>";

            return text;
        }

        #endregion
    }
}
