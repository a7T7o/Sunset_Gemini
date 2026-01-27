using UnityEngine;

namespace FarmGame.UI
{
    /// <summary>
    /// 物品丢弃辅助类 - 统一所有丢弃逻辑
    /// 
    /// 【使用方式】
    /// ItemDropHelper.DropAtPlayer(item);
    /// ItemDropHelper.DropAtPlayer(item, cooldown: 3f);
    /// 
    /// 【丢弃位置】
    /// 使用玩家 Collider 中心作为丢弃位置（符合项目规范）
    /// 
    /// 【设计原则】
    /// 1. DRY - 消除三处重复的 SpawnById 调用
    /// 2. 内部处理 setSpawnCooldown: false
    /// 3. 缓存 PlayerController 引用
    /// </summary>
    public static class ItemDropHelper
    {
        // 缓存 PlayerController 引用
        private static PlayerController _cachedPlayer;
        
        /// <summary>
        /// 在玩家位置丢弃物品
        /// </summary>
        /// <param name="item">要丢弃的物品</param>
        /// <param name="cooldown">拾取冷却时间（秒），默认 5 秒</param>
        public static void DropAtPlayer(ItemStack item, float cooldown = 5f)
        {
            if (item.IsEmpty)
            {
                return;
            }
            
            // 获取玩家位置（使用 Collider 中心）
            Vector3 dropPos = GetPlayerDropPosition();
            if (dropPos == Vector3.zero)
            {
                Debug.LogError("[ItemDropHelper] 无法获取玩家位置，物品将丢失！");
                return;
            }
            
            // 生成世界物品
            SpawnWorldItem(item, dropPos, cooldown);
        }
        
        /// <summary>
        /// 在指定位置丢弃物品
        /// </summary>
        public static void DropAtPosition(ItemStack item, Vector3 position, float cooldown = 5f)
        {
            if (item.IsEmpty)
            {
                return;
            }
            
            SpawnWorldItem(item, position, cooldown);
        }
        
        /// <summary>
        /// 获取玩家丢弃位置（Collider 中心）
        /// 🔴 使用 Collider 中心（符合项目规范 rules.md）
        /// </summary>
        private static Vector3 GetPlayerDropPosition()
        {
            // 使用缓存的 PlayerController
            if (_cachedPlayer == null)
            {
                _cachedPlayer = Object.FindFirstObjectByType<PlayerController>();
            }
            
            if (_cachedPlayer == null)
            {
                Debug.LogError("[ItemDropHelper] 找不到 PlayerController");
                return Vector3.zero;
            }
            
            // 🔴 使用 Collider 中心（符合项目规范）
            var collider = _cachedPlayer.GetComponent<Collider2D>();
            if (collider != null)
            {
                return collider.bounds.center;
            }
            
            // 回退到 Transform 位置
            return _cachedPlayer.transform.position;
        }
        
        /// <summary>
        /// 生成世界物品
        /// 🔥 内部处理 setSpawnCooldown: false，外部调用者无需关心
        /// </summary>
        private static void SpawnWorldItem(ItemStack item, Vector3 position, float cooldown)
        {
            if (WorldItemPool.Instance == null)
            {
                Debug.LogError("[ItemDropHelper] WorldItemPool.Instance 为 null，物品将丢失！");
                return;
            }
            
            // 🔥 统一调用 SpawnById，内部处理所有参数
            // 参数：playAnimation=true（播放动画），setSpawnCooldown=false（由 SetDropCooldown 单独设置）
            var pickup = WorldItemPool.Instance.SpawnById(
                item.itemId, 
                item.quality, 
                item.amount, 
                position, 
                playAnimation: true, 
                setSpawnCooldown: false
            );
            
            if (pickup != null)
            {
                pickup.SetDropCooldown(cooldown);
            }
            else
            {
                Debug.LogError($"[ItemDropHelper] 生成物品失败: itemId={item.itemId}");
            }
        }
        
        /// <summary>
        /// 清除缓存（场景切换时调用）
        /// </summary>
        public static void ClearCache()
        {
            _cachedPlayer = null;
        }
    }
}
