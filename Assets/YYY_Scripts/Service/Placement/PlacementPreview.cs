using UnityEngine;
using FarmGame.Data;

/// <summary>
/// 放置预览组件
/// 显示物品放置位置和有效性指示
/// </summary>
public class PlacementPreview : MonoBehaviour
{
        #region 序列化字段
        [Header("━━━━ 预览设置 ━━━━")]
        [Tooltip("预览 Sprite 渲染器")]
        [SerializeField] private SpriteRenderer previewRenderer;

        [Tooltip("边距指示器（可选）")]
        [SerializeField] private SpriteRenderer marginIndicator;

        [Header("━━━━ 颜色配置 ━━━━")]
        [Tooltip("有效位置颜色")]
        [SerializeField] private Color validColor = new Color(0f, 1f, 0f, 0.5f);

        [Tooltip("无效位置颜色")]
        [SerializeField] private Color invalidColor = new Color(1f, 0f, 0f, 0.5f);

        [Tooltip("超出范围颜色")]
        [SerializeField] private Color outOfRangeColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

        [Header("━━━━ 边距指示器配置 ━━━━")]
        [Tooltip("边距指示器颜色")]
        [SerializeField] private Color marginColor = new Color(1f, 1f, 0f, 0.2f);

        [Tooltip("边距指示器 Sprite（圆形或方形）")]
        [SerializeField] private Sprite marginSprite;
        #endregion

        #region 私有字段
        private ItemData currentItem;
        private bool isValid = true;
        private bool isOutOfRange = false;
        private float currentVMargin = 0f;
        private float currentHMargin = 0f;
        #endregion

        #region Unity 生命周期
        private void Awake()
        {
            // 确保有预览渲染器
            if (previewRenderer == null)
            {
                previewRenderer = GetComponent<SpriteRenderer>();
                if (previewRenderer == null)
                {
                    previewRenderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }

            // 设置排序层
            previewRenderer.sortingLayerName = "Effects";
            previewRenderer.sortingOrder = 100;

            // 初始隐藏
            Hide();
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 显示预览
        /// </summary>
        public void Show(ItemData item)
        {
            if (item == null) return;

            currentItem = item;
            gameObject.SetActive(true);

            // 设置预览 Sprite
            if (item.icon != null)
            {
                previewRenderer.sprite = item.icon;
            }

            // 如果是树苗，显示边距指示器
            if (item.placementType == PlacementType.Sapling && item is SaplingData sapling)
            {
                float vMargin, hMargin;
                if (sapling.GetStage0Margins(out vMargin, out hMargin))
                {
                    ShowMarginIndicator(vMargin, hMargin);
                }
                else
                {
                    ShowMarginIndicator(0.2f, 0.15f);
                }
            }
            else
            {
                HideMarginIndicator();
            }

            // 默认设置为有效
            SetValid(true);
        }

        /// <summary>
        /// 隐藏预览
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
            currentItem = null;
            HideMarginIndicator();
        }

        /// <summary>
        /// 更新预览位置
        /// </summary>
        public void UpdatePosition(Vector3 worldPosition)
        {
            if (currentItem == null) return;

            // 根据放置类型决定是否对齐网格
            Vector3 targetPos = worldPosition;
            if (currentItem.placementType == PlacementType.Sapling || 
                currentItem.placementType == PlacementType.Building)
            {
                targetPos = AlignToGrid(worldPosition);
            }

            transform.position = targetPos;
        }

        /// <summary>
        /// 设置有效性状态
        /// </summary>
        public void SetValid(bool valid)
        {
            isValid = valid;
            UpdateColor();
        }

        /// <summary>
        /// 设置超出范围状态
        /// </summary>
        public void SetOutOfRange(bool outOfRange)
        {
            isOutOfRange = outOfRange;
            UpdateColor();
        }

        /// <summary>
        /// 显示边距指示器
        /// </summary>
        public void ShowMarginIndicator(float vMargin, float hMargin)
        {
            currentVMargin = vMargin;
            currentHMargin = hMargin;

            if (marginIndicator == null)
            {
                CreateMarginIndicator();
            }

            if (marginIndicator != null)
            {
                marginIndicator.gameObject.SetActive(true);
                // 设置缩放以匹配边距
                float maxMargin = Mathf.Max(vMargin, hMargin) * 2f;
                marginIndicator.transform.localScale = new Vector3(
                    hMargin * 2f,
                    vMargin * 2f,
                    1f
                );
                marginIndicator.color = marginColor;
            }
        }

        /// <summary>
        /// 隐藏边距指示器
        /// </summary>
        public void HideMarginIndicator()
        {
            if (marginIndicator != null)
            {
                marginIndicator.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 获取当前预览位置
        /// </summary>
        public Vector3 GetPreviewPosition()
        {
            return transform.position;
        }

        /// <summary>
        /// 获取当前是否有效
        /// </summary>
        public bool IsValid => isValid && !isOutOfRange;
        #endregion

        #region 私有方法
        /// <summary>
        /// 更新预览颜色
        /// </summary>
        private void UpdateColor()
        {
            if (previewRenderer == null) return;

            if (isOutOfRange)
            {
                previewRenderer.color = outOfRangeColor;
            }
            else if (isValid)
            {
                previewRenderer.color = validColor;
            }
            else
            {
                previewRenderer.color = invalidColor;
            }
        }

        /// <summary>
        /// 对齐到整数网格
        /// </summary>
        private Vector3 AlignToGrid(Vector3 position)
        {
            return new Vector3(
                Mathf.Round(position.x),
                Mathf.Round(position.y),
                position.z
            );
        }

        /// <summary>
        /// 创建边距指示器
        /// </summary>
        private void CreateMarginIndicator()
        {
            GameObject indicatorObj = new GameObject("MarginIndicator");
            indicatorObj.transform.SetParent(transform);
            indicatorObj.transform.localPosition = Vector3.zero;

            marginIndicator = indicatorObj.AddComponent<SpriteRenderer>();
            marginIndicator.sortingLayerName = "Effects";
            marginIndicator.sortingOrder = 99;

            // 使用提供的 Sprite 或创建默认圆形
            if (marginSprite != null)
            {
                marginIndicator.sprite = marginSprite;
            }
            else
            {
                // 创建简单的圆形 Sprite
                marginIndicator.sprite = CreateCircleSprite();
            }

            marginIndicator.color = marginColor;
        }

        /// <summary>
        /// 创建简单的圆形 Sprite
        /// </summary>
        private Sprite CreateCircleSprite()
        {
            int size = 64;
            Texture2D texture = new Texture2D(size, size);
            Color[] colors = new Color[size * size];

            float center = size / 2f;
            float radius = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist <= radius)
                    {
                        colors[y * size + x] = Color.white;
                    }
                    else
                    {
                        colors[y * size + x] = Color.clear;
                    }
                }
            }

            texture.SetPixels(colors);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
        #endregion
    }
