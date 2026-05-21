using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LitJson;
using ModCore.Services;
using Object = UnityEngine.Object;

namespace ModCore.Data;

public static partial class Loader
{
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

    private static async Task StartModifyAsync()
    {
        Plugin.Log.LogMessage("Start modify");
        var sw = Stopwatch.StartNew();

        foreach (var mod in ModService.Mods)
        {
            foreach (var (file, obj) in GetModifyTargets(Path.Combine(mod.RootPath, GameSourceModifyPath), true, mod)
                         .Concat(GetModifyTargets(Path.Combine(mod.RootPath, DataObjectModifyPath), false, mod)))
            {
                try
                {
                    var jsonData = JsonMapper.ToObject(await File.ReadAllTextAsync(file).ConfigureAwait(false));
                    await ModifyAsync(obj, jsonData, mod).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"Modify failed from {file}: {ex}");
                }
            }
        }

        Plugin.Log.LogMessage($"Modify time: {sw.ElapsedMilliseconds}ms");
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
    /// 异步修改。
    /// </summary>
    /// <param name="obj">需修改的对象。</param>
    /// <param name="jsonData">JSON数据。</param>
    /// <param name="mod">模组。</param>
    private static async Task ModifyAsync(object obj, JsonData jsonData, ModData? mod)
    {
        try
        {
            if (!await ModifyMatchAsync(obj, jsonData, mod)) ModifyObject(obj, jsonData, mod);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning(ex);
        }
    }

    /// <summary>
    /// 异步匹配修改。
    /// </summary>
    /// <param name="source">源对象。</param>
    /// <param name="jsonData">JSON数据。</param>
    /// <param name="mod">模组。</param>
    /// <returns>是否至少匹配到一个目标。</returns>
    private static async Task<bool> ModifyMatchAsync(object source, JsonData jsonData, ModData? mod)
    {
        var set = new HashSet<object>();
        
        MatchTargets(set, jsonData, source, mod);

        if (source is CardData)
        {
            MatchCardTag(set, jsonData);
            MatchCardType(set, jsonData);
        }

        if (set.Count is 0) return false;

        var sem = new SemaphoreSlim(Environment.ProcessorCount);
        var tasks = new List<Task>(set.Count);

        foreach (var obj in set)
        {
            await sem.WaitAsync().ConfigureAwait(false);

            tasks.Add(Task.Run(() =>
            {
                try
                {
                    ModifyObject(obj, jsonData, mod);
                }
                finally
                {
                    sem.Release();
                }
            }));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);

        return true;
    }

    private static void MatchTargets(HashSet<object> set, JsonData jsonData, object source, ModData? mod)
    {
        if (!jsonData.ContainsKey("$matchTargets")) return;
        
        var targets = jsonData["$matchTargets"];
        if (!targets.IsArray) return;

        var type = source.GetType();
        var count = targets.Count;
        for (var i = 0; i < count; i++)
        {
            var data = targets[i];
            if (!data.IsString) continue;

            var target = Database.GetData(type, data.ToString(), mod);
            if (target is null) continue;
            
            set.Add(target);
        }
    }

    private static void MatchCardTag(HashSet<object> set, JsonData jsonData)
    {
        if (!jsonData.ContainsKey("MatchTagWarpData")) return;

        var warpData = jsonData["MatchTagWarpData"];
        jsonData.Remove("MatchTagWarpData");

        if (!warpData.IsArray) return;

        var count = warpData.Count;
        for (var i = 0; i < count; i++)
        {
            var data = warpData[i];
            if (!data.IsString) continue;

            var tag = Database.GetData<CardTag>(data.ToString());
            if (tag is null) continue;

            foreach (var card in tag.GetCards()) set.Add(card);
        }
    }

    private static void MatchCardType(HashSet<object> set, JsonData jsonData)
    {
        if (!jsonData.ContainsKey("MatchTypeWarpData")) return;

        var warpData = jsonData["MatchTypeWarpData"];
        jsonData.Remove("MatchTypeWarpData");

        if (!warpData.IsString) return;

        if (!Enum.TryParse(warpData.ToString(), out CardTypes type))
        {
            Plugin.Log.LogWarning($"Unmatched CardTypes {warpData}");
            return;
        }

        foreach (var card in type.GetCards()) set.Add(card);
    }

    /// <summary>
    /// 修改对象。
    /// </summary>
    /// <param name="obj">对象。</param>
    /// <param name="jsonData">JSON数据。</param>
    /// <param name="mod">模组。</param>
    public static void ModifyObject(object? obj, JsonData jsonData, ModData? mod)
    {
        if (obj is null) return;
        if (!jsonData.IsObject) return;

        var type = obj.GetType();

        if (jsonData.ContainsKey("$override"))
        {
            var data = jsonData["$override"];
            if (data.IsObject) FromJsonOverwrite(data.ToJson(), data, obj, mod, true);
        }

        foreach (var key in jsonData.Keys)
        {
            if (!key.EndsWith("WarpData")) continue;

            var jsonField = jsonData[key];
            if (!jsonField.IsObject && !jsonField.IsArray) continue;
            var fieldName = key[..^8];

            try
            {
                var warpType = (WarpType)(int)jsonData[$"{fieldName}WarpType"];

                var field = GetField(type, fieldName);
                if (field is null) continue;
                if (field.IsNotSerialized) continue;

                if (jsonField.IsObject)
                {
                    if (warpType is not WarpType.Modify) continue;

                    ModifyObject(field.GetValue(obj), jsonField, mod);
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
                Array.Copy(arr, arrNew, arr.Length);

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
                Array.Copy(arr, arrNew, arr.Length);

                for (var i = 0; i < jsonData.Count; i++)
                {
                    arrNew.SetValue(FromJson(elementType, jsonData[i], mod), i + arr.Length);
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
                    list.Add(FromJson(elementType, jsonData[i], mod));
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

            var unityObj = GetWarpObject(elementType, data.ToString(), mod);
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