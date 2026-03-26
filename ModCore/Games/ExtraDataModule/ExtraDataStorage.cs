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
    public static bool TryExtractData(string raw, out (string Key, string Value) data)
    {
        data = default;

        if (raw.Length < PrefixLength) return false;

        for (var i = 0; i < PrefixLength; i++)
        {
            if (raw[i] != Prefix[i]) return false;
        }

        var index = raw.IndexOf(':', PrefixLength);
        if (index == -1) return false;

        data.Key = raw[PrefixLength..index];
        data.Value = raw[(index + 1)..];

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
    private static string ToRaw(string key, string value) => $"{Prefix}{key}:{value}";

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
    /// <param name="key">数据键</param>
    /// <param name="value">数据值。</param>
    /// <param name="parser">数据解析器，若为 <c>null</c> 则会尝试从 <see cref="DefaultParsers"/> 获取。</param>
    /// <typeparam name="T">数据类型。</typeparam>
    /// <returns>是否成功添加数据。</returns>
    public bool TryAdd<T>(string key, T value, IParser<T>? parser = null)
    {
        if (_data.ContainsKey(key)) return false;
        
        _data[key] = ExtraData.Create(value, parser);
        return true;
    }

    /// <summary>
    /// 获取数据。
    /// </summary>
    /// <param name="key">数据键。</param>
    /// <returns>若数据键存在则返回所关联的数据，否则返回 <c>null</c>。</returns>
    public ExtraData? Get(string key)
    {
        return _data.GetValueOrDefault(key);
    }

    /// <summary>
    /// 获取数据。
    /// </summary>
    /// <param name="key">数据键。</param>
    /// <param name="def">默认值。</param>
    /// <returns>若数据键存在则返回所关联的数据，否则使用默认值创建并返回。</returns>
    public ExtraData Get(string key, string def)
    {
        return _data.TryGetValue(key, out var data) ? data : _data[key] = new ExtraData(def);
    }

    /// <summary>
    /// 设置数据，若数据已存在且已存在非字符串代理则失败。
    /// </summary>
    /// <param name="key">数据键。</param>
    /// <param name="value">数据值。</param>
    /// <returns>是否成功设置。</returns>
    public bool Set(string key, string value)
    {
        if (!_data.TryGetValue(key, out var data))
        {
            _data[key] = new ExtraData(value);
            return true;
        }

        if (data.TrySetData(value) || data.Proxy is not DataProxy<string> proxy) return false;

        proxy.Value = value;
        return true;
    }

    /// <summary>
    /// 设置数据。若数据存在则尝试更新或创建代理；若数据不存在则创建。
    /// </summary>
    /// <param name="key">数据键。</param>
    /// <param name="value">数据值。</param>
    /// <param name="parser">数据解析器，若为 <c>null</c> 则会尝试从 <see cref="DefaultParsers"/> 获取。</param>
    /// <typeparam name="T">数据类型。</typeparam>
    /// <returns>是否成功设置。</returns>
    public bool Set<T>(string key, T value, IParser<T>? parser = null)
    {
        if (_data.TryGetValue(key, out var data))
        {
            return data.SetProxy(value, parser) is not null;
        }

        _data[key] = ExtraData.Create(value, parser);
        return true;
    }

    /// <summary>
    /// 移除数据，被移除的数据将失效。
    /// </summary>
    /// <param name="key">要移除的数据键。</param>
    /// <returns>成功移除返回<c>true</c>，指定键不存在则返回<c>false</c>。</returns>
    public bool Remove(string key)
    {
        var data = Get(key);
        if (data is null) return false;
        data.Invalidate();

        return _data.Remove(key);
    }
}