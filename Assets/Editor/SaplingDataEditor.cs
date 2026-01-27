using UnityEngine;
using UnityEditor;
using FarmGame.Data;

namespace FarmGame.Editor
{
    /// <summary>
    /// SaplingData 自定义编辑器
    /// 隐藏不需要的字段，只显示树苗专属配置
    /// </summary>
    [CustomEditor(typeof(SaplingData))]
    public class SaplingDataEditor : UnityEditor.Editor
    {
        // 需要隐藏的字段名（父类的放置配置 + 不需要的字段）
        private static readonly string[] HiddenFields = new string[]
        {
            "isPlaceable",
            "placementType",
            "placementPrefab",
            "buildingSize",
            "placementOffset",
            "canRotate",
            "rotationStep"
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

            // 显示冬季警告
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("注意：冬季无法种植树苗", MessageType.Info);
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
