using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 简单门触发器 - 用于建筑预制体的子物体
/// 用法：将此组件添加到建筑预制体的子物体上，配置传送目标即可
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DoorTrigger : MonoBehaviour
{
    [Header("传送设置")]
    [Tooltip("目标场景名称（留空=当前场景传送）")]
    public string targetSceneName;
    
    [Tooltip("传送到的目标位置")]
    public Vector2 targetPosition;

    [Header("交互设置")]
    [Tooltip("需要按键触发")]
    public bool requireKeyPress = true;
    
    [Tooltip("触发按键")]
    public KeyCode interactKey = KeyCode.E;
    
    [Tooltip("提示文本")]
    public string promptText = "按 E 进入";

    [Header("可选设置")]
    public AudioClip doorSound;

    private bool playerInRange = false;
    private GameObject player;

    private void Start()
    {
        // 确保是触发器
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }
    }

    private void Update()
    {
        if (playerInRange && requireKeyPress && Input.GetKeyDown(interactKey))
        {
            EnterDoor();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            player = other.gameObject;
            
            Debug.Log(promptText); // 这里可以集成你的UI系统

            if (!requireKeyPress)
            {
                EnterDoor();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            player = null;
        }
    }

    private void EnterDoor()
    {
        if (player == null) return;

        // 播放音效
        if (doorSound != null)
        {
            AudioSource.PlayClipAtPoint(doorSound, transform.position);
        }

        // 传送
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            // 切换场景
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            // 当前场景传送
            player.transform.position = targetPosition;
        }
    }

    // Scene视图可视化
    private void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = playerInRange ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(transform.position, col.bounds.size);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 显示传送目标
        if (targetPosition != Vector2.zero)
        {
            Gizmos.color = Color.cyan;
            Vector3 target = new Vector3(targetPosition.x, targetPosition.y, 0);
            Gizmos.DrawLine(transform.position, target);
            Gizmos.DrawWireSphere(target, 0.5f);
        }

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, 
            $"门触发器\n目标: {(string.IsNullOrEmpty(targetSceneName) ? "当前场景" : targetSceneName)}");
#endif
    }
}

