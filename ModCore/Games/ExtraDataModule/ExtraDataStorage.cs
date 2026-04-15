using System;
using ModCore.Games.ExtraDataModule.Parsers;

namespace ModCore.Games.ExtraDataModule;

/// <summary>
/// 额外数据存储器。
/// </summary>
public class ExtraDataStorage
{
    /// <summary>
    /// 数据前缀。
    /// </summary>
    private const string Prefix = "MCED|";

    /// <summary>
    /// 前缀长度。
    /// </summary>
    private const int PrefixLength = 5;

    /// <summary>
    /// 存储器表。
    /// </summary>
    private static readonly Dictionary<object, ExtraDataStorage> Storages = [];

    /// <summary>
    /// 数据表。
    /// </summary>
    private readonly Dictionary<string, ExtraData> _data = [];

    /// <summary>
    /// 数据量。
    /// </summary>
    public int Count => _data.Count;

    internal static void ClearStorage()
    {
        Storages.Clear();
    }

    /// <summary>
    /// 获取存储器。
    /// </summary>
    /// <param name="obj">存储器所绑定的组件。</param>
    /// <returns>存储器，若不存在或已被销毁则返回 <c>null</c>。</returns>
    public static ExtraDataStorage? GetStorage(object obj)
    {
        return Storages.GetValueOrDefault(obj);
    }

    /// <summary>
    /// 创建或创建存储器。
    /// </summary>
    /// <param name="obj">存储器所绑定的组件。</param>
    /// <returns>若已存在有效存储器则返回，否则返创建新存储器并返回。</returns>
    public static ExtraDataStorage GetOrCreateStorage(object obj)
    {
        return GetStorage(obj) ?? (Storages[obj] = new ExtraDataStorage());
    }

    /// <summary>
    /// 尝试提取数据。
    /// </summary>
    /// <param name="raw">原始数据。</param>
    /// <param name="data">提取出的数据。</param>
    /// <returns></returns>
    public static bool TryExtractData(ReadOnlySpan<char> raw, out (string Key, string Value) data)
    {
        data = default;

        if (!raw.StartsWith(Prefix)) return false;

        raw = raw[PrefixLength..];

        var lenSepIndex = raw.IndexOf('|');
        if (lenSepIndex <= 0) return false;

        if (!int.TryParse(raw[..lenSepIndex], out var keyLength) || keyLength < 0) return false;

        raw = raw[(lenSepIndex + 1)..];
        if (raw.Length < keyLength + 1) return false;

        if (raw[keyLength] != '|') return false;

        data.Key = raw[..keyLength].ToString();
        data.Value = raw[(keyLength + 1)..].ToString();

        return true;
    }

    /// <summary>
    /// 重置。
    /// </summary>
    public void Reset()
    {
        foreach (var data in _data.Values)
        {
            data.Invalidate();
        }

        _data.Clear();
    }

    /// <summary>
    /// 移除数据，被移除的数据将失效。
    /// </summary>
    /// <param name="key">要移除的数据键。</param>
    /// <returns>成功移除返回 <c>true</c>，指定键不存在则返回 <c>false</c>。</returns>
    public bool Remove(string key)
    {
        var data = Get(key);
        if (data is null) return false;
        data.Invalidate();

        return _data.Remove(key);
    }

    /// <summary>
    /// 获取每项数据的原始字符串形式。
    /// </summary>
    /// <returns>原始字符串的可枚举对象。</returns>
    public IEnumerable<string> GetRaws()
    {
        foreach (var (key, data) in _data)
        {
            if (data.IsValid && data.Data is { } str)
            {
                yield return ToRaw(key, str);
            }
        }
    }

    /// <summary>
    /// 将数据转换成原始数据形式。
    /// </summary>
    /// <param name="key">数据键。</param>
    /// <param name="value">数据值。</param>
    /// <returns>原始数据形式。</returns>
    private static string ToRaw(string key, string value) => $"{Prefix}{key.Length}|{key}|{value}";

    /// <summary>
    /// 尝试添加数据，如果数据键已存在则不添加。
    /// </summary>
    /// <param name="key">数据键。</param>
    /// <param name="value">数据值。</param>
    /// <returns>是否成功添加数据。</returns>
    public bool TryAdd(string key, string value)
    {
        if (_data.ContainsKey(key)) return false;

        _data[key] = new ExtraData(value);
        return true;
    }

    /// <summary>
    /// 尝试添加数据，如果数据键已存在则不添加。
    /// </summary>
    /// <param name="key">数据键。</param>
    /// <param name="value">数据值。</param>
    /// <param name="parser">数据解析器，若为 <c>null</c> 则会尝试从 <see cref="DefaultParsers"/> 获取。</param>
    /// <typeparam name="T">目标数据类型。</typeparam>
    /// <returns>是否成功添加数据。</returns>
    public bool TryAdd<T>(string key, T value, IParser<T>? parser = null)
    {
        if (_data.ContainsKey(key)) return false;

        _data[key] = ExtraData.Create(value, parser);
        return true;
    }

    /// <summary>
    /// 尝试获取代理。
    /// </summary>
    /// <param name="key">数据键。</param>
    /// <param name="proxy">返回的数据代理。</param>
    /// <param name="parser">数据解析器，若为 <c>null</c> 则会尝试从 <see cref="DefaultParsers"/> 获取。</param>
    /// <typeparam name="T">目标数据类型。</typeparam>
    /// <returns>如果键存在且成功获取或创建代理，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    /// <exception cref="InvalidCastException">当已存在代理且类型不匹配时抛出。</exception>
    public bool TryGetProxy<T>(string key, out DataProxy<T>? proxy, IParser<T>? parser = null)
    {
        if (_data.TryGetValue(key, out var data))
        {
            proxy = data.GetProxy(parser);
            return proxy is not null;
        }

        proxy = null;
        return false;
    }

    /// <summary>
    /// 尝试获取值。
    /// </summary>
    /// <typeparam name="T">目标数据类型。</typeparam>
    /// <param name="key">数据键。</param>
    /// <param name="value">返回的数据值；如果键不存在或无法解析为目标类型，则为 <c>default</c>。</param>
    /// <param name="parser">数据解析器，若为 <c>null</c> 则会尝试从 <see cref="DefaultParsers"/> 获取。</param>
    /// <returns>如果键存在且成功获取或创建代理，则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    /// <exception cref="InvalidCastException">当已存在代理且类型不匹配时抛出。</exception>
    public bool TryGetValue<T>(string key, out T? value, IParser<T>? parser = null)
    {
        value = default;

        if (!_data.TryGetValue(key, out var data)) return false;

        var proxy = data.GetProxy(parser);
        if (proxy is null) return false;

        value = proxy.Value;
        return true;
    }

    /// <summary>
    /// 获取数据。
    /// </summary>
    /// <param name="key">数据键。</param>
    /// <returns>若数据键存在则返回所关联的数据，否则返回 <c>null</c>。</returns>
    public ExtraData? Get(string key) => _data.GetValueOrDefault(key);

    /// <summary>
    /// 获取或添加数据。
    /// </summary>
    /// <param name="key">数据键。</param>
    /// <param name="def">默认数据值。</param>
    /// <returns>若数据键存在则返回所关联的数据，否则使用默认值添加并返回。</returns>
    public ExtraData GetOrAdd(string key, string def)
    {
        return _data.TryGetValue(key, out var data) ? data : _data[key] = new ExtraData(def);
    }

    /// <summary>
    /// 获取或添加代理。
    /// </summary>
    /// <param name="key">数据键。</param>
    /// <param name="def">默认数据值。</param>
    /// <param name="parser">数据解析器，若为 <c>null</c> 则会尝试从 <see cref="DefaultParsers"/> 获取。</param>
    /// <typeparam name="T">目标数据类型。</typeparam>
    /// <returns>若数据键存在则通过 <see cref="ExtraData.GetProxy{T}(T,IParser{T})"/> 获取代理，否则使用默认值添加数据并返回创建的代理。</returns>
    /// <exception cref="InvalidCastException">当已存在代理且类型不匹配时抛出。</exception>
    public DataProxy<T> GetOrAdd<T>(string key, T def, IParser<T>? parser = null)
    {
        if (_data.TryGetValue(key, out var data)) return data.GetProxy(def, parser);

        _data[key] = ExtraData.Create(def, out var proxy, parser);
        return proxy;
    }

    // /// <summary>
    // /// 设置数据。若数据存在则尝试更新或创建代理；若数据不存在则创建。
    // /// </summary>
    // /// <param name="key">数据键。</param>
    // /// <param name="value">数据值。</param>
    // /// <param name="parser">数据解析器，若为 <c>null</c> 则会尝试从 <see cref="DefaultParsers"/> 获取。</param>
    // /// <typeparam name="T">数据类型。</typeparam>
    // /// <returns>是否成功设置。</returns>
    // public bool Set<T>(string key, T value, IParser<T>? parser = null)
    // {
    //     if (_data.TryGetValue(key, out var data))
    //     {
    //         return data.SetProxy(value, parser) is not null;
    //     }
    //
    //     _data[key] = ExtraData.Create(value, parser);
    //     return true;
    // }
}