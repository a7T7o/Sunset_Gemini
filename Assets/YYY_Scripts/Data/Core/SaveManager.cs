using System;
using System.IO;
using UnityEngine;

namespace FarmGame.Data.Core
{
    /// <summary>
    /// 存档管理器 (MVP 版本)
    /// 
    /// 职责：
    /// - 协调存档/读档流程
    /// - 收集全局数据（时间、玩家）
    /// - 序列化/反序列化 JSON
    /// - 文件读写
    /// 
    /// 本阶段简化：
    /// - 只做当前场景内的状态恢复（不换场景）
    /// - 使用 Unity JsonUtility（简单但有限制）
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        #region 单例
        
        private static SaveManager _instance;
        
        public static SaveManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<SaveManager>();
                    
                    if (_instance == null)
                    {
                        var go = new GameObject("[SaveManager]");
                        _instance = go.AddComponent<SaveManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region 配置
        
        [Header("存档配置")]
        [SerializeField] private string saveFileExtension = ".json";
        [SerializeField] private string saveFolder = "Save";
        
        [Header("调试")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool prettyPrintJson = true;
        
        #endregion
        
        #region 属性
        
        /// <summary>
        /// 存档目录路径（Assets/Save）
        /// </summary>
        public string SaveFolderPath
        {
            get
            {
#if UNITY_EDITOR
                // 编辑器模式：使用 Assets 目录
                return Path.Combine(Application.dataPath, saveFolder);
#else
                // 打包后：使用游戏根目录
                return Path.Combine(Application.dataPath, "..", saveFolder);
#endif
            }
        }
        
        /// <summary>
        /// 当前加载的存档数据（用于调试）
        /// </summary>
        public GameSaveData CurrentSaveData { get; private set; }
        
        #endregion
        
        #region Unity 生命周期
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 确保存档目录存在
            EnsureSaveFolderExists();
            
            if (showDebugInfo)
                Debug.Log($"[SaveManager] 初始化完成，存档路径: {SaveFolderPath}");
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
        
        #endregion
        
        #region 核心 API
        
        /// <summary>
        /// 保存游戏
        /// </summary>
        /// <param name="slotName">存档槽名称（如 "slot1", "autosave"）</param>
        /// <returns>是否成功</returns>
        public bool SaveGame(string slotName)
        {
            if (string.IsNullOrEmpty(slotName))
            {
                Debug.LogError("[SaveManager] 存档名称不能为空");
                return false;
            }
            
            try
            {
                // 1. 创建存档数据结构
                var saveData = new GameSaveData();
                saveData.lastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                
                // 2. 收集游戏时间数据
                saveData.gameTime = CollectGameTimeData();
                
                // 3. 收集玩家数据
                saveData.player = CollectPlayerData();
                
                // 4. 收集背包数据
                saveData.inventory = CollectInventoryData();
                
                // 5. 收集世界对象数据（通过 Registry）
                if (PersistentObjectRegistry.Instance != null)
                {
                    saveData.worldObjects = PersistentObjectRegistry.Instance.CollectAllSaveData();
                }
                
                // 6. 序列化为 JSON
                string json = prettyPrintJson 
                    ? JsonUtility.ToJson(saveData, true) 
                    : JsonUtility.ToJson(saveData);
                
                // 7. 写入文件
                string filePath = GetSaveFilePath(slotName);
                File.WriteAllText(filePath, json);
                
                CurrentSaveData = saveData;
                
                if (showDebugInfo)
                    Debug.Log($"[SaveManager] 保存成功: {filePath}, 世界对象: {saveData.worldObjects?.Count ?? 0}");
                
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] 保存失败: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }
        
        /// <summary>
        /// 加载游戏
        /// </summary>
        /// <param name="slotName">存档槽名称</param>
        /// <returns>是否成功</returns>
        public bool LoadGame(string slotName)
        {
            if (string.IsNullOrEmpty(slotName))
            {
                Debug.LogError("[SaveManager] 存档名称不能为空");
                return false;
            }
            
            string filePath = GetSaveFilePath(slotName);
            
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[SaveManager] 存档文件不存在: {filePath}");
                return false;
            }
            
            try
            {
                // 1. 读取文件
                string json = File.ReadAllText(filePath);
                
                // 2. 反序列化
                var saveData = JsonUtility.FromJson<GameSaveData>(json);
                
                if (saveData == null)
                {
                    Debug.LogError("[SaveManager] 存档数据解析失败");
                    return false;
                }
                
                // 3. 恢复游戏时间
                RestoreGameTimeData(saveData.gameTime);
                
                // 4. 恢复玩家数据
                RestorePlayerData(saveData.player);
                
                // 5. 恢复背包数据
                RestoreInventoryData(saveData.inventory);
                
                // 6. 恢复世界对象数据
                if (PersistentObjectRegistry.Instance != null && saveData.worldObjects != null)
                {
                    PersistentObjectRegistry.Instance.RestoreAllFromSaveData(saveData.worldObjects);
                }
                
                CurrentSaveData = saveData;
                
                if (showDebugInfo)
                    Debug.Log($"[SaveManager] 加载成功: {filePath}, 世界对象: {saveData.worldObjects?.Count ?? 0}");
                
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] 加载失败: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }
        
        /// <summary>
        /// 检查存档是否存在
        /// </summary>
        public bool SaveExists(string slotName)
        {
            return File.Exists(GetSaveFilePath(slotName));
        }
        
        /// <summary>
        /// 删除存档
        /// </summary>
        public bool DeleteSave(string slotName)
        {
            string filePath = GetSaveFilePath(slotName);
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                if (showDebugInfo)
                    Debug.Log($"[SaveManager] 删除存档: {filePath}");
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 获取所有存档槽名称
        /// </summary>
        public string[] GetAllSaveSlots()
        {
            if (!Directory.Exists(SaveFolderPath))
                return Array.Empty<string>();
            
            var files = Directory.GetFiles(SaveFolderPath, "*" + saveFileExtension);
            var slots = new string[files.Length];
            
            for (int i = 0; i < files.Length; i++)
            {
                slots[i] = Path.GetFileNameWithoutExtension(files[i]);
            }
            
            return slots;
        }
        
        #endregion
        
        #region 数据收集
        
        /// <summary>
        /// 收集游戏时间数据
        /// Rule: P1-2 时间恢复 - 从 TimeManager 获取实际时间
        /// </summary>
        private GameTimeSaveData CollectGameTimeData()
        {
            var data = new GameTimeSaveData();
            
            // 从 TimeManager 获取数据
            if (TimeManager.Instance != null)
            {
                data.day = TimeManager.Instance.GetDay();
                data.season = (int)TimeManager.Instance.GetSeason();
                data.year = TimeManager.Instance.GetYear();
                data.hour = TimeManager.Instance.GetHour();
                data.minute = TimeManager.Instance.GetMinute();
                
                if (showDebugInfo)
                    Debug.Log($"[SaveManager] 收集时间数据: Year {data.year} Season {data.season} Day {data.day} {data.hour}:{data.minute:D2}");
            }
            else
            {
                // 回退到默认值
                data.day = 1;
                data.season = 0;
                data.year = 1;
                data.hour = 6;
                data.minute = 0;
                
                Debug.LogWarning("[SaveManager] TimeManager 未找到，使用默认时间");
            }
            
            return data;
        }
        
        /// <summary>
        /// 收集玩家数据
        /// </summary>
        private PlayerSaveData CollectPlayerData()
        {
            var data = new PlayerSaveData();
            
            // 尝试查找玩家
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                data.positionX = player.transform.position.x;
                data.positionY = player.transform.position.y;
                data.sceneName = player.scene.name;
            }
            
            return data;
        }
        
        /// <summary>
        /// 收集背包数据
        /// 注意：InventoryService 现在实现了 IPersistentObject，
        /// 会通过 PersistentObjectRegistry 自动收集
        /// 这里保留方法用于兼容性，但实际数据由 Registry 收集
        /// </summary>
        private InventorySaveData CollectInventoryData()
        {
            var data = new InventorySaveData();
            
            // InventoryService 现在通过 IPersistentObject 接口保存
            // 这里只返回空数据，实际数据在 worldObjects 中
            // 保留此方法是为了兼容旧存档格式
            
            return data;
        }
        
        #endregion
        
        #region 数据恢复
        
        /// <summary>
        /// 恢复游戏时间数据
        /// Rule: P1-2 时间恢复 - 调用 TimeManager.SetTime()
        /// </summary>
        private void RestoreGameTimeData(GameTimeSaveData data)
        {
            if (data == null) return;
            
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.SetTime(
                    data.year,
                    (SeasonManager.Season)data.season,
                    data.day,
                    data.hour,
                    data.minute
                );
                
                if (showDebugInfo)
                    Debug.Log($"[SaveManager] 恢复时间: Year {data.year} Season {data.season} Day {data.day} {data.hour}:{data.minute:D2}");
            }
            else
            {
                Debug.LogWarning("[SaveManager] TimeManager 未找到，无法恢复时间");
            }
        }
        
        /// <summary>
        /// 恢复玩家数据
        /// </summary>
        private void RestorePlayerData(PlayerSaveData data)
        {
            if (data == null) return;
            
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = new Vector3(data.positionX, data.positionY, 0);
            }
        }
        
        /// <summary>
        /// 恢复背包数据
        /// 注意：InventoryService 现在实现了 IPersistentObject，
        /// 会通过 PersistentObjectRegistry 自动恢复
        /// 这里保留方法用于兼容旧存档
        /// </summary>
        private void RestoreInventoryData(InventorySaveData data)
        {
            // InventoryService 现在通过 IPersistentObject 接口恢复
            // 这里只处理旧存档格式的兼容性
            
            if (data == null || data.slots == null || data.slots.Count == 0) return;
            
            // 如果旧存档有数据，尝试迁移到新系统
            var inventory = FindFirstObjectByType<InventoryService>();
            if (inventory != null)
            {
                foreach (var slotData in data.slots)
                {
                    if (slotData.slotIndex >= 0 && slotData.slotIndex < inventory.Size && !slotData.IsEmpty)
                    {
                        // 使用新的 InventoryItem API
                        var item = SaveDataHelper.FromSaveData(slotData);
                        inventory.SetInventoryItem(slotData.slotIndex, item);
                    }
                }
                
                if (showDebugInfo)
                    Debug.Log($"[SaveManager] 已从旧存档格式迁移背包数据");
            }
        }
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 获取存档文件路径
        /// </summary>
        private string GetSaveFilePath(string slotName)
        {
            return Path.Combine(SaveFolderPath, slotName + saveFileExtension);
        }
        
        /// <summary>
        /// 确保存档目录存在
        /// </summary>
        private void EnsureSaveFolderExists()
        {
            if (!Directory.Exists(SaveFolderPath))
            {
                Directory.CreateDirectory(SaveFolderPath);
            }
        }
        
        #endregion
        
        #region 调试命令
        
#if UNITY_EDITOR
        [ContextMenu("快速保存 (slot1)")]
        private void DebugQuickSave()
        {
            SaveGame("slot1");
        }
        
        [ContextMenu("快速加载 (slot1)")]
        private void DebugQuickLoad()
        {
            LoadGame("slot1");
        }
        
        [ContextMenu("打印存档路径")]
        private void DebugPrintSavePath()
        {
            Debug.Log($"[SaveManager] 存档路径: {SaveFolderPath}");
            Debug.Log($"[SaveManager] 现有存档: {string.Join(", ", GetAllSaveSlots())}");
        }
        
        [ContextMenu("打开存档目录")]
        private void DebugOpenSaveFolder()
        {
            EnsureSaveFolderExists();
            System.Diagnostics.Process.Start(SaveFolderPath);
        }
#endif
        
        #endregion
    }
}
