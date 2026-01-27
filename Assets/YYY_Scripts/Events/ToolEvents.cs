using UnityEngine;
using System;
using System.Collections.Generic;

namespace FarmGame.Events
{
    /// <summary>
    /// 工具命中事件参数
    /// </summary>
    public class ToolStrikeEventArgs : EventArgs
    {
        /// <summary>攻击者（玩家）</summary>
        public GameObject attacker;
        
        /// <summary>工具物品ID</summary>
        public int toolItemId;
        
        /// <summary>工具品质</summary>
        public int toolQuality;
        
        /// <summary>动作状态（Slice=6, Crush=8 等）</summary>
        public int actionState;
        
        /// <summary>命中帧索引</summary>
        public int frameIndex;
        
        /// <summary>命中原点（玩家中心）</summary>
        public Vector2 origin;
        
        /// <summary>朝向方向</summary>
        public Vector2 forward;
        
        /// <summary>扇形角度（度）</summary>
        public float wedgeAngleDeg;
        
        /// <summary>命中半径</summary>
        public float reach;
        
        /// <summary>扇形内的候选碰撞体</summary>
        public IReadOnlyList<Collider2D> candidates;
    }

    /// <summary>
    /// 工具事件总线 - 全局工具相关事件
    /// </summary>
    public static class ToolEvents
    {
        /// <summary>
        /// 工具命中事件 - 当工具命中资源时触发
        /// </summary>
        public static event Action<ToolStrikeEventArgs> OnToolStrike;
        
        /// <summary>
        /// 触发工具命中事件
        /// </summary>
        public static void RaiseToolStrike(ToolStrikeEventArgs args)
        {
            OnToolStrike?.Invoke(args);
        }
        
        /// <summary>
        /// 资源被破坏事件 - 当资源（树/矿石）被完全破坏时触发
        /// </summary>
        public static event Action<GameObject, int> OnResourceDestroyed;
        
        /// <summary>
        /// 触发资源被破坏事件
        /// </summary>
        /// <param name="resource">被破坏的资源</param>
        /// <param name="resourceType">资源类型（0=树, 1=矿石）</param>
        public static void RaiseResourceDestroyed(GameObject resource, int resourceType)
        {
            OnResourceDestroyed?.Invoke(resource, resourceType);
        }
    }
}
