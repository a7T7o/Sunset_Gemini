using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 凸包计算工具
/// 使用 Andrew's Monotone Chain 算法
/// 时间复杂度：O(n log n)
/// </summary>
public static class ConvexHullCalculator
{
    /// <summary>
    /// 计算点集的凸包
    /// </summary>
    /// <param name="points">输入点集</param>
    /// <returns>凸包顶点列表（逆时针顺序）</returns>
    public static List<Vector2> ComputeConvexHull(List<Vector2> points)
    {
        if (points == null || points.Count < 3)
        {
            return new List<Vector2>(points ?? new List<Vector2>());
        }

        // 复制并排序点集
        List<Vector2> sorted = new List<Vector2>(points);
        sorted.Sort((a, b) =>
        {
            int cmp = a.x.CompareTo(b.x);
            return cmp != 0 ? cmp : a.y.CompareTo(b.y);
        });

        // 移除重复点
        List<Vector2> unique = new List<Vector2>();
        for (int i = 0; i < sorted.Count; i++)
        {
            if (i == 0 || Vector2.Distance(sorted[i], sorted[i - 1]) > 0.001f)
            {
                unique.Add(sorted[i]);
            }
        }

        if (unique.Count < 3)
        {
            return unique;
        }

        // 构建下凸包
        List<Vector2> lower = new List<Vector2>();
        foreach (var p in unique)
        {
            while (lower.Count >= 2 && Cross(lower[lower.Count - 2], lower[lower.Count - 1], p) <= 0)
            {
                lower.RemoveAt(lower.Count - 1);
            }
            lower.Add(p);
        }

        // 构建上凸包
        List<Vector2> upper = new List<Vector2>();
        for (int i = unique.Count - 1; i >= 0; i--)
        {
            var p = unique[i];
            while (upper.Count >= 2 && Cross(upper[upper.Count - 2], upper[upper.Count - 1], p) <= 0)
            {
                upper.RemoveAt(upper.Count - 1);
            }
            upper.Add(p);
        }

        // 合并（移除重复的首尾点）
        lower.RemoveAt(lower.Count - 1);
        upper.RemoveAt(upper.Count - 1);
        lower.AddRange(upper);

        return lower;
    }

    /// <summary>
    /// 判断点是否在凸包内部
    /// </summary>
    /// <param name="point">待检测点</param>
    /// <param name="hull">凸包顶点列表（逆时针顺序）</param>
    /// <returns>点是否在凸包内部或边界上</returns>
    public static bool IsPointInsideConvexHull(Vector2 point, List<Vector2> hull)
    {
        if (hull == null || hull.Count < 3)
        {
            return false;
        }

        int n = hull.Count;
        for (int i = 0; i < n; i++)
        {
            Vector2 a = hull[i];
            Vector2 b = hull[(i + 1) % n];

            // 叉积判断点在边的哪一侧
            float cross = Cross(a, b, point);
            if (cross < -0.001f)
            {
                return false; // 点在边的右侧（外部）
            }
        }
        return true;
    }

    /// <summary>
    /// 计算点到凸包边界的最短距离
    /// </summary>
    /// <param name="point">待检测点</param>
    /// <param name="hull">凸包顶点列表</param>
    /// <returns>最短距离（负值表示在内部）</returns>
    public static float DistanceToConvexHull(Vector2 point, List<Vector2> hull)
    {
        if (hull == null || hull.Count < 3)
        {
            return float.MaxValue;
        }

        float minDist = float.MaxValue;
        int n = hull.Count;

        for (int i = 0; i < n; i++)
        {
            Vector2 a = hull[i];
            Vector2 b = hull[(i + 1) % n];

            float dist = PointToSegmentDistance(point, a, b);
            minDist = Mathf.Min(minDist, dist);
        }

        // 如果点在内部，返回负值
        if (IsPointInsideConvexHull(point, hull))
        {
            return -minDist;
        }

        return minDist;
    }

    /// <summary>
    /// 计算叉积 (b-a) × (c-a)
    /// 正值：c 在 ab 左侧
    /// 负值：c 在 ab 右侧
    /// 零：共线
    /// </summary>
    private static float Cross(Vector2 a, Vector2 b, Vector2 c)
    {
        return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
    }

    /// <summary>
    /// 计算点到线段的最短距离
    /// </summary>
    private static float PointToSegmentDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        Vector2 ap = p - a;

        float t = Vector2.Dot(ap, ab) / Vector2.Dot(ab, ab);
        t = Mathf.Clamp01(t);

        Vector2 closest = a + t * ab;
        return Vector2.Distance(p, closest);
    }

    /// <summary>
    /// 从 Bounds 列表生成凸包
    /// </summary>
    /// <param name="boundsList">Bounds 列表</param>
    /// <returns>凸包顶点列表</returns>
    public static List<Vector2> ComputeConvexHullFromBounds(List<Bounds> boundsList)
    {
        List<Vector2> points = new List<Vector2>();

        foreach (var bounds in boundsList)
        {
            // 添加四个角点
            points.Add(new Vector2(bounds.min.x, bounds.min.y));
            points.Add(new Vector2(bounds.max.x, bounds.min.y));
            points.Add(new Vector2(bounds.min.x, bounds.max.y));
            points.Add(new Vector2(bounds.max.x, bounds.max.y));
        }

        return ComputeConvexHull(points);
    }
}
