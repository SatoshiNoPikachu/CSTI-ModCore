using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LitJson;
using ModCore.Services;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ModCore.Data;

/// <summary>
/// 加载器
/// </summary>
public static partial class Loader
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
    /// UID字典
    /// </summary>
    private static Dictionary<string, UniqueIDScriptable>? _uidMap;

    /// <summary>
    /// 异步加载
    /// </summary>
    internal static async void LoadAsync()
    {
        try
        {
            if (IsLoaded) return;

            LoadingScreen.SetText(LoadingScreen.TextInit);

            var op = await PreloadGameSceneAsync();

            InitDatabaseAndAutoRegisterType();

            await Task.Yield();

            if (!LoadingScreen.CheckGameLoad()) return;

            LoadBeforeEvent?.Invoke();

            LoadingScreen.SetText(LoadingScreen.TextLoadAsset);

            var sw = Stopwatch.StartNew();

            var taskData = LoadAssetAsync();
            await taskData;

            LoadingScreen.SetText(LoadingScreen.TextApplyModify);
            DataMap.Mapping();
            await ModifyAsync();

            sw.Stop();
            Plugin.Log.LogMessage($"Total loading time: {sw.ElapsedMilliseconds}ms");

            LoadingScreen.SetText(LoadingScreen.TextRunScript);

            DataMap.Mapping();

            LoadCompleteEvent?.Invoke();

            await ActivateAndUnloadGameSceneAsync(op);

            _preloadData = null;
            ClearCache();
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
    private static async Task LoadAssetAsync()
    {
        var mods = ModService.Mods;
        var objMap = new Dictionary<ModData, List<JsonLoadingContext>>(mods.Count);

        foreach (var dataInfo in DataInfos.Values)
        {
            var type = dataInfo.Type;
            var dataName = dataInfo.Name;

            var isSo = type.IsSubclassOf(typeof(ScriptableObject));
            var isUidObj = type.IsSubclassOf(typeof(UniqueIDScriptable));

            var dict = Database.GetData(type) ?? MakeDataDict(type);
            if (dict is null) continue;

            Database.AddData(type, dict);

            foreach (var mod in mods)
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

                var modTypedDict = mod.AllData;
                IDictionary? modObjDict = null;

                if (!objMap.TryGetValue(mod, out var list))
                {
                    list = [];
                    objMap[mod] = list;

                    modTypedDict = mod.AllData = new Dictionary<Type, IDictionary>(DataInfos.Count);
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

                        list.Add(new JsonLoadingContext(obj, filePath));
                        dict[name] = obj;

                        modObjDict ??= modTypedDict[type] = MakeDataDict(type)!;
                        modObjDict[fileName] = obj;
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.LogWarning($"{mod.Namespace} load {dataName} failed: {ex}");
                    }
                }
            }
        }

        var taskSprite = TextureLoader.LoadTexture2DAndSpriteAsync();

        await Task.Run(() =>
        {
            Parallel.ForEach(objMap.SelectMany(kvp => kvp.Value),
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
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
        }).ConfigureAwait(false);

        _uidMap = new Dictionary<string, UniqueIDScriptable>(UniqueIDScriptable.AllUniqueObjects);

        var allUidObj = GameLoad.Instance.DataBase.AllData;
        var uidObjs = new List<UniqueIDScriptable>();

        foreach (var (obj, _) in _preloadData!)
        {
            if (obj is not UniqueIDScriptable uidObj) continue;

            var uid = uidObj.UniqueID;
            if (!_uidMap.TryAdd(uid, uidObj))
            {
                Plugin.Log.LogWarning($"Preload not register same uid {uid}.");
                continue;
            }

            uidObjs.Add(uidObj);
        }

        foreach (var (mod, contexts) in objMap)
        {
            foreach (var context in contexts)
            {
                if (!context.Valid || context.Obj is not UniqueIDScriptable uidObj) continue;

                var uid = uidObj.UniqueID;
                if (!_uidMap.TryAdd(uid, uidObj))
                {
                    Plugin.Log.LogWarning(
                        $"{mod.Namespace} has same uid {uid} of type {uidObj.GetType().Name} from {context.Path}.");
                    continue;
                }

                uidObjs.Add(uidObj);
            }
        }

        allUidObj.AddRange(uidObjs);

        await taskSprite;

        await Task.Run(() =>
        {
            Parallel.ForEach(_preloadData, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                context => FixData(context.Obj, context.JsonData, null));
        });

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
                        var json = await File.ReadAllTextAsync(context.Path);
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

        foreach (var uidObj in uidObjs)
        {
            uidObj.Init();
        }

        _uidMap = UniqueIDScriptable.AllUniqueObjects;
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

    private static async Task<AsyncOperation?> PreloadGameSceneAsync()
    {
        var tcs = new TaskCompletionSource<AsyncOperation?>();
        GameLoad.Instance.StartCoroutine(Coroutine(tcs));
        return await tcs.Task;

        static IEnumerator Coroutine(TaskCompletionSource<AsyncOperation?> tcs)
        {
            var op = SceneManager.LoadSceneAsync(GameLoad.Instance.GameSceneIndex, LoadSceneMode.Additive);
            if (op is null)
            {
                Plugin.Log.LogWarning("Failed to load game scene.");
                tcs.TrySetResult(null);
                yield break;
            }

            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
            {
                yield return null;
            }

            tcs.TrySetResult(op);
        }
    }

    private static async Task ActivateAndUnloadGameSceneAsync(AsyncOperation? op)
    {
        if (op is null) return;

        var tcs = new TaskCompletionSource<bool>();
        GameLoad.Instance.StartCoroutine(Coroutine(op, tcs));
        await tcs.Task;
        return;

        static IEnumerator Coroutine(AsyncOperation op, TaskCompletionSource<bool> tcs)
        {
            var gl = GameLoad.Instance;
            var sceneIndex = gl.GameSceneIndex;

            gl.CurrentGameDataIndex = -1;
            op.allowSceneActivation = true;

            yield return op;

            yield return SceneManager.UnloadSceneAsync(sceneIndex);

            tcs.TrySetResult(true);
        }
    }

    internal static void DestroyGameSceneObjects()
    {
        var scene = SceneManager.GetSceneByBuildIndex(GameLoad.Instance.GameSceneIndex);

        foreach (var go in scene.GetRootGameObjects())
        {
            Object.DestroyImmediate(go);
        }
    }
}