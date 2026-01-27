#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

[CustomPropertyDrawer(typeof(TagSelectorAttribute))]
public class TagSelectorDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        var builtIn = InternalEditorUtility.tags;
        // 插入 "Untagged" 作为第一个选项，便于选择默认标签
        var tags = new string[(builtIn?.Length ?? 0) + 1];
        tags[0] = "Untagged";
        if (builtIn != null && builtIn.Length > 0)
            builtIn.CopyTo(tags, 1);
        if (property.propertyType == SerializedPropertyType.String)
        {
            int index = Mathf.Max(0, System.Array.IndexOf(tags, string.IsNullOrEmpty(property.stringValue) ? "Untagged" : property.stringValue));
            int newIndex = EditorGUI.Popup(position, label.text, index, tags);
            if (newIndex >= 0 && newIndex < tags.Length)
                property.stringValue = tags[newIndex] == "Untagged" ? string.Empty : tags[newIndex];
        }
        else if (property.propertyType == SerializedPropertyType.Generic && property.isArray)
        {
            // 多选：用 MaskField 表现，内部用字符串数组保存
            int mask = 0;
            for (int i = 0; i < tags.Length; i++)
            {
                for (int j = 0; j < property.arraySize; j++)
                {
                    if (property.GetArrayElementAtIndex(j).stringValue == tags[i])
                    {
                        mask |= (1 << i);
                        break;
                    }
                }
            }
            int newMask = EditorGUI.MaskField(position, label.text, mask, tags);
            if (newMask != mask)
            {
                // 写回字符串数组
                var list = new System.Collections.Generic.List<string>();
                for (int i = 0; i < tags.Length; i++)
                {
                    if ((newMask & (1 << i)) != 0) list.Add(tags[i]);
                }
                property.arraySize = list.Count;
                for (int i = 0; i < list.Count; i++)
                {
                    property.GetArrayElementAtIndex(i).stringValue = list[i] == "Untagged" ? string.Empty : list[i];
                }
            }
        }
        else
        {
            EditorGUI.LabelField(position, label.text, "Use [TagSelector] on string or string[]");
        }
        EditorGUI.EndProperty();
    }
}
#endif
