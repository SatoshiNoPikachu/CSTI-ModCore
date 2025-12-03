using System;

namespace ModCore.Data;

/// <summary>
/// 数据库
/// </summary>
public static class Database
{
    /// <summary>
    /// 数据字典
    /// </summary>
    private static readonly Dictionary<Type, IDictionary> AllData = new();

    /// <summary>
    /// 获取数据字典
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <returns>数据字典</returns>
    public static Dictionary<string, T>? GetData<T>()
    {
        return AllData.TryGetValue(typeof(T), out var dict) ? dict as Dictionary<string, T> : null;
    }

    /// <summary>
    /// 获取数据字典
    /// </summary>
    /// <param name="type">数据类型</param>
    /// <returns></returns>
    public static IDictionary? GetData(Type type)
    {
        return AllData.GetValueOrDefault(type);
    }

    /// <summary>
    /// 根据键获取数据
    /// </summary>
    /// <param name="key">数据键</param>
    /// <typeparam name="T">数据类型</typeparam>
    /// <returns>数据对象</returns>
    public static T? GetData<T>(string key)
    {
        var dict = GetData<T>();
        return dict is null ? default : dict.GetValueOrDefault(key);
    }

    /// <summary>
    /// 获取数据
    /// </summary>
    /// <param name="type">数据类型</param>
    /// <param name="key">数据键</param>
    /// <returns>数据对象</returns>
    public static object? GetData(Type type, string key)
    {
        var dict = GetData(type);
        return dict?[key];
    }

    /// <summary>
    /// 根据多个键获取数据
    /// </summary>
    /// <param name="keys">可迭代的数据键序列</param>
    /// <param name="skipInvalid">跳过无效值</param>
    /// <typeparam name="T">数据类型</typeparam>
    /// <returns>数据列表</returns>
    public static IEnumerable<T?> GetData<T>(IEnumerable<string> keys, bool skipInvalid = true)
    {
        var dict = GetData<T>();
        if (dict is null) yield break;

        foreach (var key in keys)
        {
            if (dict.TryGetValue(key, out var data))
            {
                yield return data;
                continue;
            }

            if (skipInvalid) continue;
            yield return default;
        }
    }

    /// <summary>
    /// 添加数据
    /// </summary>
    /// <param name="type">数据类型</param>
    /// <param name="dict">数据对象字典</param>
    public static void AddData(Type type, IDictionary dict)
    {
        AllData.TryAdd(type, dict);
    }

    /// <summary>
    /// 添加数据
    /// </summary>
    /// <param name="dict">数据对象字典</param>
    /// <typeparam name="T">数据类型</typeparam>
    public static void AddData<T>(Dictionary<string, T> dict)
    {
        AllData.TryAdd(typeof(T), dict);
    }

    /// <summary>
    /// 添加数据对象
    /// </summary>
    /// <param name="key">数据键</param>
    /// <param name="obj">数据对象</param>
    /// <typeparam name="T">数据类型</typeparam>
    public static void AddObject<T>(string key, T obj)
    {
        var type = typeof(T);
        var dict = GetData<T>();
        if (dict is null)
        {
            dict = new Dictionary<string, T>
            {
                [key] = obj
            };
            AllData[type] = dict;
            return;
        }

        dict.TryAdd(key, obj);
    }

    /// <summary>
    /// 根据命名空间与数据键获取数据
    /// </summary>
    /// <param name="type">数据类型</param>
    /// <param name="key">数据键，如果不带有命名空间且模组不为空，则先尝试直接获取，如果获取失败则附加模组数据的命名空间再尝试获取</param>
    /// <param name="mod">模组</param>
    /// <returns>数据对象</returns>
    public static object? GetData(Type type, string key, ModData? mod)
    {
        if (mod is null || ModData.HasNamespace(key)) return GetData(type, key);

        return GetData(type, key) ?? GetData(type, $"{mod.Namespace}:{key}");
    }

    /// <summary>
    /// 根据命名空间与数据键获取数据
    /// </summary>
    /// <param name="key">数据键，如果不带有命名空间且模组不为空，则先尝试直接获取，如果获取失败则附加模组数据的命名空间再尝试获取</param>
    /// <param name="mod">模组</param>
    /// <typeparam name="T">数据类型</typeparam>
    /// <returns>数据对象</returns>
    public static T? GetData<T>(string key, ModData? mod)
    {
        if (mod is null || ModData.HasNamespace(key)) return GetData<T>(key);

        return GetData<T>(key) ?? GetData<T>($"{mod.Namespace}:{key}");
    }

    // /// <summary>
    // /// 清空数据
    // /// </summary>
    // public static void Clear()
    // {
    //     foreach (var obj in AllData.Values.SelectMany(data => data.Values))
    //     {
    //         if (obj is Object unityObj) Object.Destroy(unityObj);
    //     }
    //
    //     AllData.Clear();
    // }
}