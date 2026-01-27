using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

/// <summary>
/// OcclusionTransparency的自定义Inspector
/// 优化UI：标签使用ReorderableList + Tag Popup下拉框（学习Game Input Manager）
/// </summary>
[CustomEditor(typeof(OcclusionTransparency))]
public class OcclusionTransparencyEditor : Editor
{
    private OcclusionManager cachedManager;

    private void OnEnable()
    {
        cachedManager = FindFirstObjectByType<OcclusionManager>();
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("此组件无参数。所有透明度相关设置已集中到 OcclusionManager。若需调整，请在 OcclusionManager 中修改全局透明度与渐变速度，并重新进入 Play 生效。", MessageType.Info);

        if (cachedManager == null)
        {
            if (GUILayout.Button("选中/创建 OcclusionManager"))
            {
                var mgr = FindFirstObjectByType<OcclusionManager>();
                if (mgr == null)
                {
                    var go = new GameObject("OcclusionManager");
                    mgr = go.AddComponent<OcclusionManager>();
                }
                Selection.activeObject = mgr.gameObject;
            }
        }
        else
        {
            if (GUILayout.Button("转到 OcclusionManager"))
            {
                Selection.activeObject = cachedManager.gameObject;
            }
        }
    }
}
