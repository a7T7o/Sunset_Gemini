#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// 三向动画融合生成器（中文界面）
/// - 融合 LayerAnimSetupTool（生成动画 + 应用Pivot）与 SliceAnimControllerTool（生成Controller）
/// - 固定 8 帧；命名：{动作类型}_{方向}_Clip_{itemId}_{quality}
/// - 仅创建 Any State → 目标状态 的过渡，不创建状态之间的互转
/// - Controller 参数：State, Direction, ToolItemId, ToolQuality
/// </summary>
public class TriDirectionalFusionTool : EditorWindow
{
    public enum ActionType
    {
        Slice = 6,
        Pierce = 7,
        Crush = 8,
    }

    // ━━━━ 基本设置 ━━━━
    ActionType actionType = ActionType.Slice;
    int itemId = 0;
    int qualityCount = 1; // 品质数量（单个数字）
    bool applyPivot = true;
    string itemName = ""; // 用于自定义目录后缀
    int totalFrames = 60;  // 动画总帧数（按60FPS计算秒）
    int lastFrame = 60;    // 最后一帧（帧分布截止帧）

    // ━━━━ 输入 ━━━━
    DefaultAsset spriteRootFolder;   // 包含 Down/Side/Up 的文件夹
    DefaultAsset sourcePivotFolder;  // 包含 {ActionType}_{Direction} 的 Aseprite 源（可选）

    // ━━━━ 输出 ━━━━
    string clipsOutputBase = "Assets/Animations/Clips"; // 用户可浏览选择
    string controllerOutputFolder = "Assets/Animations/Controllers";

    Vector2 scroll;

    [MenuItem("Tools/三向动画融合生成器")]
    static void Open()
    {
        var win = GetWindow<TriDirectionalFusionTool>("三向动画融合生成器");
        win.minSize = new Vector2(560, 520);
        win.Show();
    }

    void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        GUILayout.Label("━━━━ 基本设置 ━━━━", EditorStyles.boldLabel);
        actionType = (ActionType)EditorGUILayout.EnumPopup("动作类型", actionType);
        itemId = EditorGUILayout.IntField("物品ID (itemId)", itemId);
        qualityCount = Mathf.Max(1, EditorGUILayout.IntField("品质数量 (>=1)", qualityCount));
        applyPivot = EditorGUILayout.Toggle("生成前应用Pivot(可选)", applyPivot);
        itemName = EditorGUILayout.TextField("物品名称(用于文件夹后缀)", itemName);

        GUILayout.Label("━━━━ 时间轴设置 ━━━━", EditorStyles.boldLabel);
        totalFrames = Mathf.Max(1, EditorGUILayout.IntField("动画总帧数", totalFrames));
        lastFrame = Mathf.Clamp(EditorGUILayout.IntField("最后一帧", lastFrame), 1, totalFrames);
        EditorGUILayout.HelpBox($"sprite将均匀分布在前{lastFrame}帧，最后{Mathf.Max(0, totalFrames - lastFrame)}帧保持最后一个sprite（60FPS）", MessageType.Info);
        EditorGUILayout.Space(6);

        GUILayout.Label("━━━━ 输入 ━━━━", EditorStyles.boldLabel);
        spriteRootFolder = EditorGUILayout.ObjectField("Sprite根文件夹", spriteRootFolder, typeof(DefaultAsset), false) as DefaultAsset;
        EditorGUILayout.HelpBox("拖入包含 Down/Side/Up 子文件夹的根目录。例如：Sprites/Slice/{id}_{物品名称}/", MessageType.Info);
        sourcePivotFolder = EditorGUILayout.ObjectField("源Pivot文件夹(可选)", sourcePivotFolder, typeof(DefaultAsset), false) as DefaultAsset;
        EditorGUILayout.HelpBox("拖入包含 {动作类型}_{方向} 的Aseprite导出资源的文件夹，例如：Slice_Down、Slice_Side、Slice_Up。用于读取 per-frame Pivot 并应用到目标 Sprite。", MessageType.None);
        EditorGUILayout.Space(6);

        GUILayout.Label("━━━━ 输出 ━━━━", EditorStyles.boldLabel);
        // Clips 输出
        EditorGUILayout.BeginHorizontal();
        clipsOutputBase = EditorGUILayout.TextField("Clips输出基础路径", clipsOutputBase);
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            var p = EditorUtility.OpenFolderPanel("选择Clips输出文件夹", clipsOutputBase, "");
            if (!string.IsNullOrEmpty(p)) clipsOutputBase = ConvertToAssetPath(p);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.HelpBox($"最终输出：{clipsOutputBase}/{GetActionName()}/{GetItemFolderName()}/{{Down|Side|Up}}/{{ClipName}}.anim", MessageType.Info);

        // Controller 输出
        EditorGUILayout.BeginHorizontal();
        controllerOutputFolder = EditorGUILayout.TextField("Controller输出路径", controllerOutputFolder);
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            var p = EditorUtility.OpenFolderPanel("选择Controller输出文件夹", controllerOutputFolder, "");
            if (!string.IsNullOrEmpty(p)) controllerOutputFolder = ConvertToAssetPath(p);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.HelpBox($"将生成：{controllerOutputFolder}/{GetActionName()}/{GetItemFolderName()}/{GetActionName()}_Controller_{GetItemFolderName()}.controller", MessageType.Info);
        EditorGUILayout.Space(10);

        // 操作按钮
        GUI.enabled = spriteRootFolder != null && itemId >= 0 && qualityCount >= 1;
        if (GUILayout.Button("一键生成：动画 + 控制器", GUILayout.Height(42)))
        {
            try
            {
                GenerateAll();
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError(e);
                EditorUtility.DisplayDialog("错误", e.Message, "确定");
            }
        }
        GUI.enabled = true;

        EditorGUILayout.EndScrollView();
    }

    void GenerateAll()
    {
        string spriteRoot = AssetDatabase.GetAssetPath(spriteRootFolder);
        if (string.IsNullOrEmpty(spriteRoot))
            throw new Exception("Sprite根文件夹无效");

        // 方向集合
        string[] dirs = new[] { "Down", "Side", "Up" };
        var action = GetActionName();

        // 预备输出目录
        foreach (var d in dirs)
        {
            string outDir = Path.Combine(clipsOutputBase, action, GetItemFolderName(), d).Replace("\\", "/");
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
        }

        // 可选：应用Pivot
        if (applyPivot && sourcePivotFolder != null)
        {
            ApplyPivotForAll(spriteRoot, action, dirs);
        }

        // 生成动画 Clips
        var createdClips = new List<AnimationClip>();
        foreach (var d in dirs)
        {
            string dirPath = Path.Combine(spriteRoot, d).Replace("\\", "/");
            if (!Directory.Exists(dirPath))
            {
                Debug.LogWarning($"未找到方向文件夹: {d}");
                continue;
            }

            // 收集该方向所有 sprite（按 品质→帧序号）
            var map = CollectDirectionSprites(dirPath);

            for (int q = 0; q < qualityCount; q++)
            {
                if (!map.TryGetValue(q, out var frames) || frames.Count == 0)
                {
                    Debug.LogWarning($"[{d}] 品质{q} 未找到任何帧，跳过");
                    continue;
                }
                // 只取前8帧，若不足则按可用帧生成
                var eight = frames.OrderBy(s => ExtractFrameIndex(s.name)).Take(8).ToList();
                string clipName = $"{action}_{d}_Clip_{itemId}_{q}";
                string outDir = Path.Combine(clipsOutputBase, action, GetItemFolderName(), d).Replace("\\", "/");
                var clip = CreateClipWithTiming(
                    eight,
                    Path.Combine(outDir, clipName + ".anim").Replace("\\", "/"),
                    totalFrames,
                    lastFrame
                );
                if (clip != null) createdClips.Add(clip);
            }
        }

        // 生成 Controller（仅 Any State → 目标；无状态互相转换）
        var controller = CreateController(action, itemId, createdClips);

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成", $"✅ 生成完成\nClips: {createdClips.Count}\nController: {(controller != null ? AssetDatabase.GetAssetPath(controller) : "-")}", "确定");
    }

    string GetActionName()
    {
        switch (actionType)
        {
            case ActionType.Slice: return "Slice";
            case ActionType.Pierce: return "Pierce";
            case ActionType.Crush: return "Crush";
            default: return "Slice";
        }
    }

    void ApplyPivotForAll(string spriteRoot, string action, string[] dirs)
    {
        string pivotRoot = AssetDatabase.GetAssetPath(sourcePivotFolder);
        foreach (var d in dirs)
        {
            var pivotAsset = FindPivotAsset(pivotRoot, $"{action}_{d}");
            if (pivotAsset == null)
            {
                Debug.LogWarning($"[Pivot] 未找到 {action}_{d} 的源，跳过");
                continue;
            }
            var pivots = GetPivotsFromAseprite(pivotAsset);
            if (pivots.Count == 0)
            {
                Debug.LogWarning($"[Pivot] {action}_{d} 源未读取到 pivots，跳过");
                continue;
            }

            string dirPath = Path.Combine(spriteRoot, d).Replace("\\", "/");
            foreach (var tex in FindTexturesInFolder(dirPath))
            {
                ApplyPivotsToTexture(tex, pivots);
            }
        }
    }

    UnityEngine.Object FindPivotAsset(string baseFolder, string expectedName)
    {
        string[] guids = AssetDatabase.FindAssets("", new[] { baseFolder });
        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            string file = Path.GetFileNameWithoutExtension(path);
            if (file == expectedName)
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex != null) return tex;
                var all = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var a in all) if (a is Sprite) return a;
            }
        }
        return null;
    }

    Dictionary<int, List<Sprite>> CollectDirectionSprites(string dirPath)
    {
        var map = new Dictionary<int, List<Sprite>>();
        foreach (var tex in FindTexturesInFolder(dirPath))
        {
            var path = AssetDatabase.GetAssetPath(tex);
            var all = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var obj in all)
            {
                if (obj is Sprite sp)
                {
                    if (TryParseQualityAndFrame(sp.name, out int q, out int f))
                    {
                        if (!map.TryGetValue(q, out var list))
                        {
                            list = new List<Sprite>();
                            map[q] = list;
                        }
                        list.Add(sp);
                    }
                }
            }
        }
        return map;
    }

    bool TryParseQualityAndFrame(string name, out int quality, out int frame)
    {
        quality = 0; frame = 0;
        // 规则：{名称}_{品质}_{帧序号}
        var parts = name.Split('_');
        if (parts.Length < 3) return false;
        // 取倒数第二、倒数第一段
        if (!int.TryParse(parts[parts.Length - 2], out quality)) return false;
        if (!int.TryParse(parts[parts.Length - 1], out frame)) return false;
        return true;
    }

    int ExtractFrameIndex(string name)
    {
        if (TryParseQualityAndFrame(name, out var _, out var f)) return f;
        // 兜底：末尾数字
        int end = name.Length - 1; int start = end;
        while (start >= 0 && char.IsDigit(name[start])) start--;
        if (start < end && int.TryParse(name.Substring(start + 1), out int idx)) return idx;
        return 0;
    }

    Texture2D[] FindTexturesInFolder(string folderPath)
    {
        var list = new List<Texture2D>();
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null) list.Add(tex);
        }
        return list.OrderBy(t => t.name).ToArray();
    }

    AnimationClip CreateClipWithTiming(List<Sprite> frames, string clipAssetPath, int totalFrames, int lastFrame)
    {
        if (frames == null || frames.Count == 0) return null;
        var sprites = frames.OrderBy(s => ExtractFrameIndex(s.name)).ToList();
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipAssetPath);
        bool isNew = clip == null;
        if (isNew) clip = new AnimationClip(); else clip.ClearCurves();

        var binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        int count = Mathf.Min(8, sprites.Count);
        float fps = 60f;
        float totalTime = Mathf.Max(1, totalFrames) / fps;
        float holdStartTime = Mathf.Clamp(lastFrame, 1, Mathf.Max(1, totalFrames)) / fps; // 最后段开始时间

        // 关键帧：前 count-1 张均匀分布在 [0, holdStartTime)
        // 最后一张在 holdStartTime 切入，并在 totalTime 处再插一个相同关键帧以固定片段长度
        var keyList = new List<ObjectReferenceKeyframe>();
        if (count == 1)
        {
            // 只有一张：0s 和 totalTime 两个关键帧
            keyList.Add(new ObjectReferenceKeyframe { time = 0f, value = sprites[0] });
            if (totalTime > 0f)
                keyList.Add(new ObjectReferenceKeyframe { time = totalTime, value = sprites[0] });
        }
        else
        {
            for (int i = 0; i < count - 1; i++)
            {
                float t = (count - 1 <= 1) ? 0f : (i / (float)(count - 1)) * holdStartTime;
                keyList.Add(new ObjectReferenceKeyframe { time = t, value = sprites[i] });
            }
            // 最后一张在 holdStartTime
            keyList.Add(new ObjectReferenceKeyframe { time = holdStartTime, value = sprites[count - 1] });
            // 追加终点关键帧，保持最后一张到 totalTime
            if (totalTime > holdStartTime)
                keyList.Add(new ObjectReferenceKeyframe { time = totalTime, value = sprites[count - 1] });
        }

        var keys = keyList.ToArray();
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        clip.frameRate = 60f;

        if (isNew)
            AssetDatabase.CreateAsset(clip, clipAssetPath);
        else
            EditorUtility.SetDirty(clip);

        return clip;
    }

    string GetItemFolderName()
    {
        string suffix = SanitizeName(itemName ?? "");
        return string.IsNullOrEmpty(suffix) ? itemId.ToString() : $"{itemId}_{suffix}";
    }

    string SanitizeName(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var chars = raw.Where(c => !invalid.Contains(c)).ToArray();
        var s = new string(chars);
        // 进一步去掉首尾空格并把连续空白替换为下划线
        s = s.Trim();
        if (s.Length == 0) return string.Empty;
        var arr = s.Select(ch => char.IsWhiteSpace(ch) ? '_' : ch).ToArray();
        return new string(arr);
    }

    AnimatorController CreateController(string action, int itemId, List<AnimationClip> clips)
    {
        // Controllers/{Action}/{id}_{itemName}/
        if (!Directory.Exists(controllerOutputFolder)) Directory.CreateDirectory(controllerOutputFolder);
        string actionFolder = Path.Combine(controllerOutputFolder, action).Replace("\\", "/");
        if (!Directory.Exists(actionFolder)) Directory.CreateDirectory(actionFolder);
        string itemFolder = Path.Combine(actionFolder, GetItemFolderName()).Replace("\\", "/");
        if (!Directory.Exists(itemFolder)) Directory.CreateDirectory(itemFolder);

        string ctrlPath = Path.Combine(itemFolder, $"{action}_Controller_{GetItemFolderName()}.controller").Replace("\\", "/");
        if (File.Exists(ctrlPath)) AssetDatabase.DeleteAsset(ctrlPath);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
        // 参数
        controller.AddParameter("State", AnimatorControllerParameterType.Int);
        controller.AddParameter("Direction", AnimatorControllerParameterType.Int);
        controller.AddParameter("ToolItemId", AnimatorControllerParameterType.Int);
        controller.AddParameter("ToolQuality", AnimatorControllerParameterType.Int);

        var baseLayer = controller.layers[0];
        var sm = baseLayer.stateMachine;
        var idle = sm.AddState("Idle", new Vector3(100, 0, 0));
        sm.defaultState = idle;

        // 遍历clips，创建状态并添加 Any State → 状态 过渡
        foreach (var clip in clips)
        {
            string name = clip.name; // {Action}_{Dir}_Clip_{itemId}_{q}
            if (!TryParseStateMeta(name, out int dirVal, out int q)) continue;

            var st = sm.AddState(name, new Vector3(400 + dirVal * 220, q * 70, 0));
            st.motion = clip;

            // Any State -> state
            var anyTrans = sm.AddAnyStateTransition(st);
            anyTrans.hasExitTime = false;
            anyTrans.duration = 0f;
            anyTrans.AddCondition(AnimatorConditionMode.Equals, (int)actionType, "State");
            anyTrans.AddCondition(AnimatorConditionMode.Equals, dirVal, "Direction");
            anyTrans.AddCondition(AnimatorConditionMode.Equals, itemId, "ToolItemId");
            anyTrans.AddCondition(AnimatorConditionMode.Equals, q, "ToolQuality");

            // 按你的规范：仅 Any State → 目标状态，不创建状态之间或状态→Idle 的转换
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    bool TryParseStateMeta(string clipName, out int dirVal, out int q)
    {
        dirVal = 2; // Side 默认 2
        q = 0;
        string lower = clipName.ToLower();
        if (lower.Contains("down")) dirVal = 0;
        else if (lower.Contains("up")) dirVal = 1;
        else dirVal = 2;
        int us = clipName.LastIndexOf('_');
        if (us >= 0 && int.TryParse(clipName.Substring(us + 1), out int qq)) { q = qq; return true; }
        return false;
    }

    // ====== Pivot 读取与应用（来自现有工具思路） ======
    List<Vector2> GetPivotsFromAseprite(UnityEngine.Object asepriteFile)
    {
        var pivots = new List<Vector2>();
        if (asepriteFile == null) return pivots;
        string path = AssetDatabase.GetAssetPath(asepriteFile);
        if (asepriteFile is Sprite) path = AssetDatabase.GetAssetPath((asepriteFile as Sprite).texture);
        var all = AssetDatabase.LoadAllAssetsAtPath(path);
        var sprites = new List<Sprite>();
        foreach (var a in all) if (a is Sprite s) sprites.Add(s);
        if (sprites.Count == 0) return pivots;
        sprites.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
        foreach (var s in sprites)
        {
            Vector2 pivotPixels = s.pivot; Vector2 size = s.rect.size;
            pivots.Add(new Vector2(pivotPixels.x / size.x, pivotPixels.y / size.y));
        }
        return pivots;
    }

    void ApplyPivotsToTexture(Texture2D texture, List<Vector2> pivots)
    {
        string path = AssetDatabase.GetAssetPath(texture);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        if (!importer.isReadable) importer.isReadable = true;
        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        if (provider == null) return;
        provider.InitSpriteEditorDataProvider();
        var rects = provider.GetSpriteRects();
        if (rects == null || rects.Length == 0) return;
        var sorted = rects.OrderBy(r => r.name).ToArray();
        int count = Mathf.Min(sorted.Length, pivots.Count);
        for (int i = 0; i < count; i++) { sorted[i].pivot = pivots[i]; sorted[i].alignment = SpriteAlignment.Custom; }
        provider.SetSpriteRects(sorted);
        provider.Apply();
        importer.SaveAndReimport();
    }

    string ConvertToAssetPath(string absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath)) return "";
        string dataPath = Application.dataPath;
        if (absolutePath.StartsWith(dataPath)) return ("Assets" + absolutePath.Substring(dataPath.Length)).Replace("\\", "/");
        return absolutePath.Replace("\\", "/");
    }
}
#endif
