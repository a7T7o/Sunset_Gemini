using UnityEngine;

/// <summary>
/// Animator扩展方法
/// </summary>
public static class AnimatorExtensions
{
    /// <summary>
    /// 检查Animator是否有指定名称的参数
    /// </summary>
    public static bool HasParameter(this Animator animator, string paramName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return false;
        
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 安全地设置整数参数（如果参数存在）
    /// 无Controller或参数不存在时静默跳过，不输出警告
    /// </summary>
    public static void SafeSetInteger(this Animator animator, string paramName, int value)
    {
        if (animator != null && animator.runtimeAnimatorController != null && animator.HasParameter(paramName))
        {
            animator.SetInteger(paramName, value);
        }
        // 静默处理：无Controller或参数不存在时不输出警告
    }
    
    /// <summary>
    /// 安全地设置浮点参数（如果参数存在）
    /// </summary>
    public static void SafeSetFloat(this Animator animator, string paramName, float value)
    {
        if (animator != null && animator.HasParameter(paramName))
        {
            animator.SetFloat(paramName, value);
        }
    }
    
    /// <summary>
    /// 安全地设置布尔参数（如果参数存在）
    /// </summary>
    public static void SafeSetBool(this Animator animator, string paramName, bool value)
    {
        if (animator != null && animator.HasParameter(paramName))
        {
            animator.SetBool(paramName, value);
        }
    }
    
    /// <summary>
    /// 安全地触发Trigger（如果参数存在）
    /// </summary>
    public static void SafeSetTrigger(this Animator animator, string paramName)
    {
        if (animator != null && animator.HasParameter(paramName))
        {
            animator.SetTrigger(paramName);
        }
    }

    /// <summary>
    /// 安全地获取整数参数（如果参数不存在返回默认值）
    /// 无Controller或参数不存在时静默返回默认值，不输出警告
    /// </summary>
    public static int SafeGetInteger(this Animator animator, string paramName, int defaultValue = 0)
    {
        if (animator != null && animator.runtimeAnimatorController != null && animator.HasParameter(paramName))
        {
            return animator.GetInteger(paramName);
        }
        // 静默处理：无Controller或参数不存在时返回默认值，不输出警告
        return defaultValue;
    }

    /// <summary>
    /// 安全地获取浮点参数（如果参数不存在返回默认值）
    /// </summary>
    public static float SafeGetFloat(this Animator animator, string paramName, float defaultValue = 0f)
    {
        if (animator != null && animator.HasParameter(paramName))
        {
            return animator.GetFloat(paramName);
        }
        return defaultValue;
    }

    /// <summary>
    /// 安全地获取布尔参数（如果参数不存在返回默认值）
    /// </summary>
    public static bool SafeGetBool(this Animator animator, string paramName, bool defaultValue = false)
    {
        if (animator != null && animator.HasParameter(paramName))
        {
            return animator.GetBool(paramName);
        }
        return defaultValue;
    }
}


