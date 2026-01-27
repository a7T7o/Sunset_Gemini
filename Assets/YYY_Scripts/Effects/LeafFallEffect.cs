using UnityEngine;

/// <summary>
/// 树叶飘落效果 - 管理单个树叶的飘落行为
/// </summary>
public class LeafFallEffect : MonoBehaviour
{
    #region 私有字段
    
    private SpriteRenderer _spriteRenderer;
    private float _targetY;
    private float _fallSpeed;
    private float _driftSpeed;
    private float _rotateSpeed;
    private float _fadeStartY;
    private float _fadeDistance;
    private bool _isActive = false;
    
    private Vector3 _velocity;
    private float _driftPhase;
    
    #endregion
    
    #region Unity 生命周期
    
    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null)
        {
            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
    }
    
    void Update()
    {
        if (!_isActive) return;
        
        // 飘落
        float driftX = Mathf.Sin(_driftPhase) * _driftSpeed * Time.deltaTime;
        _driftPhase += Time.deltaTime * 3f;
        
        transform.position += new Vector3(driftX, -_fallSpeed * Time.deltaTime, 0);
        
        // 旋转
        transform.Rotate(0, 0, _rotateSpeed * Time.deltaTime);
        
        // 淡出
        if (transform.position.y <= _fadeStartY)
        {
            float fadeProgress = (_fadeStartY - transform.position.y) / _fadeDistance;
            fadeProgress = Mathf.Clamp01(fadeProgress);
            
            Color c = _spriteRenderer.color;
            c.a = 1f - fadeProgress;
            _spriteRenderer.color = c;
            
            if (fadeProgress >= 1f)
            {
                Deactivate();
            }
        }
    }
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 初始化并激活树叶
    /// </summary>
    public void Activate(Sprite sprite, Vector3 startPos, float targetY, float fallSpeed, float driftSpeed, float rotateSpeed)
    {
        _spriteRenderer.sprite = sprite;
        _spriteRenderer.sortingLayerName = "Effects";
        _spriteRenderer.sortingOrder = 100;
        
        transform.position = startPos;
        transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        transform.localScale = Vector3.one * Random.Range(0.8f, 1.2f);
        
        _targetY = targetY;
        _fallSpeed = fallSpeed;
        _driftSpeed = driftSpeed;
        _rotateSpeed = rotateSpeed;
        _driftPhase = Random.Range(0f, Mathf.PI * 2f);
        
        // 淡出从目标Y上方0.3开始
        _fadeStartY = targetY + 0.3f;
        _fadeDistance = 0.5f;
        
        Color c = _spriteRenderer.color;
        c.a = 1f;
        _spriteRenderer.color = c;
        
        _isActive = true;
        gameObject.SetActive(true);
    }
    
    /// <summary>
    /// 停用树叶（返回对象池）
    /// </summary>
    public void Deactivate()
    {
        _isActive = false;
        gameObject.SetActive(false);
        
        // 通知 LeafSpawner 回收
        var spawner = GetComponentInParent<LeafSpawner>();
        if (spawner != null)
        {
            spawner.ReturnLeaf(this);
        }
    }
    
    #endregion
}
