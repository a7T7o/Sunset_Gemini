using UnityEngine;
using UnityEditor;
using FarmGame.Data;

namespace FarmGame.Editor
{
    /// <summary>
    /// PlaceableItemData 自定义编辑器
    /// 隐藏父类中不需要的放置配置字段
    /// </summary>
    [CustomEditor(typeof(PlaceableItemData), true)]
    public class PlaceableItemDataEditor : UnityEditor.Editor
    {
        // 需要隐藏的字段名
        private static readonly string[] HiddenFields = new string[]
        {
            "isPlaceable",
            "placementType",
            "placementPrefab",
            "buildingSize"
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 获取迭代器
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                // 跳过隐藏字段
                if (ShouldHideField(iterator.name))
                    continue;

                // 跳过 m_Script 字段（Unity 默认显示）
                if (iterator.name == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.PropertyField(iterator, true);
                    }
                    continue;
                }

                EditorGUILayout.PropertyField(iterator, true);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private bool ShouldHideField(string fieldName)
        {
            foreach (var hidden in HiddenFields)
            {
                if (fieldName == hidden)
                    return true;
            }
            return false;
        }
    }
}
