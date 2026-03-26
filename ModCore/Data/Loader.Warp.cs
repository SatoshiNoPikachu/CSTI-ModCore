using System;
using System.Reflection;
using LitJson;

namespace ModCore.Data;

public static partial class Loader
{
    /// <summary>
    /// 获取映射对象
    /// </summary>
    /// <param name="type">数据类型</param>
    /// <param name="key">数据键或UID</param>
    /// <param name="mod">模组</param>
    /// <returns>对应数据键或UID的对象</returns>
    private static object? GetWarpObject(Type type, string key, ModData? mod)
    {
        if (type.IsSubclassOf(typeof(UniqueIDScriptable)))
        {
            var obj = _uidMap.GetValueOrDefault(key);
            if (obj is null) return null;
            if (type.IsInstanceOfType(obj)) return obj;

            Plugin.Log.LogWarning($"Cannot assign value of type {type} to {obj.GetType()}.");
        }
        else
        {
            var index = key.IndexOf('|');
            if (index < 0) return Database.GetData(type, key, mod);

            var typeName = key[..index];
            if (!DataInfos.TryGetValue(typeName, out var info))
            {
                Plugin.Log.LogWarning($"Data type {typeName} not registered.");
                return null;
            }

            var targetType = info.Type;
            if (type.IsAssignableFrom(targetType)) return Database.GetData(targetType, key[(index + 1)..], mod);

            Plugin.Log.LogWarning($"Cannot assign value of type {type} to {typeName}({targetType}).");
        }

        return null;
    }

    /// <summary>
    /// 对象映射数据
    /// </summary>
    /// <param name="obj">对象</param>
    /// <param name="field">字段信息</param>
    /// <param name="jsonData">Json数据</param>
    /// <param name="mod">模组</param>
    private static void WarpDataOfObject(object obj, FieldInfo field, JsonData jsonData, ModData? mod)
    {
        var warpName = $"{field.Name}WarpData";
        if (!jsonData.ContainsKey(warpName)) return;

        var warpData = jsonData[warpName];
        if (!warpData.IsString) return;

        var unityObj = GetWarpObject(field.FieldType, warpData.ToString(), mod);
        if (unityObj is null) return;

        field.SetValue(obj, unityObj);
    }

    /// <summary>
    /// 数组映射数据
    /// </summary>
    /// <param name="arr">数组对象</param>
    /// <param name="field">字段信息</param>
    /// <param name="elementType">元素类型</param>
    /// <param name="jsonData">Json数据</param>
    /// <param name="mod">模组</param>
    private static void WarpDataOfArray(Array arr, FieldInfo field, Type elementType, JsonData jsonData, ModData? mod)
    {
        var warpName = $"{field.Name}WarpData";
        if (!jsonData.ContainsKey(warpName)) return;

        var warpData = jsonData[warpName];
        if (!warpData.IsArray) return;

        var count = warpData.Count;
        for (var i = 0; i < count; i++)
        {
            var data = warpData[i];
            if (!data.IsString) continue;

            var unityObj = GetWarpObject(elementType, data.ToString(), mod);
            if (unityObj is null) continue;

            arr.SetValue(unityObj, i);
        }
    }

    /// <summary>
    /// 列表映射数据
    /// </summary>
    /// <param name="list">实现IList接口的对象</param>
    /// <param name="field">字段信息</param>
    /// <param name="elementType">元素类型</param>
    /// <param name="jsonData">Json数据</param>
    /// <param name="mod">模组</param>
    private static void WarpDataOfIList(IList list, FieldInfo field, Type elementType, JsonData jsonData, ModData? mod)
    {
        var warpName = $"{field.Name}WarpData";
        if (!jsonData.ContainsKey(warpName)) return;

        var warpData = jsonData[warpName];
        if (!warpData.IsArray) return;

        var count = warpData.Count;
        for (var i = 0; i < count; i++)
        {
            var data = warpData[i];
            if (!data.IsString) continue;

            var unityObj = GetWarpObject(elementType, data.ToString(), mod);
            if (unityObj is null) continue;

            list.Add(unityObj);
        }
    }

    /// <summary>
    /// 获取映射数据的数量
    /// </summary>
    /// <param name="fieldName">字段名称</param>
    /// <param name="jsonData">Json数据</param>
    /// <returns>数量</returns>
    private static int GetWarpDataCount(string fieldName, JsonData jsonData)
    {
        var warpName = $"{fieldName}WarpData";
        return jsonData.ContainsKey(warpName) ? jsonData[warpName].Count : 0;
    }
}