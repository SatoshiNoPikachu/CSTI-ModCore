using System;
using ModCore.Games.ExtraDataModule.Parsers;

namespace ModCore.Games.ExtraDataModule;

/// <summary>
/// 额外数据
/// </summary>
/// <param name="data"></param>
public class ExtraData(string data)
{
    /// <summary>
    /// 数据，当存在代理时将从代理获取。
    /// </summary>
    public string Data
    {
        get => Proxy is null ? field : field = Proxy.ToString();
        private set;
    } = data;

    /// <summary>
    /// 数据代理
    /// </summary>
    public DataProxy? Proxy { get; private set; }

    /// <summary>
    /// 是否有效。
    /// </summary>
    public bool IsValid { get; private set; } = true;

    /// <summary>
    /// 获取代理，若无代理则会尝试创建。
    /// </summary>
    /// <param name="parser">数据解析器，若为 <c>null</c> 则会尝试从 <see cref="DefaultParsers"/> 获取。</param>
    /// <typeparam name="T">目标数据类型。</typeparam>
    /// <returns>若已有代理目标类型一致或代理创建成功则返回，否则返回 <c>null</c>。</returns>
    public DataProxy<T>? GetProxy<T>(IParser<T>? parser = null)
    {
        if (Proxy is not null) return Proxy as DataProxy<T>;

        parser ??= DefaultParsers.GetParser<T>();
        var proxy = DataProxy<T>.Create(this, parser);
        Proxy = proxy;
        return proxy;
    }

    /// <summary>
    /// 设置代理。若代理不存在则创建；若代理存在且类型一致则更新数据值。
    /// </summary>
    /// <param name="value">数据值。</param>
    /// <param name="parser">数据解析器，若为 <c>null</c> 则会尝试从 <see cref="DefaultParsers"/> 获取。</param>
    /// <typeparam name="T">数据类型。</typeparam>
    /// <returns>
    /// 当未绑定代理时，返回创建的代理；<br/>
    /// 若已存在代理且类型兼容，则返回该代理；<br/>
    /// 若已存在代理的类型不一致，则返回 <c>null</c>。
    /// </returns>
    public DataProxy<T>? SetProxy<T>(T value, IParser<T>? parser = null)
    {
        if (Proxy is null)
        {
            parser ??= DefaultParsers.GetParser<T>();
            var proxy = new DataProxy<T>(this, value, parser);
            Proxy = proxy;
            return proxy;
        }
        else
        {
            var proxy = Proxy as DataProxy<T>;
            if (proxy is not null) proxy.Value = value;
            return proxy;
        }
    }

    /// <summary>
    /// 尝试设置数据，如果已存在代理，则设置失败。
    /// </summary>
    /// <param name="data">数据</param>
    /// <returns>是否设置成功。</returns>
    public bool TrySetData(string data)
    {
        if (Proxy is not null) return false;

        Data = data;
        return true;
    }

    /// <summary>
    /// 创建额外数据对象及代理。
    /// </summary>
    /// <param name="value">数据值。</param>
    /// <param name="parser">数据解析器。</param>
    /// <typeparam name="T">目标数据类型。</typeparam>
    /// <returns>额外数据对象。</returns>
    /// <exception cref="InvalidOperationException">数据值转换成字符串失败。</exception>
    public static ExtraData Create<T>(T value, IParser<T> parser)
    {
        var str = parser.ToString(value) ?? throw new InvalidOperationException();
        var data = new ExtraData(str);
        data.Proxy = new DataProxy<T>(data, value, parser);
        return data;
    }

    /// <summary>
    /// 使失效。
    /// </summary>
    public void Invalidate()
    {
        IsValid = false;
    }
}