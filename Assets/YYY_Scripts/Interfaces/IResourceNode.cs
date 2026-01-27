using UnityEngine;
using FarmGame.Data;

namespace FarmGame.Combat
{
    /// <summary>
    /// 工具命中上下文
    /// </summary>
    public struct ToolHitContext
    {
        public int toolItemId;
        public int toolQuality;
        public ToolType toolType;  // 使用 FarmGame.Data.ToolType
        public int actionState;
        public Vector2 hitPoint;
        public Vector2 hitDir;
        public GameObject attacker;
        public float baseDamage;
        public int frameIndex;
    }

    /// <summary>
    /// 资源节点接口
    /// </summary>
    public interface IResourceNode
    {
        bool CanAccept(ToolHitContext ctx);
        void OnHit(ToolHitContext ctx);
        Bounds GetBounds();
        Vector3 GetPosition();
        bool IsDepleted { get; }
        string ResourceTag { get; }
        
        /// <summary>
        /// 获取碰撞体边界（用于精确命中检测）
        /// 返回 Collider bounds，无 Collider 时回退到 Sprite bounds
        /// </summary>
        Bounds GetColliderBounds();
    }
}
