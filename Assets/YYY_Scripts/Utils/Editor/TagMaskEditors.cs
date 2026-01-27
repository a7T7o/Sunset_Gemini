#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(NavGrid2D))]
public class NavGrid2DEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        // 先画除 obstacleTags 之外的所有字段
        DrawPropertiesExcluding(serializedObject, "m_Script", "obstacleTags");
        // 画 Tag 多选
        DrawTagMask(serializedObject.FindProperty("obstacleTags"), "Obstacle Tags");
        serializedObject.ApplyModifiedProperties();
    }

    public static void DrawTagMask(SerializedProperty arrayProp, string label)
    {
        var builtin = InternalEditorUtility.tags;
        string[] tags;
        if (builtin == null || builtin.Length == 0)
        {
            tags = new[] { "Untagged" };
        }
        else
        {
            tags = new string[builtin.Length + 1];
            tags[0] = "Untagged";
            builtin.CopyTo(tags, 1);
        }
        int mask = 0;
        for (int i = 0; i < tags.Length; i++)
        {
            for (int j = 0; j < arrayProp.arraySize; j++)
            {
                var v = arrayProp.GetArrayElementAtIndex(j).stringValue;
                if (string.IsNullOrEmpty(v) && i == 0) { mask |= (1 << i); break; }
                if (v == tags[i]) { mask |= (1 << i); break; }
            }
        }
        int newMask = EditorGUILayout.MaskField(label, mask, tags);
        if (newMask != mask)
        {
            var list = new System.Collections.Generic.List<string>();
            for (int i = 0; i < tags.Length; i++) if ((newMask & (1 << i)) != 0) list.Add(tags[i]);
            arrayProp.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++) arrayProp.GetArrayElementAtIndex(i).stringValue = list[i];
        }
    }
}

[CustomEditor(typeof(GameInputManager))]
public class GameInputManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "interactableTags");
        NavGrid2DEditor.DrawTagMask(serializedObject.FindProperty("interactableTags"), "Interactable Tags");
        serializedObject.ApplyModifiedProperties();
    }
}

[CustomEditor(typeof(AutoPickupService))]
public class AutoPickupServiceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "pickupTags");
        NavGrid2DEditor.DrawTagMask(serializedObject.FindProperty("pickupTags"), "Pickup Tags");
        serializedObject.ApplyModifiedProperties();
    }
}

[CustomEditor(typeof(PlacementManager))]
public class PlacementManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "obstacleTags");
        NavGrid2DEditor.DrawTagMask(serializedObject.FindProperty("obstacleTags"), "Obstacle Tags");
        serializedObject.ApplyModifiedProperties();
    }
}

[CustomEditor(typeof(PlacementManagerV2))]
public class PlacementManagerV2Editor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "obstacleTags");
        NavGrid2DEditor.DrawTagMask(serializedObject.FindProperty("obstacleTags"), "Obstacle Tags");
        serializedObject.ApplyModifiedProperties();
    }
}

[CustomEditor(typeof(PlacementManagerV3))]
public class PlacementManagerV3Editor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "obstacleTags");
        NavGrid2DEditor.DrawTagMask(serializedObject.FindProperty("obstacleTags"), "Obstacle Tags");
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
