using System;
using System.Collections;
using System.Collections.Generic;

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
    public static Dictionary<string, T> GetData<T>()
    {
        return AllData.TryGetValue(typeof(T), out var dict) ? dict as Dictionary<string, T> : null;
    }

    /// <summary>
    /// 获取数据字典
    /// </summary>
    /// <param name="type">数据类型</param>
    /// <returns></returns>
    public static IDictionary GetData(Type type)
    {
        if (type is null) return null;
        return AllData.TryGetValue(type, out var dict) ? dict : null;
    }

    /// <summary>
    /// 根据键获取数据
    /// </summary>
    /// <param name="key">数据键</param>
    /// <typeparam name="T">数据类型</typeparam>
    /// <returns>数据对象</returns>
    public static T GetData<T>(string key)
    {
        var dict = GetData<T>();
        if (dict is null) return default;
        return dict.TryGetValue(key, out var data) ? data : default;
    }

    /// <summary>
    /// 获取数据
    /// </summary>
    /// <param name="type">数据类型</param>
    /// <param name="key">数据键</param>
    /// <returns>数据对象</returns>
    public static object GetData(Type type, string key)
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
    public static IEnumerable<T> GetData<T>(IEnumerable<string> keys, bool skipInvalid = true)
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
        if (type is null) return;
        if (AllData.ContainsKey(type)) return;

        AllData[type] = dict;
    }

    /// <summary>
    /// 添加数据
    /// </summary>
    /// <param name="dict">数据对象字典</param>
    /// <typeparam name="T">数据类型</typeparam>
    public static void AddData<T>(Dictionary<string, T> dict)
    {
        var type = typeof(T);
        if (AllData.ContainsKey(type)) return;
        AllData[type] = dict;
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