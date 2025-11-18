using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using LitJson;
using ModCore.Services;
using ModCore.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ModCore.Data;

/// <summary>
/// 加载器
/// </summary>
public static class Loader
{
    public const string DataPath = "Data";

    public const string GameSourceModifyPath = $"{DataPath}/GameSourceModify";

    public const string DataObjectModifyPath = $"{DataPath}/DataObjectModify";

    /// <summary>
    /// 加载之前事件
    /// </summary>
    public static event Action? LoadBeforeEvent;

    /// <summary>
    /// 加载完成事件
    /// </summary>
    public static event Action? LoadCompleteEvent;

    /// <summary>
    /// 是否加载完成
    /// </summary>
    public static bool IsLoaded { get; private set; }

    /// <summary>
    /// 数据信息集合
    /// </summary>
    private static readonly Dictionary<string, DataInfo> DataInfos = [];

    /// <summary>
    /// 预加载数据
    /// </summary>
    private static List<(object Obj, JsonData JsonData)>? _preloadData = [];

    /// <summary>
    /// 字段信息缓存
    /// </summary>
    private static ConcurrentDictionary<Type, ConcurrentDictionary<string, Lazy<FieldInfo>>> _cacheFields = [];

    /// <summary>
    /// 注册类型
    /// </summary>
    /// <param name="info">数据信息</param>
    /// <returns>是否注册成功</returns>
    public static bool RegisterType(DataInfo info)
    {
        if (IsLoaded) return false;
        if (DataInfos.ContainsKey(info.Name)) return false;

        DataInfos[info.Name] = info;
        Plugin.Log.LogInfo($"Registered type {info.Name} ({info.Type.FullName}) in loader.");
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
    /// 异步加载
    /// </summary>
    internal static async void LoadAsync()
    {
        try
        {
            if (IsLoaded) return;

            LoadingScreen.SetText(LoadingScreen.TextInit);

            InitDatabaseAndAutoRegisterType();

            await Task.Yield();

            if (!LoadingScreen.CheckGameLoad()) return;

            LoadBeforeEvent?.Invoke();

            LoadingScreen.SetText(LoadingScreen.TextLoadAsset);

            var sw = Stopwatch.StartNew();

            var taskJson = LoadJsonDataAsync();
            var taskTex = TextureLoader.LoadTexture2DAndSpriteAsync();
            await taskTex;

            var objMap = await taskJson;
            LoadingScreen.SetText(LoadingScreen.TextFixData);
            await FixDataAsync(objMap);

            await ModifyAsync();

            sw.Stop();
            Plugin.Log.LogMessage($"Total loading time: {sw.ElapsedMilliseconds}ms");

            LoadingScreen.SetText(LoadingScreen.TextProcessEvent);

            DataMap.Mapping();

            LoadCompleteEvent?.Invoke();

            _preloadData = null;
            _cacheFields = [];
            IsLoaded = true;

            LoadingScreen.Loaded();
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError(ex);
            LoadingScreen.OnError();
        }
    }

    /// <summary>
    /// 初始化数据并自动注册类型
    /// </summary>
    private static void InitDatabaseAndAutoRegisterType()
    {
        foreach (var type in AccessTools.AllTypes())
        {
            if (type.IsInterface || type.IsGenericTypeDefinition) continue;
            if (type.IsAbstract) continue;

            var isSo = type.IsSubclassOf(typeof(ScriptableObject));
            if (isSo)
            {
                var objs = Resources.FindObjectsOfTypeAll(type);
                if (objs?.Length > 0)
                {
                    var dict = Database.GetData(type) ?? MakeDataDict(type);
                    if (dict is null) continue;

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

                if (type.Module.Name is "Assembly-CSharp.dll") RegisterType(new DataInfo(type, false));
            }

            if (!type.IsSerializable && !isSo) continue;

            var attr = type.GetCustomAttribute<DataInfoAttribute>();
            if (attr is null) continue;

            RegisterType(new DataInfo(type, string.IsNullOrWhiteSpace(attr.Name) ? type.Name : attr.Name,
                attr.CanFallbackToRoot));
        }
    }

    /// <summary>
    /// 异步读取文本文件
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <returns>文件文本</returns>
    private static async Task<string> ReadFileTextAsync(string path)
    {
        using var reader = new StreamReader(path, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    /// <summary>
    /// Json加载时上下文
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="path"></param>
    private class JsonLoadingContext(object obj, string path)
    {
        /// <summary>
        /// 对象
        /// </summary>
        public object Obj { get; } = obj;

        /// <summary>
        /// 文件路径
        /// </summary>
        public string Path { get; } = path;

        /// <summary>
        /// 有效的
        /// </summary>
        public bool Valid { get; set; } = true;
    }

    /// <summary>
    /// 异步加载Json数据
    /// </summary>
    /// <returns></returns>
    private static async Task<Dictionary<ModData, List<JsonLoadingContext>>> LoadJsonDataAsync()
    {
        var objMap = new Dictionary<ModData, List<JsonLoadingContext>>();
        var objContexts = objMap.SelectMany(kvp => kvp.Value);

        foreach (var dataInfo in DataInfos.Values)
        {
            var type = dataInfo.Type;
            var dataName = dataInfo.Name;

            var isSo = type.IsSubclassOf(typeof(ScriptableObject));
            var isUidObj = type.IsSubclassOf(typeof(UniqueIDScriptable));

            var dict = Database.GetData(type) ?? MakeDataDict(type);
            if (dict is null) continue;

            Database.AddData(type, dict);

            foreach (var mod in ModService.GetMods())
            {
                var dataDirPath = isUidObj
                    ? Path.Combine(mod.RootPath, DataPath, dataName)
                    : Path.Combine(mod.RootPath, DataPath, "ScriptableObject", dataName);
                if (!Directory.Exists(dataDirPath))
                {
                    if (!dataInfo.CanFallbackToRoot) continue;

                    dataDirPath = isUidObj
                        ? Path.Combine(mod.RootPath, dataName)
                        : Path.Combine(mod.RootPath, "ScriptableObject", dataName);

                    if (!Directory.Exists(dataDirPath)) continue;
                }

                if (!objMap.TryGetValue(mod, out var list))
                {
                    list = [];
                    objMap.Add(mod, list);
                }

                var files = Directory.EnumerateFiles(dataDirPath, "*.json", SearchOption.AllDirectories);
                foreach (var filePath in files)
                {
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    if (ModData.HasNamespace(fileName))
                    {
                        Plugin.Log.LogWarning(
                            $"{mod.Namespace} not load the name contains namespace separator from {filePath}.");
                        continue;
                    }

                    var name = $"{mod.Namespace}:{fileName}";
                    if (dict.Contains(name))
                    {
                        Plugin.Log.LogWarning($"{mod.Namespace} not load same name {dataName} from {filePath}.");
                        continue;
                    }

                    try
                    {
                        var obj = isSo ? ScriptableObject.CreateInstance(type) : Activator.CreateInstance(type);
                        if (isSo) ((ScriptableObject)obj).name = name;

                        dict.Add(name, obj);
                        list.Add(new JsonLoadingContext(obj, filePath));
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.LogWarning($"{mod.Namespace} load {dataName} failed: {ex}");
                    }
                }
            }
        }

        await Task.Run(() =>
        {
            Parallel.ForEach(objContexts, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                context =>
                {
                    try
                    {
                        var json = File.ReadAllText(context.Path, Encoding.UTF8);
                        JsonUtility.FromJsonOverwrite(json, context.Obj);
                    }
                    catch (Exception ex)
                    {
                        context.Valid = false;
                        Plugin.Log.LogWarning($"Loading {context.Path} failed: {ex}");
                    }
                });
        });

        var allUidObj = GameLoad.Instance.DataBase.AllData;
        var allUidObjDict = UniqueIDScriptable.AllUniqueObjects;

        foreach (var (obj, _) in _preloadData!)
        {
            if (obj is not UniqueIDScriptable uidObj) continue;

            var uid = uidObj.UniqueID;
            if (allUidObjDict.ContainsKey(uid))
            {
                Plugin.Log.LogWarning($"Preload not register same uid {uid}.");
                continue;
            }

            uidObj.Init();
            allUidObj.Add(uidObj);
        }

        foreach (var (mod, contexts) in objMap)
        {
            foreach (var context in contexts)
            {
                if (!context.Valid || context.Obj is not UniqueIDScriptable uidObj) continue;

                var uid = uidObj.UniqueID;
                if (allUidObjDict.ContainsKey(uid))
                {
                    Plugin.Log.LogWarning(
                        $"{mod.Namespace} has same uid {uid} of type {uidObj.GetType().Name} from {context.Path}.");
                    continue;
                }

                uidObj.Init();
                allUidObj.Add(uidObj);
            }
        }

        return objMap;
    }

    private static async Task FixDataAsync(Dictionary<ModData, List<JsonLoadingContext>> objMap)
    {
        await Task.Run(() =>
        {
            Parallel.ForEach(_preloadData!, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                context => FixData(context.Obj, context.JsonData, null));
        });

        await Task.Run(async () =>
        {
            var sw = Stopwatch.StartNew();

            var maxSemaphoreNum = Environment.ProcessorCount;
            var semaphore = new SemaphoreSlim(maxSemaphoreNum);
            var tcs = new TaskCompletionSource<bool>();
            var flag = false;

            foreach (var (mod, contexts) in objMap)
            {
                foreach (var context in contexts.Where(context => context.Valid))
                {
                    await semaphore.WaitAsync();
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            // var json = File.ReadAllText(context.Path, Encoding.UTF8);
                            var json = await ReadFileTextAsync(context.Path);
                            FixData(context.Obj, JsonMapper.ToObject(json), mod);
                        }
                        catch (Exception ex)
                        {
                            Plugin.Log.LogWarning($"{mod.Namespace} fix data failed from {context.Path}: {ex}");
                        }
                        finally
                        {
                            semaphore.Release();
                            // ReSharper disable once AccessToModifiedClosure
                            if (flag && semaphore.CurrentCount == maxSemaphoreNum) tcs.TrySetResult(true);
                        }
                    });
                }
            }

            flag = true;

            if (semaphore.CurrentCount != maxSemaphoreNum)
            {
                await tcs.Task;
            }

            tcs.TrySetResult(true);

            sw.Stop();
            Plugin.Log.LogMessage($"Fix data time: {sw.ElapsedMilliseconds}ms");
        });
    }

    private static async Task ModifyAsync()
    {
        var semJson = new SemaphoreSlim(Environment.ProcessorCount);
        var semModify = new SemaphoreSlim(1);
        var tasks = new List<Task>();

        foreach (var mod in ModService.GetMods())
        {
            foreach (var (file, obj) in GetModifyTargets(Path.Combine(mod.RootPath, GameSourceModifyPath), true, mod)
                         .Concat(GetModifyTargets(Path.Combine(mod.RootPath, DataObjectModifyPath), false, mod)))
            {
                await semJson.WaitAsync();

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var jsonData = JsonMapper.ToObject(await ReadFileTextAsync(file));
                        await semModify.WaitAsync();
                        GameSourceModify(obj, jsonData, mod);
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.LogWarning($"Modify failed from {file}: {ex}");
                    }
                    finally
                    {
                        semJson.Release();
                        semModify.Release();
                    }
                }));
            }
        }

        await Task.WhenAll(tasks);
    }

    private static IEnumerable<(string FilePath, object Obj)> GetModifyTargets(string path, bool isUid, ModData mod)
    {
        if (!Directory.Exists(path)) yield break;

        var allUidObjDict = UniqueIDScriptable.AllUniqueObjects;

        foreach (var file in Directory.EnumerateFiles(path, "*.json", SearchOption.AllDirectories))
        {
            if (isUid)
            {
                var uid = Path.GetFileNameWithoutExtension(file);
                if (!allUidObjDict.TryGetValue(uid, out var uidObj))
                {
                    Plugin.Log.LogWarning($"Modify: Cannot find uid {uid} from {file}.");
                    continue;
                }

                if (!uidObj) continue;

                yield return (file, uidObj);
            }
            else
            {
                var dataName = Directory.GetParent(file)?.Name;
                if (dataName is null) continue;

                if (!DataInfos.TryGetValue(dataName, out var info))
                {
                    Plugin.Log.LogWarning($"Modify: Cannot find data name for {dataName} from {file}.");
                    continue;
                }

                var key = Path.GetFileNameWithoutExtension(file);
                var obj = Database.GetData(info.Type, key, mod);
                if (obj is null)
                {
                    Plugin.Log.LogWarning($"Modify: Cannot find data key {key} for {dataName} from {file}.");
                    continue;
                }

                yield return (file, obj);
            }
        }
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

        _preloadData!.Add((obj, JsonMapper.ToObject(json)));
    }

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

    // /// <summary>
    // /// 根据数据键获取映射对象
    // /// </summary>
    // /// <param name="type">数据类型</param>
    // /// <param name="key">数据键</param>
    // /// <param name="mod">模组</param>
    // /// <returns></returns>
    // private static Object? GetWarpObjectFromKey(Type type, string key, ModData? mod)
    // {
    //     if (mod is null) return Database.GetData(type, key) as Object;
    //
    //     if (!ModData.HasNamespace(key))
    //     {
    //         return (Database.GetData(type, key) ?? Database.GetData(type, $"{mod.Namespace}:{key}")) as Object;
    //     }
    //
    //     return Database.GetData(type, key) as Object;
    // }

    /// <summary>
    /// 获取映射对象
    /// </summary>
    /// <param name="type">数据类型</param>
    /// <param name="key">数据键或UID</param>
    /// <param name="mod">模组</param>
    /// <returns></returns>
    private static Object? GetWarpObject(Type type, string key, ModData? mod)
    {
        var unityObj = type.IsSubclassOf(typeof(UniqueIDScriptable))
            ? UniqueIDScriptable.AllUniqueObjects.TryGetValue(key, out var u) ? u : null
            : Database.GetData(type, key, mod) as Object;
        if (!unityObj) return null;

        if (unityObj!.GetType() == type) return unityObj;
        Plugin.Log.LogWarning("UID object type is different from target type.");
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
    public static FieldInfo? GetField(Type type, string name)
    {
        var fields = _cacheFields.GetOrAdd(type, _ => []);
        return fields.GetOrAdd(name,
                _ => new Lazy<FieldInfo>(() => AccessTools.Field(type, name),
                    LazyThreadSafetyMode.ExecutionAndPublication))
            .Value;
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
    /// <param name="mod"></param>
    public static void FromJsonOverwrite(string json, object obj, ModData? mod)
    {
        JsonUtility.FromJsonOverwrite(json, obj);
        FixData(obj, JsonMapper.ToObject(json), mod);
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

    /// <summary>
    /// 修改类型
    /// </summary>
    private enum WarpType
    {
        None,
        Copy,
        Custom,
        Reference,
        Add,
        Modify,
        AddReference
    }

    /// <summary>
    /// 游戏资源修改
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="jsonData"></param>
    /// <param name="mod">模组</param>
    public static void GameSourceModify(object obj, JsonData jsonData, ModData? mod)
    {
        try
        {
            ModifyMatchCardTag(obj, jsonData, mod);
            ModifyMatchCardType(obj, jsonData, mod);
            ModifyObject(obj, jsonData, mod);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning(ex);
        }
    }

    /// <summary>
    /// 修改匹配卡牌标签
    /// </summary>
    /// <param name="source">源对象</param>
    /// <param name="jsonData">Json数据</param>
    /// <param name="mod">模组</param>
    private static void ModifyMatchCardTag(object source, JsonData jsonData, ModData? mod)
    {
        if (source is not CardData || !jsonData.ContainsKey("MatchTagWarpData")) return;

        var warpData = jsonData["MatchTagWarpData"];
        jsonData.Remove("MatchTagWarpData");

        if (!warpData.IsArray) return;

        for (var i = 0; i < warpData.Count; i++)
        {
            var data = warpData[i];
            if (!data.IsString) continue;

            var tag = Database.GetData<CardTag>(data.ToString());
            if (tag is null) continue;

            foreach (var card in tag.GetCards())
            {
                if (ReferenceEquals(card, source)) continue;
                ModifyObject(card, jsonData, mod);
            }
        }
    }

    /// <summary>
    /// 修改匹配卡牌类型
    /// </summary>
    /// <param name="source">源对象</param>
    /// <param name="jsonData">Json数据</param>
    /// <param name="mod">模组</param>
    private static void ModifyMatchCardType(object source, JsonData jsonData, ModData? mod)
    {
        if (source is not CardData || !jsonData.ContainsKey("MatchTypeWarpData")) return;

        var warpData = jsonData["MatchTypeWarpData"];
        jsonData.Remove("MatchTypeWarpData");

        if (!warpData.IsString) return;

        if (!Enum.TryParse(warpData.ToString(), out CardTypes type))
        {
            Plugin.Log.LogWarning($"Unmatched CardTypes {warpData}");
            return;
        }

        foreach (var card in type.GetCards())
        {
            if (ReferenceEquals(card, source)) continue;
            ModifyObject(card, jsonData, mod);
        }
    }

    /// <summary>
    /// 修改对象
    /// </summary>
    /// <param name="obj">对象</param>
    /// <param name="jsonData">Json数据</param>
    /// <param name="mod">模组</param>
    public static void ModifyObject(object? obj, JsonData jsonData, ModData? mod)
    {
        if (obj is null) return;
        if (!jsonData.IsObject) return;

        var type = obj.GetType();

        foreach (var key in jsonData.Keys)
        {
            if (!key.EndsWith("WarpData")) continue;

            var jsonField = jsonData[key];
            if (!jsonField.IsObject && !jsonField.IsArray) continue;
            var fieldName = key.Substring(0, key.Length - 8);

            try
            {
                var warpType = (WarpType)(int)jsonData[$"{fieldName}WarpType"];

                var field = GetField(type, fieldName);
                if (field is null) continue;
                if (field.IsNotSerialized) continue;

                if (jsonField.IsObject)
                {
                    if (warpType is not WarpType.Modify) continue;
                }
                else
                {
                    ModifyArray(obj, field.GetValue(obj), jsonField, warpType, field, mod);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning(e);
            }
        }
    }

    /// <summary>
    /// 修改数组
    /// </summary>
    /// <param name="source">源对象</param>
    /// <param name="obj">目标数组</param>
    /// <param name="jsonData">Json数据</param>
    /// <param name="warpType">修改类型</param>
    /// <param name="field">字段信息</param>
    /// <param name="mod">模组</param>
    private static void ModifyArray(object source, object? obj, JsonData jsonData, WarpType warpType, FieldInfo field,
        ModData? mod)
    {
        if (obj is null) return;
        if (!jsonData.IsArray) return;

        var type = obj.GetType();
        // if (!field.FieldType.IsAssignableFrom(type)) return;

        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            if (elementType is null) return;

            var arr = (Array)obj;
            if (arr.Rank > 1) return;

            if (warpType is WarpType.AddReference)
            {
                if (!elementType.IsSubclassOf(typeof(Object))) return;

                var arrNew = Array.CreateInstance(elementType, arr.Length + jsonData.Count);
                for (var i = 0; i < arr.Length; i++)
                {
                    arrNew.SetValue(arr.GetValue(i), i);
                }

                ModifyWarpDataOfArray(arrNew, elementType, jsonData, arr.Length, mod);
                field.SetValue(source, arrNew);
            }
            else if (warpType is WarpType.Modify)
            {
                if (elementType.IsSubclassOf(typeof(Object))) return;

                for (var i = 0; i < jsonData.Count; i++)
                {
                    ModifyObject(arr.GetValue(i), jsonData[i], mod);
                }
            }
            else if (warpType is WarpType.Add)
            {
                if (elementType.IsSubclassOf(typeof(Object))) return;

                var arrNew = Array.CreateInstance(elementType, arr.Length + jsonData.Count);
                for (var i = 0; i < arr.Length; i++)
                {
                    arrNew.SetValue(arr.GetValue(i), i);
                }

                for (var i = 0; i < jsonData.Count; i++)
                {
                    arrNew.SetValue(FromJson(elementType, jsonData[i], null), i + arr.Length);
                }

                field.SetValue(source, arrNew);
            }
        }
        else if (GetIListType(type, out var elementType))
        {
            if (obj is not IList list)
            {
                Plugin.Log.LogWarning("Object type is not implement IList interface.");
                return;
            }

            if (warpType is WarpType.AddReference)
            {
                if (!elementType!.IsSubclassOf(typeof(Object))) return;

                ModifyWarpDataOfList(list, elementType, jsonData, mod);
            }
            else if (warpType is WarpType.Modify)
            {
                if (elementType!.IsSubclassOf(typeof(Object))) return;

                for (var i = 0; i < jsonData.Count; i++)
                {
                    ModifyObject(list[i], jsonData[i], mod);
                }
            }
            else if (warpType is WarpType.Add)
            {
                if (elementType!.IsSubclassOf(typeof(Object))) return;

                for (var i = 0; i < jsonData.Count; i++)
                {
                    list.Add(FromJson(elementType, jsonData[i], null));
                }
            }
        }
    }

    /// <summary>
    /// 修改数组映射数据
    /// </summary>
    /// <param name="arr">数组对象</param>
    /// <param name="elementType">元素类型</param>
    /// <param name="warpData">JSON数据</param>
    /// <param name="startIndex">起始索引</param>
    /// <param name="mod">模组</param>
    private static void ModifyWarpDataOfArray(Array arr, Type elementType, JsonData warpData, int startIndex,
        ModData? mod)
    {
        if (!warpData.IsArray) return;

        var count = warpData.Count;
        if (arr.Length < count + startIndex) return;

        for (var i = 0; i < count; i++)
        {
            var data = warpData[i];
            if (!data.IsString) continue;

            var unityObj = GetWarpObject(elementType, data.ToString(), null);
            if (unityObj is null) continue;

            arr.SetValue(unityObj, i + startIndex);
        }
    }

    /// <summary>
    /// 修改列表映射数据
    /// </summary>
    /// <param name="list">列表</param>
    /// <param name="elementType">元素类型</param>
    /// <param name="warpData">JSON数据</param>
    /// <param name="mod">模组</param>
    private static void ModifyWarpDataOfList(IList list, Type elementType, JsonData warpData, ModData? mod)
    {
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
}