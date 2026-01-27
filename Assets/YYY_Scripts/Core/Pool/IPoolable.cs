namespace Sunset.Pool
{
    /// <summary>
    /// 可池化对象接口
    /// 实现此接口的对象可以被对象池管理
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// 从池中取出时调用
        /// 用于初始化/重置对象状态
        /// </summary>
        void OnSpawn();
        
        /// <summary>
        /// 返回池中时调用
        /// 用于清理对象状态
        /// </summary>
        void OnDespawn();
    }
}
