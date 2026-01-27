using UnityEngine;
using FarmGame.Data;

/// <summary>
/// 资源节点基类 - 树木、矿石等可采集资源的通用逻辑
/// </summary>
public abstract class ResourceNode : MonoBehaviour
{
    #region 配置

    [Header("资源配置")]
    [Tooltip("资源生命值（需要多少次采集才能获取）")]
    [SerializeField] protected int maxHealth = 3;

    [Tooltip("掉落表")]
    [SerializeField] protected DropTable dropTable;

    [Header("视觉反馈")]
    [Tooltip("受击时的抖动强度")]
    [SerializeField] protected float hitShakeIntensity = 0.1f;

    [Tooltip("受击时的抖动持续时间")]
    [SerializeField] protected float hitShakeDuration = 0.2f;

    #endregion

    #region 状态

    protected int currentHealth;
    protected bool isDepleted = false;
    protected Vector3 originalPosition;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDepleted => isDepleted;

    #endregion

    #region Unity生命周期

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
        originalPosition = transform.position;
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 对资源造成伤害（采集）
    /// </summary>
    /// <param name="damage">伤害值</param>
    /// <returns>是否已耗尽</returns>
    public virtual bool TakeDamage(int damage = 1)
    {
        if (isDepleted) return true;

        currentHealth -= damage;
        
        // 播放受击效果
        OnHit();

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Deplete();
            return true;
        }

        return false;
    }

    /// <summary>
    /// 重置资源（用于资源重生）
    /// </summary>
    public virtual void Reset()
    {
        currentHealth = maxHealth;
        isDepleted = false;
        transform.position = originalPosition;
        OnReset();
    }

    #endregion

    #region 保护方法

    /// <summary>
    /// 资源耗尽时调用
    /// </summary>
    protected virtual void Deplete()
    {
        isDepleted = true;
        
        // 生成掉落物品
        SpawnDrops();
        
        // 子类实现具体的耗尽效果（消失、变成树桩等）
        OnDepleted();
    }

    /// <summary>
    /// 生成掉落物品
    /// </summary>
    protected virtual void SpawnDrops()
    {
        if (dropTable == null) return;

        var drops = dropTable.GenerateDrops();
        
        foreach (var drop in drops)
        {
            if (drop.item == null) continue;

            // 使用WorldSpawnService生成掉落物
            if (WorldSpawnService.Instance != null)
            {
                WorldSpawnService.Instance.SpawnMultiple(
                    drop.item, 
                    drop.quality, 
                    drop.amount, 
                    transform.position, 
                    dropTable.spreadRadius
                );
            }
        }
    }

    /// <summary>
    /// 受击时调用（播放抖动等效果）
    /// </summary>
    protected virtual void OnHit()
    {
        // 简单的抖动效果
        StartCoroutine(ShakeCoroutine());
    }

    /// <summary>
    /// 抖动协程
    /// </summary>
    protected System.Collections.IEnumerator ShakeCoroutine()
    {
        float elapsed = 0f;
        
        while (elapsed < hitShakeDuration)
        {
            float x = Random.Range(-hitShakeIntensity, hitShakeIntensity);
            float y = Random.Range(-hitShakeIntensity, hitShakeIntensity);
            transform.position = originalPosition + new Vector3(x, y, 0f);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = originalPosition;
    }

    /// <summary>
    /// 资源耗尽后的处理（子类重写）
    /// </summary>
    protected abstract void OnDepleted();

    /// <summary>
    /// 资源重置后的处理（子类重写）
    /// </summary>
    protected abstract void OnReset();

    #endregion

    #region 编辑器

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (maxHealth < 1) maxHealth = 1;
    }
#endif

    #endregion
}
