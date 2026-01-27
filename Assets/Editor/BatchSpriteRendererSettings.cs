using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 批量SpriteRenderer设置工具
/// 选择文件/文件夹，批量修改含有SpriteRenderer的预制体
/// </summary>
public class BatchSpriteRendererSettings : EditorWindow
{
    // 勾选状态
    private bool changeSprite = false;
    private bool changeColor = false;
    private bool changeFlipX = false;
    private bool changeFlipY = false;
    private bool changeDrawMode = false;
    private bool changeMaskInteraction = false;
    private bool changeSpriteSortPoint = false;
    private bool changeMaterial = false;
    private bool changeSortingLayer = false;
    private bool changeOrderInLayer = false;

    // 参数值
    private Sprite targetSprite = null;
    private Color targetColor = Color.white;
    private bool targetFlipX = false;
    private bool targetFlipY = false;
    private SpriteDrawMode targetDrawMode = SpriteDrawMode.Simple;
    private SpriteMaskInteraction targetMaskInteraction = SpriteMaskInteraction.None;
    private SpriteSortPoint targetSpriteSortPoint = SpriteSortPoint.Center;
    private Material targetMaterial = null;
    private string targetSortingLayer = "Default";
    private int targetOrderInLayer = 0;

    private Vector2 scrollPos;
    private List<GameObject> foundPrefabs = new List<GameObject>();
    private bool showPreview = false;

    [MenuItem("Tools/批量SpriteRenderer设置工具")]
    public static void ShowWindow()
    {
        var window = GetWindow<BatchSpriteRendererSettings>("批量SR设置");
        window.minSize = new Vector2(400, 600);
        window.Show();
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Label("批量SpriteRenderer设置工具", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "在Project窗口选择文件或文件夹，点击\"扫描\"查找所有含有SpriteRenderer的预制体\n" +
            "勾选要修改的参数，然后点击\"应用设置\"批量修改", 
            MessageType.Info);

        EditorGUILayout.Space();

        // 扫描按钮
        if (GUILayout.Button("扫描选中的文件/文件夹", GUILayout.Height(30)))
        {
            ScanSelectedAssets();
        }

        // 显示找到的预制体
        if (foundPrefabs.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"找到 {foundPrefabs.Count} 个含有SpriteRenderer的预制体", EditorStyles.boldLabel);
            
            showPreview = EditorGUILayout.Foldout(showPreview, "预览列表");
            if (showPreview)
            {
                EditorGUI.indentLevel++;
                foreach (var prefab in foundPrefabs.Take(20)) // 只显示前20个
                {
                    EditorGUILayout.ObjectField(prefab, typeof(GameObject), false);
                }
                if (foundPrefabs.Count > 20)
                {
                    EditorGUILayout.LabelField($"... 还有 {foundPrefabs.Count - 20} 个");
                }
                EditorGUI.indentLevel--;
            }
        }

        EditorGUILayout.Space();
        DrawSeparator();
        EditorGUILayout.Space();

        GUILayout.Label("SpriteRenderer 参数设置", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("只修改勾选的参数，未勾选的保持不变", MessageType.Info);

        EditorGUILayout.Space();

        // 精灵
        EditorGUILayout.BeginHorizontal();
        changeSprite = EditorGUILayout.Toggle(changeSprite, GUILayout.Width(20));
        GUI.enabled = changeSprite;
        targetSprite = (Sprite)EditorGUILayout.ObjectField("精灵 (Sprite)", targetSprite, typeof(Sprite), false);
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        // 颜色
        EditorGUILayout.BeginHorizontal();
        changeColor = EditorGUILayout.Toggle(changeColor, GUILayout.Width(20));
        GUI.enabled = changeColor;
        targetColor = EditorGUILayout.ColorField("颜色 (Color)", targetColor);
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        // 翻转 X
        EditorGUILayout.BeginHorizontal();
        changeFlipX = EditorGUILayout.Toggle(changeFlipX, GUILayout.Width(20));
        GUI.enabled = changeFlipX;
        targetFlipX = EditorGUILayout.Toggle("翻转 X (Flip X)", targetFlipX);
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        // 翻转 Y
        EditorGUILayout.BeginHorizontal();
        changeFlipY = EditorGUILayout.Toggle(changeFlipY, GUILayout.Width(20));
        GUI.enabled = changeFlipY;
        targetFlipY = EditorGUILayout.Toggle("翻转 Y (Flip Y)", targetFlipY);
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        // 绘制模式
        EditorGUILayout.BeginHorizontal();
        changeDrawMode = EditorGUILayout.Toggle(changeDrawMode, GUILayout.Width(20));
        GUI.enabled = changeDrawMode;
        targetDrawMode = (SpriteDrawMode)EditorGUILayout.EnumPopup("绘制模式 (Draw Mode)", targetDrawMode);
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        // 遮罩交互
        EditorGUILayout.BeginHorizontal();
        changeMaskInteraction = EditorGUILayout.Toggle(changeMaskInteraction, GUILayout.Width(20));
        GUI.enabled = changeMaskInteraction;
        targetMaskInteraction = (SpriteMaskInteraction)EditorGUILayout.EnumPopup("遮罩交互 (Mask Interaction)", targetMaskInteraction);
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        // Sprite排序点
        EditorGUILayout.BeginHorizontal();
        changeSpriteSortPoint = EditorGUILayout.Toggle(changeSpriteSortPoint, GUILayout.Width(20));
        GUI.enabled = changeSpriteSortPoint;
        targetSpriteSortPoint = (SpriteSortPoint)EditorGUILayout.EnumPopup("Sprite排序点 (Sort Point)", targetSpriteSortPoint);
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        // 材质
        EditorGUILayout.BeginHorizontal();
        changeMaterial = EditorGUILayout.Toggle(changeMaterial, GUILayout.Width(20));
        GUI.enabled = changeMaterial;
        targetMaterial = (Material)EditorGUILayout.ObjectField("材质 (Material)", targetMaterial, typeof(Material), false);
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        DrawSeparator();
        EditorGUILayout.Space();

        GUILayout.Label("排序设置", EditorStyles.boldLabel);

        // 排序图层
        EditorGUILayout.BeginHorizontal();
        changeSortingLayer = EditorGUILayout.Toggle(changeSortingLayer, GUILayout.Width(20));
        GUI.enabled = changeSortingLayer;
        
        // 获取所有Sorting Layer
        string[] sortingLayerNames = GetSortingLayerNames();
        int currentIndex = System.Array.IndexOf(sortingLayerNames, targetSortingLayer);
        if (currentIndex < 0) currentIndex = 0;
        
        int newIndex = EditorGUILayout.Popup("排序图层 (Sorting Layer)", currentIndex, sortingLayerNames);
        targetSortingLayer = sortingLayerNames[newIndex];
        
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        // 图层顺序
        EditorGUILayout.BeginHorizontal();
        changeOrderInLayer = EditorGUILayout.Toggle(changeOrderInLayer, GUILayout.Width(20));
        GUI.enabled = changeOrderInLayer;
        targetOrderInLayer = EditorGUILayout.IntField("图层顺序 (Order in Layer)", targetOrderInLayer);
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        DrawSeparator();
        EditorGUILayout.Space();

        // 应用按钮
        GUI.enabled = foundPrefabs.Count > 0;
        
        GUILayout.BeginHorizontal();
        
        if (GUILayout.Button("全选参数", GUILayout.Height(25)))
        {
            SelectAllParameters(true);
        }
        
        if (GUILayout.Button("全不选", GUILayout.Height(25)))
        {
            SelectAllParameters(false);
        }
        
        GUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (GUILayout.Button("应用设置到所有预制体", GUILayout.Height(40)))
        {
            ApplySettings();
        }

        GUI.enabled = true;

        EditorGUILayout.EndScrollView();
    }

    private void ScanSelectedAssets()
    {
        foundPrefabs.Clear();

        Object[] selections = Selection.objects;
        
        if (selections.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请在Project窗口选择文件或文件夹", "确定");
            return;
        }

        foreach (Object obj in selections)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            
            if (System.IO.Directory.Exists(path))
            {
                // 是文件夹，扫描所有预制体
                string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { path });
                foreach (string guid in guids)
                {
                    string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                    CheckAndAddPrefab(prefabPath);
                }
            }
            else if (path.EndsWith(".prefab"))
            {
                // 是预制体文件
                CheckAndAddPrefab(path);
            }
        }

        Debug.Log($"<color=green>扫描完成！找到 {foundPrefabs.Count} 个含有SpriteRenderer的预制体</color>");
        
        if (foundPrefabs.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "未找到含有SpriteRenderer的预制体", "确定");
        }
    }

    private void CheckAndAddPrefab(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab != null && prefab.GetComponent<SpriteRenderer>() != null)
        {
            if (!foundPrefabs.Contains(prefab))
            {
                foundPrefabs.Add(prefab);
            }
        }
    }

    private void ApplySettings()
    {
        if (foundPrefabs.Count == 0) return;

        int changedCount = 0;
        
        foreach (GameObject prefab in foundPrefabs)
        {
            SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
            if (sr == null) continue;

            bool changed = false;

            // 应用勾选的参数
            if (changeSprite)
            {
                sr.sprite = targetSprite;
                changed = true;
            }

            if (changeColor)
            {
                sr.color = targetColor;
                changed = true;
            }

            if (changeFlipX)
            {
                sr.flipX = targetFlipX;
                changed = true;
            }

            if (changeFlipY)
            {
                sr.flipY = targetFlipY;
                changed = true;
            }

            if (changeDrawMode)
            {
                sr.drawMode = targetDrawMode;
                changed = true;
            }

            if (changeMaskInteraction)
            {
                sr.maskInteraction = targetMaskInteraction;
                changed = true;
            }

            if (changeSpriteSortPoint)
            {
                sr.spriteSortPoint = targetSpriteSortPoint;
                changed = true;
            }

            if (changeMaterial)
            {
                sr.sharedMaterial = targetMaterial;
                changed = true;
            }

            if (changeSortingLayer)
            {
                sr.sortingLayerName = targetSortingLayer;
                changed = true;
            }

            if (changeOrderInLayer)
            {
                sr.sortingOrder = targetOrderInLayer;
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(prefab);
                changedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=green>设置完成！修改了 {changedCount} 个预制体</color>");
        EditorUtility.DisplayDialog("完成", $"成功修改了 {changedCount} 个预制体！", "确定");
    }

    private void SelectAllParameters(bool select)
    {
        changeSprite = select;
        changeColor = select;
        changeFlipX = select;
        changeFlipY = select;
        changeDrawMode = select;
        changeMaskInteraction = select;
        changeSpriteSortPoint = select;
        changeMaterial = select;
        changeSortingLayer = select;
        changeOrderInLayer = select;
    }

    private string[] GetSortingLayerNames()
    {
        System.Type internalEditorUtilityType = typeof(UnityEditorInternal.InternalEditorUtility);
        System.Reflection.PropertyInfo sortingLayersProperty = internalEditorUtilityType.GetProperty("sortingLayerNames", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        return (string[])sortingLayersProperty.GetValue(null, new object[0]);
    }

    private void DrawSeparator()
    {
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
    }
}

