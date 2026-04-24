using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using HarmonyLib;

namespace ModCore.Data;

public static partial class Loader
{
    /// <summary>
    /// 字段信息缓存
    /// </summary>
    private static ConcurrentDictionary<Type, ConcurrentDictionary<string, Lazy<FieldInfo>>> _cacheFields = [];

    /// <summary>
    /// IList类型缓存
    /// </summary>
    private static ConcurrentDictionary<Type, Type?> _cacheIListTypes = [];

    /// <summary>
    /// 游戏类型缓存
    /// </summary>
    private static ConcurrentDictionary<Type, bool> _cacheGameTypes = [];

    /// <summary>
    /// 清除缓存
    /// </summary>
    private static void ClearCache()
    {
        _cacheFields = [];
        _cacheIListTypes = [];
        _cacheGameTypes = [];
    }

    /// <summary>
    /// 获取字段信息
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="name">字段名称</param>
    /// <returns>字段信息</returns>
    public static FieldInfo? GetField(Type type, string name)
    {
        var fields = _cacheFields.GetOrAdd(type, _ => []);
        return fields.GetOrAdd(name,
                _ => new Lazy<FieldInfo>(() => AccessTools.Field(type, name),
                    LazyThreadSafetyMode.ExecutionAndPublication))
            .Value;
    }

    /// <summary>
    /// 生成数据字典。
    /// </summary>
    /// <param name="type">值类型。</param>
    /// <returns>数据字典。</returns>
    public static IDictionary? MakeDataDict(Type type)
    {
        try
        {
            var dictType = typeof(Dictionary<,>).MakeGenericType(typeof(string), type);
            return (IDictionary)Activator.CreateInstance(dictType);
        }
        catch (Exception)
        {
            Plugin.Log.LogWarning($"Type {type} cannot be used as a dictionary value type.");
            return null;
        }
    }

    /// <summary>
    /// 生成指定容量的数据字典。
    /// </summary>
    /// <param name="type">值类型。</param>
    /// <param name="capacity">容量。</param>
    /// <returns>数据字典。</returns>
    public static IDictionary? MakeDataDict(Type type, int capacity)
    {
        try
        {
            var dictType = typeof(Dictionary<,>).MakeGenericType(typeof(string), type);
            return (IDictionary)Activator.CreateInstance(dictType, capacity);
        }
        catch (Exception)
        {
            Plugin.Log.LogWarning($"Type {type} cannot be used as a dictionary value type.");
            return null;
        }
    }

    /// <summary>
    /// 获取IList接口泛型参数类型
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="listType">IList泛型参数类型</param>
    /// <returns>是否实现了泛型IList</returns>
    public static bool GetIListType(Type type, out Type? listType)
    {
        listType = _cacheIListTypes.GetOrAdd(type, static t =>
        {
            foreach (var i in t.GetInterfaces())
            {
                if (i.IsConstructedGenericType && i.GetGenericTypeDefinition() == typeof(IList<>))
                    return i.GetGenericArguments()[0];
            }

            return null;
        });

        return listType is not null;
    }

    /// <summary>
    /// 是否是游戏类型
    /// </summary>
    /// <param name="type">类型</param>
    /// <returns>是否是游戏类型</returns>
    public static bool IsGameType(Type type)
    {
        return _cacheGameTypes.GetOrAdd(type, static t =>
        {
            if (t.IsArray)
            {
                t = t.GetElementType()!;
            }
            else if (GetIListType(t, out var listType))
            {
                t = listType!;
            }

            var a = t.Assembly;
            return !a.IsDynamic && a.Location.StartsWith(BepInEx.Paths.ManagedPath, StringComparison.Ordinal);
        });
    }

    /// <summary>
    /// 是否是无需修补的数据类型
    /// </summary>
    /// <param name="type">类型</param>
    /// <returns>是否是无需修补的数据类型</returns>
    private static bool IsSkipFixElementType(Type type)
    {
        if (type.IsPrimitive) return true;
        if (type.IsEnum) return true;

        return type == typeof(string) || !type.IsSerializable;
    }
}