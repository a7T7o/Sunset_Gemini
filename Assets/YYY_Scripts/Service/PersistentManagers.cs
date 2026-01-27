using UnityEngine;

/// <summary>
/// 持久化管理器容器
/// 确保所有子管理器在场景切换时不被销毁
/// 
/// 使用方法：
/// 1. 在场景中创建一个根物体，命名为 "PersistentManagers"
/// 2. 添加此组件
/// 3. 将 TimeManager、SeasonManager、WeatherSystem 等管理器作为子物体
/// 4. 这些管理器不需要再调用 DontDestroyOnLoad
/// </summary>
public class PersistentManagers : MonoBehaviour
{
    private static PersistentManagers instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("<color=cyan>[PersistentManagers] 初始化完成，管理器将在场景切换时保持</color>");
        }
        else
        {
            Debug.LogWarning("<color=yellow>[PersistentManagers] 检测到重复实例，销毁</color>");
            Destroy(gameObject);
        }
    }
}
