#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class SceneOrganizer
{
    [MenuItem("Tools/Cascade/Organize Scene")] 
    public static void Organize()
    {
        // 根分组
        var root = Ensure("Primary");
        var core = EnsureChild(root, "_Core");
        var managers = EnsureChild(root, "_Managers");
        var world = EnsureChild(root, "_World");
        var debug = EnsureChild(root, "_Debug");
        var systems = GameObject.Find("Systems");

        // 迁移/合并：优先使用已有同名对象，避免重复组件
        MoveIfExists("GameManager", core.transform);
        MoveIfExists("EventSystem", core.transform);

        MoveIfExists("InventorySystem", managers.transform);
        MoveIfExists("EquipmentSystem", managers.transform);
        MoveIfExists("InputSystem", managers.transform);
        MoveIfExists("HotbarSelection", managers.transform);
        MoveIfExists("GameInputManager", managers.transform);

        MoveIfExists("WorldSpawnService", world.transform);
        MoveIfExists("NavGrid2D", world.transform);

        MoveIfExists("InventoryBootstrap", debug.transform);
        MoveIfExists("WorldSpawnDebug", debug.transform);
        MoveIfExists("TestOBJ", debug.transform);

        // 若存在我们创建的 Systems 容器，保持禁用，防止双实例冲突
        if (systems != null)
        {
            systems.SetActive(false);
            systems.name = "Systems_Disabled";
            Debug.Log("[SceneOrganizer] Disabled duplicate 'Systems' object to avoid conflicts.");
        }
        Debug.Log("[SceneOrganizer] Organize done.");
    }

    static GameObject Ensure(string name)
    {
        var go = GameObject.Find(name);
        if (go == null) go = new GameObject(name);
        return go;
    }

    static GameObject EnsureChild(GameObject parent, string name)
    {
        var t = parent.transform.Find(name);
        if (t != null) return t.gameObject;
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        return go;
    }

    static void MoveIfExists(string name, Transform parent)
    {
        var go = GameObject.Find(name);
        if (go == null) return;
        go.transform.SetParent(parent, true);
    }
}
#endif
