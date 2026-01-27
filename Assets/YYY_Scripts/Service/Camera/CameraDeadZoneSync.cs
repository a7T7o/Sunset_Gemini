using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Unity.Cinemachine;

namespace Sunset.Service.Camera
{
    /// <summary>
    /// 摄像头边界限制组件
    /// 自动检测场景边界并同步到 CinemachineConfiner2D，确保摄像头视野不超出场景
    /// </summary>
    public class CameraDeadZoneSync : MonoBehaviour
    {
        #region 序列化字段
        
        [Header("边界检测")]
        [SerializeField] private bool autoDetectBounds = true;
        [SerializeField] private string[] worldLayerNames = { "LAYER 1", "LAYER 2", "LAYER 3" };
        [SerializeField] private float boundsPadding = 0f;
        
        [Header("手动边界（当 autoDetectBounds = false）")]
        [SerializeField] private Bounds manualBounds = new Bounds(Vector3.zero, new Vector3(50, 50, 0));
        
        [Header("引用")]
        [SerializeField] private CinemachineCamera cinemachineCamera;
        [SerializeField] private UnityEngine.Camera mainCamera;
        
        [Header("Confiner 设置")]
        [Tooltip("用于定义边界的 PolygonCollider2D，会自动创建")]
        [SerializeField] private PolygonCollider2D boundingCollider;
        
        [Header("调试")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private bool logDebugInfo = false;
        
        #endregion
        
        #region 私有字段
        
        private CinemachineConfiner2D _confiner;
        private Bounds _worldBounds;
        private bool _isInitialized;
        
        #endregion
        
        #region 公共属性
        
        /// <summary>
        /// 当前检测到的世界边界
        /// </summary>
        public Bounds WorldBounds => _worldBounds;
        
        #endregion

        
        #region Unity 生命周期
        
        private void Awake()
        {
            // 自动获取引用
            if (cinemachineCamera == null)
            {
                cinemachineCamera = GetComponentInParent<CinemachineCamera>();
                if (cinemachineCamera == null)
                {
                    cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();
                }
            }
            
            if (mainCamera == null)
            {
                mainCamera = UnityEngine.Camera.main;
            }
            
            // 获取或创建 Confiner2D
            SetupConfiner();
            
            ValidateReferences();
        }
        
        private void Start()
        {
            if (_isInitialized)
            {
                RefreshBounds();
            }
        }
        
        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        #endregion
        
        #region 公共方法
        
        /// <summary>
        /// 刷新边界检测并应用到 Confiner
        /// </summary>
        public void RefreshBounds()
        {
            if (!_isInitialized)
            {
                if (logDebugInfo)
                    Debug.LogWarning("[CameraConfiner] 组件未正确初始化，无法刷新边界");
                return;
            }
            
            if (autoDetectBounds)
            {
                DetectWorldBounds();
            }
            else
            {
                _worldBounds = manualBounds;
            }
            
            UpdateBoundingCollider();
            InvalidateConfinerCache();
            
            if (logDebugInfo)
            {
                Debug.Log($"[CameraConfiner] 边界已更新: Center={_worldBounds.center}, Size={_worldBounds.size}");
            }
        }
        
        #endregion

        
        #region 私有方法
        
        /// <summary>
        /// 设置 Confiner2D 组件
        /// </summary>
        private void SetupConfiner()
        {
            if (cinemachineCamera == null) return;
            
            // 获取或添加 CinemachineConfiner2D
            _confiner = cinemachineCamera.GetComponent<CinemachineConfiner2D>();
            if (_confiner == null)
            {
                _confiner = cinemachineCamera.gameObject.AddComponent<CinemachineConfiner2D>();
                if (logDebugInfo)
                    Debug.Log("[CameraConfiner] 已添加 CinemachineConfiner2D 组件");
            }
            
            // 创建或获取边界碰撞体
            if (boundingCollider == null)
            {
                // 在场景中创建一个专用的边界物体
                GameObject boundsObj = GameObject.Find("_CameraBounds");
                if (boundsObj == null)
                {
                    boundsObj = new GameObject("_CameraBounds");
                    boundsObj.transform.SetParent(transform.root);
                }
                
                boundingCollider = boundsObj.GetComponent<PolygonCollider2D>();
                if (boundingCollider == null)
                {
                    boundingCollider = boundsObj.AddComponent<PolygonCollider2D>();
                    boundingCollider.isTrigger = true;
                }
                
                if (logDebugInfo)
                    Debug.Log("[CameraConfiner] 已创建边界碰撞体");
            }
            
            // 设置 Confiner 的边界碰撞体
            _confiner.BoundingShape2D = boundingCollider;
        }
        
        /// <summary>
        /// 验证必要引用
        /// </summary>
        private void ValidateReferences()
        {
            _isInitialized = true;
            
            if (cinemachineCamera == null)
            {
                Debug.LogWarning("[CameraConfiner] 未找到 CinemachineCamera，功能已禁用");
                _isInitialized = false;
            }
            
            if (_confiner == null)
            {
                Debug.LogWarning("[CameraConfiner] 未找到 CinemachineConfiner2D，功能已禁用");
                _isInitialized = false;
            }
            
            if (boundingCollider == null)
            {
                Debug.LogWarning("[CameraConfiner] 未找到边界碰撞体，功能已禁用");
                _isInitialized = false;
            }
            
            if (mainCamera == null)
            {
                Debug.LogWarning("[CameraConfiner] 未找到主摄像头，功能已禁用");
                _isInitialized = false;
            }
        }
        
        /// <summary>
        /// 场景加载回调
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (logDebugInfo)
                Debug.Log($"[CameraConfiner] 场景加载: {scene.name}，刷新边界");
            
            // 延迟一帧刷新，确保场景物体已初始化
            StartCoroutine(DelayedRefresh());
        }
        
        private System.Collections.IEnumerator DelayedRefresh()
        {
            yield return null;
            RefreshBounds();
        }

        
        /// <summary>
        /// 自动检测世界边界（基于 Tilemap）
        /// </summary>
        private void DetectWorldBounds()
        {
            Bounds totalBounds = new Bounds(Vector3.zero, Vector3.zero);
            bool boundsInitialized = false;
            
            // 检测所有 Tilemap 的边界
            var tilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
            foreach (var tilemap in tilemaps)
            {
                // 过滤：只包含指定层级下的 Tilemap
                if (!IsInWorldLayers(tilemap.transform))
                    continue;
                
                if (tilemap.cellBounds.size.x > 0 && tilemap.cellBounds.size.y > 0)
                {
                    Bounds tilemapBounds = tilemap.localBounds;
                    Vector3 worldMin = tilemap.transform.TransformPoint(tilemapBounds.min);
                    Vector3 worldMax = tilemap.transform.TransformPoint(tilemapBounds.max);
                    Bounds worldBounds = new Bounds();
                    worldBounds.SetMinMax(worldMin, worldMax);
                    
                    if (!boundsInitialized)
                    {
                        totalBounds = worldBounds;
                        boundsInitialized = true;
                    }
                    else
                    {
                        totalBounds.Encapsulate(worldBounds);
                    }
                }
            }
            
            if (boundsInitialized)
            {
                // 扩展边界
                if (boundsPadding != 0)
                    totalBounds.Expand(boundsPadding * 2f);
                _worldBounds = totalBounds;
                
                if (logDebugInfo)
                {
                    Debug.Log($"[CameraConfiner] 检测到世界边界: Center={_worldBounds.center}, Size={_worldBounds.size}");
                }
            }
            else
            {
                Debug.LogWarning("[CameraConfiner] 未检测到任何 Tilemap，使用手动边界");
                _worldBounds = manualBounds;
            }
        }
        
        /// <summary>
        /// 检查物体是否在指定世界层级下
        /// </summary>
        private bool IsInWorldLayers(Transform t)
        {
            if (worldLayerNames == null || worldLayerNames.Length == 0)
                return true;
            
            Transform current = t;
            while (current != null)
            {
                foreach (var layerName in worldLayerNames)
                {
                    if (current.name == layerName)
                        return true;
                }
                current = current.parent;
            }
            return false;
        }
        
        /// <summary>
        /// 更新边界碰撞体的形状
        /// </summary>
        private void UpdateBoundingCollider()
        {
            if (boundingCollider == null) return;
            
            // 创建矩形边界的顶点（顺时针）
            Vector2[] points = new Vector2[4];
            points[0] = new Vector2(_worldBounds.min.x, _worldBounds.min.y); // 左下
            points[1] = new Vector2(_worldBounds.min.x, _worldBounds.max.y); // 左上
            points[2] = new Vector2(_worldBounds.max.x, _worldBounds.max.y); // 右上
            points[3] = new Vector2(_worldBounds.max.x, _worldBounds.min.y); // 右下
            
            boundingCollider.SetPath(0, points);
            
            if (logDebugInfo)
            {
                Debug.Log($"[CameraConfiner] 边界碰撞体已更新: {points[0]} -> {points[2]}");
            }
        }
        
        /// <summary>
        /// 使 Confiner 缓存失效，强制重新计算
        /// </summary>
        private void InvalidateConfinerCache()
        {
            if (_confiner != null)
            {
                _confiner.InvalidateBoundingShapeCache();
            }
        }
        
        #endregion

        
        #region 编辑器方法
        
#if UNITY_EDITOR
        /// <summary>
        /// 在 Scene 视图绘制边界 Gizmos
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;
            
            // 绘制世界边界（绿色）
            Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
            Gizmos.DrawWireCube(_worldBounds.center, _worldBounds.size);
        }
        
        /// <summary>
        /// Inspector 按钮：手动刷新边界
        /// </summary>
        [ContextMenu("刷新边界")]
        private void DEBUG_RefreshBounds()
        {
            if (cinemachineCamera == null)
            {
                cinemachineCamera = GetComponentInParent<CinemachineCamera>();
                if (cinemachineCamera == null)
                {
                    cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();
                }
            }
            
            if (mainCamera == null)
            {
                mainCamera = UnityEngine.Camera.main;
            }
            
            SetupConfiner();
            ValidateReferences();
            
            if (_isInitialized)
            {
                RefreshBounds();
                Debug.Log("[CameraConfiner] 边界已刷新");
            }
        }
#endif
        
        #endregion
    }
}
