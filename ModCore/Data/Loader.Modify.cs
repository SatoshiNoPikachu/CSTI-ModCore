using System;
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
                        var jsonData = JsonMapper.ToObject(await File.ReadAllTextAsync(file));
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
    /// 游戏资源修改
    /// </summary>
    /// <param name="obj">需修改的对象</param>
    /// <param name="jsonData">Json数据</param>
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