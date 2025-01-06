using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using HarmonyLib;
using LitJson;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ModCore.Data;

/// <summary>
/// 加载器
/// </summary>
public static class Loader
{
    /// <summary>
    /// 加载之前事件
    /// </summary>
    public static event Action LoadBeforeEvent;

    /// <summary>
    /// 加载完成事件
    /// </summary>
    public static event Action LoadCompleteEvent;

    /// <summary>
    /// 是否加载完成
    /// </summary>
    public static bool IsLoaded { get; private set; }

    /// <summary>
    /// 数据信息集合
    /// </summary>
    private static HashSet<DataInfo> _dataInfos = [];

    /// <summary>
    /// 预加载数据
    /// </summary>
    private static List<(object, JsonData)> _preloadData = [];

    /// <summary>
    /// 字段信息缓存
    /// </summary>
    private static Dictionary<Type, Dictionary<string, FieldInfo>> _cacheFields = [];

    /// <summary>
    /// 注册类型
    /// </summary>
    /// <param name="info">数据信息</param>
    /// <returns>是否注册成功</returns>
    public static bool RegisterType(DataInfo info)
    {
        if (IsLoaded || info.Type is null) return false;
        if (!_dataInfos.Add(info)) return false;

        Plugin.Log.LogInfo($"Registered type {info.Type.FullName} in loader.");
        return true;
    }

    /// <summary>
    /// 注册类型
    /// </summary>
    /// <param name="infos">数据信息</param>
    public static void RegisterTypes(IEnumerable<DataInfo> infos)
    {
        if (IsLoaded) return;

        foreach (var info in infos)
        {
            RegisterType(info);
        }
    }

    /// <summary>
    /// 加载全部数据
    /// </summary>
    internal static void LoadAllData()
    {
        if (IsLoaded) return;

        LoadBeforeEvent?.Invoke();
        InitDatabaseAndAutoRegisterType();

        var modDirs = new DirectoryInfo(BepInEx.Paths.PluginPath).GetDirectories();

        LoadAllTexture2D(modDirs, "Resource/Texture2D");

        var warpData = new List<(object, JsonData)>();
        foreach (var info in _dataInfos)
        {
            Plugin.Log.LogMessage($"Load data {info.Name}");

            var data = LoadData(info, modDirs);
            if (data is null) continue;
            warpData.AddRange(data);
        }

        foreach (var (obj, jsonData) in _preloadData)
        {
            FixData(obj, jsonData);

            if (obj is not UniqueIDScriptable uidObj) continue;

            if (UniqueIDScriptable.AllUniqueObjects.ContainsKey(uidObj.UniqueID))
            {
                Plugin.Log.LogWarning($"Preload not register same uid {uidObj.UniqueID}.");
            }
            else
            {
                uidObj.Init();
            }

            GameLoad.Instance.DataBase.AllData.Add(uidObj);
        }

        foreach (var (obj, jsonData) in warpData)
        {
            FixData(obj, jsonData);
        }

        LoadCompleteEvent?.Invoke();
        _dataInfos = null;
        _preloadData = null;
        _cacheFields = [];
        IsLoaded = true;
    }

    /// <summary>
    /// 初始化数据并自动注册类型
    /// </summary>
    private static void InitDatabaseAndAutoRegisterType()
    {
        foreach (var type in AccessTools.AllTypes())
        {
            if (type.IsInterface || type.IsGenericTypeDefinition) continue;

            var isSo = type.IsSubclassOf(typeof(ScriptableObject));
            if (isSo)
            {
                var objs = Resources.FindObjectsOfTypeAll(type);
                if (objs?.Length > 0)
                {
                    var dict = Database.GetData(type) ?? MakeDataDict(type);
                    foreach (var obj in objs)
                    {
                        var so = (ScriptableObject)obj;
                        var name = so.name;
                        if (dict.Contains(name))
                        {
                            Plugin.Log.LogWarning($"{type.Name} has same name {name}.");
                            continue;
                        }

                        dict.Add(name, so);
                    }

                    Database.AddData(type, dict);
                }
            }

            if (type.IsAbstract) continue;
            if (!type.IsSerializable && !isSo) continue;

            var attr = type.GetCustomAttribute<DataInfoAttribute>();
            if (attr is null) continue;

            RegisterType(new DataInfo(type, string.IsNullOrWhiteSpace(attr.Name) ? type.Name : attr.Name));
        }
    }

    /// <summary>
    /// 加载全部2D纹理
    /// </summary>
    /// <param name="dirs">目录信息</param>
    /// <param name="texPath">纹理路径</param>
    /// <returns>精灵字典</returns>
    private static void LoadAllTexture2D(IEnumerable<DirectoryInfo> dirs, string texPath)
    {
        var sprites = Database.GetData<Sprite>();
        if (sprites is null)
        {
            sprites = new Dictionary<string, Sprite>();
            Database.AddData(sprites);
        }

        foreach (var sprite in Resources.FindObjectsOfTypeAll<Sprite>())
        {
            if (sprites.ContainsKey(sprite.name)) continue;
            sprites.Add(sprite.name, sprite);
        }

        foreach (var dir in dirs)
        {
            var path = Path.Combine(dir.FullName, texPath);
            if (!Directory.Exists(path)) continue;

            var files = new DirectoryInfo(path).GetFiles();
            foreach (var file in files)
            {
                if (file.Extension.ToLower() is not (".png" or ".jpg" or ".jpeg")) continue;

                var name = Path.GetFileNameWithoutExtension(file.Name);
                if (sprites.ContainsKey(name))
                {
                    Plugin.Log.LogWarning($"{dir.Name} not load texture same name {name}.");
                    continue;
                }

                var bytes = File.ReadAllBytes(file.FullName);
                var tex = new Texture2D(0, 0)
                {
                    name = name
                };
                if (!tex.LoadImage(bytes))
                {
                    Object.Destroy(tex);
                    continue;
                }

                var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
                sprite.name = name;

                sprites.Add(name, sprite);
            }
        }
    }

    /// <summary>
    /// 加载数据
    /// </summary>
    /// <param name="info">数据信息</param>
    /// <param name="modDirs">目录信息</param>
    /// <returns>对象和Json数据的元组的可迭代对象</returns>
    private static IEnumerable<(object, JsonData)> LoadData(DataInfo info, IEnumerable<DirectoryInfo> modDirs)
    {
        var type = info.Type;
        var dataName = info.Name;

        var isSo = type.IsSubclassOf(typeof(ScriptableObject));
        var isUidObj = type.IsSubclassOf(typeof(UniqueIDScriptable));

        var allUidObj = GameLoad.Instance.DataBase.AllData;

        var dict = Database.GetData(type) ?? MakeDataDict(type);
        var list = new List<(object, JsonData)>();

        foreach (var modDir in modDirs)
        {
            var path = isUidObj
                ? Path.Combine(modDir.FullName, dataName)
                : Path.Combine(modDir.FullName, "ScriptableObject", dataName);
            if (!Directory.Exists(path)) continue;

            var files = new DirectoryInfo(path).GetFiles("*.json");
            foreach (var file in files)
            {
                var name = Path.GetFileNameWithoutExtension(file.Name);
                if (dict.Contains(name))
                {
                    Plugin.Log.LogWarning($"{modDir.Name} not load {dataName} same name {name}.");
                    continue;
                }

                object obj = null;
                try
                {
                    obj = isSo ? ScriptableObject.CreateInstance(type) : Activator.CreateInstance(type);
                    var json = File.ReadAllText(file.FullName, Encoding.UTF8);
                    JsonUtility.FromJsonOverwrite(json, obj);

                    if (isSo)
                    {
                        var so = (ScriptableObject)obj;
                        so.name = name;
                    }

                    if (isUidObj)
                    {
                        var uidObj = (UniqueIDScriptable)obj;
                        var uid = uidObj.UniqueID;
                        if (UniqueIDScriptable.AllUniqueObjects.ContainsKey(uid))
                        {
                            Plugin.Log.LogWarning($"{modDir.Name} not load {dataName} same uid {uid}.");
                            Object.Destroy(uidObj);
                            continue;
                        }

                        uidObj.Init();
                        allUidObj.Add(uidObj);
                    }

                    dict.Add(name, obj);

                    list.Add((obj, JsonMapper.ToObject(json)));
                }
                catch (Exception e)
                {
                    if (isSo && obj != null) Object.Destroy((Object)obj);

                    Plugin.Log.LogError(e);
                }
            }
        }

        Database.AddData(type, dict);

        return list;
    }

    /// <summary>
    /// 预加载数据，仅供非UnityEngine.Object类型提供相同名称检查
    /// </summary>
    /// <param name="name">对象名称</param>
    /// <param name="json">JSON字符串</param>
    /// <typeparam name="T">数据类型</typeparam>
    public static void PreloadData<T>(string name, string json)
    {
        if (IsLoaded) return;

        var type = typeof(T);
        if (Database.GetData(type)?.Contains(name) is true) return;

        var obj = (T)(type.IsSubclassOf(typeof(ScriptableObject))
            ? ScriptableObject.CreateInstance(type)
            : Activator.CreateInstance(type));
        JsonUtility.FromJsonOverwrite(json, obj);

        if (obj is Object unityObj)
        {
            unityObj.name = name;
        }
        else
        {
            Database.AddObject(name, obj);
        }

        _preloadData.Add((obj, JsonMapper.ToObject(json)));
    }

    /// <summary>
    /// 修复数据
    /// </summary>
    /// <param name="obj">对象</param>
    /// <param name="jsonData">Json数据</param>
    /// <param name="curField">当前字段信息，供序列对象使用</param>
    /// <param name="parent">当前字段信息，供序列对象使用</param>
    public static void FixData(object obj, JsonData jsonData, FieldInfo curField = null, JsonData parent = null)
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
                        WarpDataOfObject(obj, field, jsonData);
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

                        FixData(fieldValue, jsonField, field, jsonData);
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

                        FixData(fieldValue, jsonField, field, jsonData);
                    }
                    else
                    {
                        fieldValue = FromJson(fieldType, jsonField);
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
                        WarpDataOfArray(arr, curField, elementType, parent);
                        return;
                    }

                    if (IsSkipFixElementType(elementType)) return;

                    for (var i = 0; i < jsonData.Count; i++)
                    {
                        var element = FromJson(elementType, jsonData[i]);
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

                    if (elementType.IsSubclassOf(typeof(Object)))
                    {
                        WarpDataOfIList(list, curField, elementType, parent);
                        return;
                    }

                    if (IsSkipFixElementType(elementType)) return;

                    for (var i = 0; i < jsonData.Count; i++)
                    {
                        var element = FromJson(elementType, jsonData[i]);
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
    /// 获取映射对象
    /// </summary>
    /// <param name="type">数据类型</param>
    /// <param name="key">名称或UID</param>
    /// <returns></returns>
    private static Object GetWarpObject(Type type, string key)
    {
        var unityObj = type.IsSubclassOf(typeof(UniqueIDScriptable))
            ? UniqueIDScriptable.GetFromID(key)
            : Database.GetData(type, key) as Object;
        if (!unityObj) return null;

        if (unityObj.GetType() == type) return unityObj;
        Plugin.Log.LogWarning("UID object type is different from target type.");
        return null;
    }

    /// <summary>
    /// 对象映射数据
    /// </summary>
    /// <param name="obj">对象</param>
    /// <param name="field">字段信息</param>
    /// <param name="jsonData">Json数据</param>
    private static void WarpDataOfObject(object obj, FieldInfo field, JsonData jsonData)
    {
        var warpName = $"{field.Name}WarpData";
        if (!jsonData.ContainsKey(warpName)) return;

        var warpData = jsonData[warpName];
        if (!warpData.IsString) return;

        var unityObj = GetWarpObject(field.FieldType, warpData.ToString());
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
    private static void WarpDataOfArray(Array arr, FieldInfo field, Type elementType, JsonData jsonData)
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

            var unityObj = GetWarpObject(elementType, data.ToString());
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
    private static void WarpDataOfIList(IList list, FieldInfo field, Type elementType, JsonData jsonData)
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

            var unityObj = GetWarpObject(elementType, data.ToString());
            if (unityObj is null) continue;

            list.Add(unityObj);
        }
    }

    /// <summary>
    /// 获取映射数据的数量
    /// </summary>
    /// <param name="fieldName">字段名称</param>
    /// <param name="jsonData">Json数据</param>
    /// <returns></returns>
    private static int GetWarpDataCount(string fieldName, JsonData jsonData)
    {
        var warpName = $"{fieldName}WarpData";
        return jsonData.ContainsKey(warpName) ? jsonData[warpName].Count : 0;
    }

    /// <summary>
    /// 获取字段信息
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="name">字段名称</param>
    /// <returns>字段信息</returns>
    public static FieldInfo GetField(Type type, string name)
    {
        if (_cacheFields.TryGetValue(type, out var fields))
        {
            if (fields.TryGetValue(name, out var f)) return f;
        }
        else
        {
            fields = [];
            _cacheFields[type] = fields;
        }

        var field = AccessTools.Field(type, name);
        fields[name] = field;

        return field;
    }

    /// <summary>
    /// JSON反序列化
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="jsonData">JSON数据</param>
    /// <returns>对象</returns>
    private static object FromJson(Type type, JsonData jsonData)
    {
        var obj = Activator.CreateInstance(type);
        JsonUtility.FromJsonOverwrite(jsonData.ToJson(), obj);
        FixData(obj, jsonData);
        return obj;
    }

    /// <summary>
    /// JSON反序列化
    /// </summary>
    /// <param name="json">JSON字符串</param>
    /// <typeparam name="T">类型</typeparam>
    /// <returns>对象</returns>
    public static T FromJson<T>(string json)
    {
        var type = typeof(T);
        var obj = type.IsSubclassOf(typeof(ScriptableObject))
            ? ScriptableObject.CreateInstance(type)
            : Activator.CreateInstance(type);
        JsonUtility.FromJsonOverwrite(json, obj);
        FixData(obj, JsonMapper.ToObject(json));
        return (T)obj;
    }

    /// <summary>
    /// JSON反序列化覆写对象
    /// </summary>
    /// <param name="json">JSON字符串</param>
    /// <param name="obj">对象</param>
    public static void FromJsonOverwrite(string json, object obj)
    {
        JsonUtility.FromJsonOverwrite(json, obj);
        FixData(obj, JsonMapper.ToObject(json));
    }

    /// <summary>
    /// 生成数据字典
    /// </summary>
    /// <param name="type">值类型</param>
    /// <returns>数据字典</returns>
    public static IDictionary MakeDataDict(Type type)
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
    public static bool GetIListType(Type type, out Type listType)
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
            type = listType;
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