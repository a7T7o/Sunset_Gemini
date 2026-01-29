using UnityEngine;

namespace FarmGame.Data.Core
{
    /// <summary>
    /// 持久化对象接口
    /// 
    /// 所有需要存档的世界对象都必须实现此接口。
    /// 
    /// 设计原则：
    /// - 每个持久化对象有唯一的 GUID
    /// - 对象负责自己的序列化/反序列化逻辑
    /// - 支持场景切换时的状态保持
    /// 
    /// 实现示例：
    /// - ChestController: 存储箱子内容物
    /// - TreeController: 存储生长阶段、血量
    /// - CropController: 存储作物状态
    /// - PlacedItemController: 存储放置的物品
    /// </summary>
    public interface IPersistentObject
    {
        /// <summary>
        /// 对象唯一标识符（GUID）
        /// 
        /// 规则：
        /// - 在对象首次创建时生成
        /// - 存档时保存，读档时用于匹配
        /// - 场景中预放置的对象应在编辑器中预生成 GUID
        /// </summary>
        string PersistentId { get; }
        
        /// <summary>
        /// 对象类型标识
        /// 
        /// 用于反序列化时确定创建哪种类型的对象。
        /// 建议使用类名或自定义的类型字符串。
        /// 
        /// 示例：
        /// - "Chest"
        /// - "Tree"
        /// - "Crop"
        /// - "PlacedItem"
        /// </summary>
        string ObjectType { get; }
        
        /// <summary>
        /// 保存对象状态
        /// </summary>
        /// <returns>包含对象状态的存档数据</returns>
        WorldObjectSaveData Save();
        
        /// <summary>
        /// 加载对象状态
        /// </summary>
        /// <param name="data">存档数据</param>
        void Load(WorldObjectSaveData data);
        
        /// <summary>
        /// 对象是否应该被保存
        /// 
        /// 某些临时对象（如正在被销毁的对象）不应该被保存。
        /// </summary>
        bool ShouldSave { get; }
    }
    
    /// <summary>
    /// 持久化对象基类
    /// 
    /// 提供 IPersistentObject 的基础实现，
    /// 子类只需要重写 Save/Load 方法即可。
    /// </summary>
    public abstract class PersistentObjectBase : MonoBehaviour, IPersistentObject
    {
        [Header("持久化配置")]
        [SerializeField, Tooltip("对象唯一 ID（自动生成，勿手动修改）")]
        protected string persistentId;
        
        [SerializeField, Tooltip("是否在编辑器中预生成 ID")]
        protected bool preGenerateId = true;
        
        /// <summary>
        /// 对象唯一标识符
        /// </summary>
        public string PersistentId
        {
            get
            {
                if (string.IsNullOrEmpty(persistentId))
                {
                    persistentId = System.Guid.NewGuid().ToString();
                }
                return persistentId;
            }
        }
        
        /// <summary>
        /// 对象类型标识（子类必须实现）
        /// </summary>
        public abstract string ObjectType { get; }
        
        /// <summary>
        /// 是否应该被保存（默认为 true）
        /// </summary>
        public virtual bool ShouldSave => gameObject.activeInHierarchy;
        
        /// <summary>
        /// 保存对象状态（子类必须实现）
        /// </summary>
        public abstract WorldObjectSaveData Save();
        
        /// <summary>
        /// 加载对象状态（子类必须实现）
        /// </summary>
        public abstract void Load(WorldObjectSaveData data);
        
        /// <summary>
        /// 创建基础存档数据
        /// </summary>
        protected WorldObjectSaveData CreateBaseSaveData()
        {
            return new WorldObjectSaveData
            {
                guid = PersistentId,
                objectType = ObjectType,
                sceneName = gameObject.scene.name,
                isActive = gameObject.activeSelf
            };
        }
        
        /// <summary>
        /// 设置基础存档数据的位置信息
        /// </summary>
        protected void SetPositionData(WorldObjectSaveData data)
        {
            data.SetPosition(transform.position);
            data.rotationZ = transform.eulerAngles.z;
        }
        
        /// <summary>
        /// 从存档数据恢复位置
        /// </summary>
        protected void RestorePosition(WorldObjectSaveData data)
        {
            transform.position = data.GetPosition();
            transform.rotation = Quaternion.Euler(0, 0, data.rotationZ);
        }
        
        #region 编辑器支持
        
#if UNITY_EDITOR
        /// <summary>
        /// 在编辑器中预生成 ID
        /// </summary>
        protected virtual void OnValidate()
        {
            if (preGenerateId && string.IsNullOrEmpty(persistentId))
            {
                persistentId = System.Guid.NewGuid().ToString();
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
        
        /// <summary>
        /// 重新生成 ID（编辑器菜单）
        /// </summary>
        [ContextMenu("重新生成持久化 ID")]
        protected void RegeneratePersistentId()
        {
            persistentId = System.Guid.NewGuid().ToString();
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[{GetType().Name}] 已重新生成 ID: {persistentId}");
        }
#endif
        
        #endregion
    }
    
    /// <summary>
    /// 持久化对象注册表
    /// 
    /// 管理场景中所有持久化对象的注册和查找。
    /// 存档时遍历注册表保存所有对象，
    /// 读档时根据 GUID 匹配并恢复状态。
    /// </summary>
    public interface IPersistentObjectRegistry
    {
        /// <summary>
        /// 注册持久化对象
        /// </summary>
        void Register(IPersistentObject obj);
        
        /// <summary>
        /// 注销持久化对象
        /// </summary>
        void Unregister(IPersistentObject obj);
        
        /// <summary>
        /// 根据 GUID 查找对象
        /// </summary>
        IPersistentObject FindByGuid(string guid);
        
        /// <summary>
        /// 获取所有持久化对象
        /// </summary>
        System.Collections.Generic.IEnumerable<IPersistentObject> GetAll();
        
        /// <summary>
        /// 获取指定类型的所有对象
        /// </summary>
        System.Collections.Generic.IEnumerable<T> GetAllOfType<T>() where T : IPersistentObject;
    }
}
