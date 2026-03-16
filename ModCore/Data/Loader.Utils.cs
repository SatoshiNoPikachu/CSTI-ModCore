using System;
using System.Reflection;
using System.Threading;
using HarmonyLib;

namespace ModCore.Data;

public static partial class Loader
{
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
    /// 生成数据字典
    /// </summary>
    /// <param name="type">值类型</param>
    /// <returns>数据字典</returns>
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
    /// 获取IList接口泛型参数类型
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="listType">IList泛型参数类型</param>
    /// <returns>是否实现了泛型IList</returns>
    public static bool GetIListType(Type type, out Type? listType)
    {
        foreach (var t in type.GetInterfaces())
        {
            if (!t.IsConstructedGenericType || t.GetGenericTypeDefinition() != typeof(IList<>)) continue;

            listType = t.GetGenericArguments()[0];
            return true;
        }

        listType = null;
        return false;
    }
    
    /// <summary>
    /// 是否是游戏类型
    /// </summary>
    /// <param name="type">类型</param>
    /// <returns>是否是游戏类型</returns>
    public static bool IsGameType(Type type)
    {
        if (type.IsArray)
        {
            type = type.GetElementType()!;
        }
        else if (GetIListType(type, out var listType))
        {
            type = listType!;
        }

        var assembly = type.Assembly;
        return !assembly.IsDynamic && assembly.Location.StartsWith(BepInEx.Paths.ManagedPath);
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