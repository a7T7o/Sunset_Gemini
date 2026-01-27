using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(PlayerAutoNavigator))]
public class PlayerAutoNavigatorEditor : Editor
{
    private SerializedProperty losObstacleTagsProp;
    
    void OnEnable()
    {
        losObstacleTagsProp = serializedObject.FindProperty("losObstacleTags");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // 绘制所有默认字段，但跳过losObstacleTags（我们会自定义绘制）
        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;
            
            // 跳过Script字段和losObstacleTags字段
            if (prop.name == "m_Script" || prop.name == "losObstacleTags")
                continue;
            
            EditorGUILayout.PropertyField(prop, true);
        }
        
        // 🔥 自定义绘制losObstacleTags：使用MaskField风格
        EditorGUILayout.Space(5);
        
        // 获取Unity项目中定义的所有Tag
        string[] allTags = UnityEditorInternal.InternalEditorUtility.tags;
        
        if (allTags.Length == 0)
        {
            EditorGUILayout.HelpBox("项目中没有定义Tag", MessageType.Info);
        }
        else
        {
            // 获取当前已选中的Tags
            HashSet<string> selectedTags = new HashSet<string>();
            for (int i = 0; i < losObstacleTagsProp.arraySize; i++)
            {
                selectedTags.Add(losObstacleTagsProp.GetArrayElementAtIndex(i).stringValue);
            }
            
            // 转换为mask值
            int maskValue = 0;
            for (int i = 0; i < allTags.Length; i++)
            {
                if (selectedTags.Contains(allTags[i]))
                {
                    maskValue |= (1 << i);
                }
            }
            
            // 🔥 绘制MaskField（显示"Mixed..."）
            EditorGUI.BeginChangeCheck();
            int newMaskValue = EditorGUILayout.MaskField("Los Obstacle Tags", maskValue, allTags);
            
            if (EditorGUI.EndChangeCheck())
            {
                // 转换mask值回Tag列表
                losObstacleTagsProp.ClearArray();
                for (int i = 0; i < allTags.Length; i++)
                {
                    if ((newMaskValue & (1 << i)) != 0)
                    {
                        int index = losObstacleTagsProp.arraySize;
                        losObstacleTagsProp.InsertArrayElementAtIndex(index);
                        losObstacleTagsProp.GetArrayElementAtIndex(index).stringValue = allTags[i];
                    }
                }
            }
        }
        
        // 快捷按钮：从NavGrid2D复制配置
        EditorGUILayout.Space(5);
        if (GUILayout.Button("从NavGrid2D复制障碍物Tag配置"))
        {
            NavGrid2D navGrid = FindFirstObjectByType<NavGrid2D>();
            if (navGrid != null)
            {
                SerializedObject navGridSO = new SerializedObject(navGrid);
                SerializedProperty navObstacleTags = navGridSO.FindProperty("obstacleTags");
                
                if (navObstacleTags != null && navObstacleTags.arraySize > 0)
                {
                    losObstacleTagsProp.ClearArray();
                    for (int i = 0; i < navObstacleTags.arraySize; i++)
                    {
                        losObstacleTagsProp.InsertArrayElementAtIndex(i);
                        losObstacleTagsProp.GetArrayElementAtIndex(i).stringValue = 
                            navObstacleTags.GetArrayElementAtIndex(i).stringValue;
                    }
                    Debug.Log($"已从NavGrid2D复制{navObstacleTags.arraySize}个障碍物Tag配置");
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", "NavGrid2D未配置障碍物Tag", "确定");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "场景中未找到NavGrid2D组件", "确定");
            }
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}
