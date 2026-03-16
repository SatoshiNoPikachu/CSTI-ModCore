using System;
using System.Reflection;
using LitJson;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ModCore.Data;

public static partial class Loader
{
    /// <summary>
    /// 修复数据
    /// </summary>
    /// <param name="obj">对象</param>
    /// <param name="jsonData">Json数据</param>
    /// <param name="mod">模组</param>
    /// <param name="curField">当前字段信息，供序列对象使用</param>
    /// <param name="parent">当前字段信息，供序列对象使用</param>
    public static void FixData(object? obj, JsonData jsonData, ModData? mod, FieldInfo? curField = null,
        JsonData? parent = null)
    {
        if (obj is null)
        {
            Plugin.Log.LogWarning("Cannot fix data, because object is null!");
            return;
        }

        var type = obj.GetType();

        if (jsonData.IsObject)
        {
            foreach (var fieldName in jsonData.Keys)
            {
                if (fieldName.EndsWith("WarpData") || fieldName.EndsWith("WarpType")) continue;

                var jsonField = jsonData[fieldName];
                if (!jsonField.IsObject && !jsonField.IsArray) continue;

                var field = GetField(type, fieldName);
                if (field is null) continue;
                if (field.IsNotSerialized) continue;

                try
                {
                    var fieldType = field.FieldType;
                    if (fieldType.IsSubclassOf(typeof(Object)))
                    {
                        WarpDataOfObject(obj, field, jsonData, mod);
                        continue;
                    }

                    if (!fieldType.IsSerializable) continue;

                    var fieldValue = field.GetValue(obj);
                    if (fieldValue is not null && IsGameType(fieldType))
                    {
                        if (jsonField.IsArray && fieldType.IsArray)
                        {
                            var elementType = fieldType.GetElementType()!;
                            if (elementType.IsSubclassOf(typeof(Object)))
                            {
                                fieldValue = Array.CreateInstance(elementType, GetWarpDataCount(fieldName, jsonData));
                            }
                        }

                        FixData(fieldValue, jsonField, mod, field, jsonData);
                    }
                    else if (jsonField.IsArray)
                    {
                        if (fieldType.IsArray)
                        {
                            var elementType = fieldType.GetElementType()!;
                            fieldValue = Array.CreateInstance(elementType, elementType.IsSubclassOf(typeof(Object))
                                ? GetWarpDataCount(fieldName, jsonData)
                                : jsonField.Count);
                        }
                        else
                        {
                            fieldValue = Activator.CreateInstance(fieldType);
                        }

                        FixData(fieldValue, jsonField, mod, field, jsonData);
                    }
                    else
                    {
                        fieldValue = FromJson(fieldType, jsonField, mod);
                    }

                    field.SetValue(obj, fieldValue);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError(e);
                }
            }
        }
        else if (jsonData.IsArray)
        {
            try
            {
                if (type.IsArray)
                {
                    var elementType = type.GetElementType();
                    if (elementType is null)
                    {
                        Plugin.Log.LogWarning($"Unable get element type for {type}.");
                        return;
                    }

                    var arr = (Array)obj;
                    if (arr.Rank > 1)
                    {
                        Plugin.Log.LogWarning($"Does not support multidimensional arrays: {type}.");
                        return;
                    }

                    if (elementType.IsSubclassOf(typeof(Object)))
                    {
                        if (curField is null || parent is null)
                        {
                            Plugin.Log.LogWarning("FixData on Array missing parameters.");
                            return;
                        }

                        WarpDataOfArray(arr, curField, elementType, parent, mod);
                        return;
                    }

                    if (IsSkipFixElementType(elementType)) return;

                    if (IsGameType(elementType))
                    {
                        for (var i = 0; i < jsonData.Count; i++)
                        {
                            var element = arr.GetValue(i);
                            FixData(element, jsonData[i], mod);
                            if (elementType.IsValueType) arr.SetValue(element, i);
                        }

                        return;
                    }

                    for (var i = 0; i < jsonData.Count; i++)
                    {
                        var element = FromJson(elementType, jsonData[i], mod);
                        arr.SetValue(element, i);
                    }
                }
                else if (GetIListType(type, out var elementType))
                {
                    if (obj is not IList list)
                    {
                        Plugin.Log.LogWarning("Object type is not implement IList interface.");
                        return;
                    }

                    if (elementType!.IsSubclassOf(typeof(Object)))
                    {
                        if (curField is null || parent is null)
                        {
                            Plugin.Log.LogWarning("FixData on IList missing parameters.");
                            return;
                        }

                        WarpDataOfIList(list, curField, elementType, parent, mod);
                        return;
                    }

                    if (IsSkipFixElementType(elementType)) return;

                    if (IsGameType(elementType))
                    {
                        for (var i = 0; i < jsonData.Count; i++)
                        {
                            var element = list[i];
                            FixData(list[i], jsonData[i], mod);
                            if (elementType.IsValueType) list[i] = element;
                        }

                        return;
                    }

                    for (var i = 0; i < jsonData.Count; i++)
                    {
                        var element = FromJson(elementType, jsonData[i], mod);
                        list.Add(element);
                    }
                }
                else Plugin.Log.LogWarning("JsonData is Array, but object is not supported type.");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }
        else Plugin.Log.LogWarning("JsonData type is not supported.");
    }

    /// <summary>
    /// JSON反序列化
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="jsonData">JSON数据</param>
    /// <param name="mod">模组</param>
    /// <returns>对象</returns>
    private static object FromJson(Type type, JsonData jsonData, ModData? mod)
    {
        var obj = Activator.CreateInstance(type);
        JsonUtility.FromJsonOverwrite(jsonData.ToJson(), obj);
        FixData(obj, jsonData, mod);
        return obj;
    }

    /// <summary>
    /// JSON反序列化
    /// </summary>
    /// <param name="json">JSON字符串</param>
    /// <param name="mod">模组</param>
    /// <typeparam name="T">类型</typeparam>
    /// <returns>对象</returns>
    public static T FromJson<T>(string json, ModData? mod)
    {
        var type = typeof(T);
        var obj = type.IsSubclassOf(typeof(ScriptableObject))
            ? ScriptableObject.CreateInstance(type)
            : Activator.CreateInstance(type);
        JsonUtility.FromJsonOverwrite(json, obj);
        FixData(obj, JsonMapper.ToObject(json), mod);
        return (T)obj;
    }

    /// <summary>
    /// JSON反序列化覆写对象
    /// </summary>
    /// <param name="json">JSON字符串</param>
    /// <param name="obj">对象</param>
    /// <param name="mod">模组</param>
    public static void FromJsonOverwrite(string json, object obj, ModData? mod)
    {
        JsonUtility.FromJsonOverwrite(json, obj);
        FixData(obj, JsonMapper.ToObject(json), mod);
    }
}