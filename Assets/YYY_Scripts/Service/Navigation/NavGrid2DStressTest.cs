using UnityEngine;

/// <summary>
/// NavGrid2D 压力测试脚本
/// 用于验证 Zero GC 优化效果
/// 
/// 使用方法：
/// 1. 将此脚本挂载到场景中的任意 GameObject 上
/// 2. 运行游戏
/// 3. 打开 Unity Profiler (Window → Analysis → Profiler)
/// 4. 查看 CPU Usage → GC Alloc 列
/// 5. 确保每帧的 GC Alloc 为 0KB
/// 
/// 测试标准：
/// - 每帧调用 100 次 IsPointBlocked
/// - Profiler 显示 GC Alloc = 0KB
/// - 帧率保持稳定（60 FPS）
/// </summary>
public class NavGrid2DStressTest : MonoBehaviour
{
    [Header("测试配置")]
    [SerializeField] private bool enableTest = true;
    [SerializeField] private int callsPerFrame = 100;
    [SerializeField] private float testRadius = 10f;
    
    [Header("统计信息")]
    [SerializeField] private int totalCalls = 0;
    [SerializeField] private float averageFPS = 0f;
    
    private NavGrid2D _navGrid;
    private Vector2 _testCenter;
    private float _fpsSum = 0f;
    private int _fpsCount = 0;
    
    void Start()
    {
        _navGrid = FindFirstObjectByType<NavGrid2D>();
        if (_navGrid == null)
        {
            Debug.LogError("[NavGrid2DStressTest] 未找到 NavGrid2D 组件！");
            enabled = false;
            return;
        }
        
        // 测试中心点设置为场景中心
        _testCenter = Vector2.zero;
        
        Debug.Log($"<color=cyan>[NavGrid2DStressTest] 压力测试已启动</color>");
        Debug.Log($"<color=cyan>每帧调用次数: {callsPerFrame}</color>");
        Debug.Log($"<color=cyan>请打开 Profiler 查看 GC Alloc 是否为 0KB</color>");
    }
    
    void Update()
    {
        if (!enableTest) return;
        
        // 压力测试：每帧调用 N 次 IsPointBlocked
        for (int i = 0; i < callsPerFrame; i++)
        {
            // 在测试半径内随机生成测试点
            Vector2 randomOffset = Random.insideUnitCircle * testRadius;
            Vector2 testPoint = _testCenter + randomOffset;
            
            // 调用 IsPointBlocked（通过 IsWalkable 间接调用）
            _navGrid.IsWalkable(testPoint);
            
            totalCalls++;
        }
        
        // 统计 FPS
        float currentFPS = 1f / Time.unscaledDeltaTime;
        _fpsSum += currentFPS;
        _fpsCount++;
        averageFPS = _fpsSum / _fpsCount;
    }
    
    void OnGUI()
    {
        if (!enableTest) return;
        
        // 在屏幕上显示统计信息
        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.cyan;
        
        GUI.Label(new Rect(10, 10, 500, 30), $"压力测试运行中...", style);
        GUI.Label(new Rect(10, 40, 500, 30), $"总调用次数: {totalCalls}", style);
        GUI.Label(new Rect(10, 70, 500, 30), $"平均 FPS: {averageFPS:F1}", style);
        GUI.Label(new Rect(10, 100, 500, 30), $"每帧调用: {callsPerFrame} 次", style);
        
        style.normal.textColor = Color.yellow;
        GUI.Label(new Rect(10, 130, 500, 30), "请打开 Profiler 查看 GC Alloc", style);
        
        // 如果 FPS 低于 50，显示警告
        if (averageFPS < 50f)
        {
            style.normal.textColor = Color.red;
            GUI.Label(new Rect(10, 160, 500, 30), "警告：FPS 过低！", style);
        }
    }
    
    void OnDestroy()
    {
        Debug.Log($"<color=cyan>[NavGrid2DStressTest] 测试结束</color>");
        Debug.Log($"<color=cyan>总调用次数: {totalCalls}</color>");
        Debug.Log($"<color=cyan>平均 FPS: {averageFPS:F1}</color>");
    }
}
