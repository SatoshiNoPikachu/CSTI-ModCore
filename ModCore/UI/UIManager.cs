using UnityEngine;

namespace ModCore.UI;

/// <summary>
/// UI管理器
/// </summary>
public static class UIManager
{
    /// <summary>
    /// 预制件字典
    /// </summary>
    private static readonly Dictionary<string, Object> Prefabs = [];

    /// <summary>
    /// 注册预制件
    /// </summary>
    /// <param name="uid">唯一ID</param>
    /// <param name="go">游戏对象</param>
    /// <returns>是否注册成功</returns>
    public static bool RegisterPrefab(string uid, GameObject go)
    {
        if (!go || Prefabs.ContainsKey(uid)) return false;

        Object.DontDestroyOnLoad(go);
        Prefabs[uid] = go;
        return true;
    }

    /// <summary>
    /// 注册预制件
    /// </summary>
    /// <param name="uid">唯一ID</param>
    /// <param name="comp">组件</param>
    /// <returns>是否注册成功</returns>
    public static bool RegisterPrefab(string uid, Component comp)
    {
        if (!comp || Prefabs.ContainsKey(uid)) return false;

        Object.DontDestroyOnLoad(comp);
        Prefabs[uid] = comp;
        return true;
    }

    /// <summary>
    /// 获取预制件
    /// </summary>
    /// <param name="uid">唯一ID</param>
    /// <returns>预制件</returns>
    public static Object? GetPrefab(string uid)
    {
        return Prefabs.GetValueOrDefault(uid);
    }

    /// <summary>
    /// 获取预制件
    /// </summary>
    /// <param name="uid">唯一ID</param>
    /// <typeparam name="T">派生自组件类型</typeparam>
    /// <returns>预制件</returns>
    public static T? GetPrefab<T>(string uid) where T : Component
    {
        return GetPrefab(uid) as T;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="uid">唯一ID</param>
    /// <returns></returns>
    public static GameObject? GetPrefabAsGameObject(string uid)
    {
        return GetPrefab(uid) switch
        {
            GameObject go => go,
            Component comp => comp.gameObject,
            _ => null
        };
    }
}