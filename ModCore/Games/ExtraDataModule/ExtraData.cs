using System;
using ModCore.Games.ExtraDataModule.Parsers;

namespace ModCore.Games.ExtraDataModule;

/// <summary>
/// 额外数据
/// </summary>
/// <param name="data">数据值。</param>
public class ExtraData(string data)
{
    /// <summary>
    /// 数据，当存在代理时将从代理获取。
    /// </summary>
    public string Data
    {
        get => Proxy is null ? field : Proxy.ToString();
        // private set;
    } = data;

    /// <summary>
    /// 数据代理。
    /// </summary>
    public DataProxy? Proxy { get; private set; }

    /// <summary>
    /// 是否有效。
    /// </summary>
    public bool IsValid { get; private set; } = true;

    /// <summary>
    /// 数据类型。
    /// </summary>
    public Type? Type => Proxy?.Type;

    /// <summary>
    /// 获取或尝试创建指定类型的数据代理。
    /// </summary>
    /// <param name="parser">数据解析器，若为 <c>null</c> 则会尝试从 <see cref="DefaultParsers"/> 获取。</param>
    /// <typeparam name="T">目标数据类型。</typeparam>
    /// <returns>若已有代理且类型匹配则直接返回；<br/>若无代理则尝试创建并返回，若失败则返回 <c>null</c>。</returns>
    /// <exception cref="InvalidCastException">当已存在代理且类型不匹配时抛出。</exception>
    public DataProxy<T>? GetProxy<T>(IParser<T>? parser = null)
    {
        if (Proxy is not null) return (DataProxy<T>)Proxy;

        parser ??= DefaultParsers.GetParser<T>();
        var proxy = DataProxy.Create(this, parser);
        Proxy = proxy;
        return proxy;
    }

    /// <summary>
    /// 获取或创建指定类型的数据代理。
    /// </summary>
    /// <param name="value">当无法从现有数据解析出代理时，用于创建代理的默认值。</param>
    /// <param name="parser">数据解析器，若为 <c>null</c> 则会尝试从 <see cref="DefaultParsers"/> 获取。</param>
    /// <typeparam name="T">目标数据类型。</typeparam>
    /// <returns>若已有代理且类型匹配则直接返回；<br/>若无代理则尝试通过现有数据创建，若失败则使用 <paramref name="value"/> 创建。</returns>
    /// <exception cref="InvalidCastException">当已存在代理且类型不匹配时抛出。</exception>
    public DataProxy<T> GetProxy<T>(T value, IParser<T>? parser = null)
    {
        if (Proxy is not null) return (DataProxy<T>)Proxy;

        parser ??= DefaultParsers.GetParser<T>();
        var proxy = DataProxy.Create(this, parser) ?? new DataProxy<T>(this, value, parser);
        Proxy = proxy;
        return proxy;
    }

    /// <summary>
    /// 创建额外数据对象及代理。
    /// </summary>
    /// <param name="value">数据值。</param>
    /// <param name="parser">数据解析器，若为 <c>null</c> 则会尝试从 <see cref="DefaultParsers"/> 获取。</param>
    /// <typeparam name="T">目标数据类型。</typeparam>
    /// <returns>额外数据对象。</returns>
    internal static ExtraData Create<T>(T value, IParser<T>? parser = null)
    {
        parser ??= DefaultParsers.GetParser<T>();

        var data = new ExtraData("");
        data.Proxy = new DataProxy<T>(data, value, parser);
        return data;
    }

    /// <summary>
    /// 创建额外数据对象及代理。
    /// </summary>
    /// <param name="value">数据值。</param>
    /// <param name="proxy">返回的数据代理。</param>
    /// <param name="parser">数据解析器，若为 <c>null</c> 则会尝试从 <see cref="DefaultParsers"/> 获取。</param>
    /// <typeparam name="T">目标数据类型。</typeparam>
    /// <returns>额外数据对象。</returns>
    internal static ExtraData Create<T>(T value, out DataProxy<T> proxy, IParser<T>? parser = null)
    {
        parser ??= DefaultParsers.GetParser<T>();

        var data = new ExtraData("");
        data.Proxy = proxy = new DataProxy<T>(data, value, parser);
        return data;
    }

    /// <summary>
    /// 使失效。
    /// </summary>
    public void Invalidate()
    {
        IsValid = false;
    }

    // public DataProxy<T> TryCreateProxy<T>(T value, IParser<T>? parser = null)
    // {
    //     if (Proxy is not null) return (DataProxy<T>)Proxy;
    //
    //     parser ??= DefaultParsers.GetParser<T>();
    //     var proxy = new DataProxy<T>(this, value, parser);
    //     Proxy = proxy;
    //     return proxy;
    // }

    // /// <summary>
    // /// 设置代理。若代理不存在则创建；若代理存在且类型一致则更新数据值。
    // /// </summary>
    // /// <param name="value">数据值。</param>
    // /// <param name="parser">数据解析器，若为 <c>null</c> 则会尝试从 <see cref="DefaultParsers"/> 获取。</param>
    // /// <typeparam name="T">数据类型。</typeparam>
    // /// <returns>
    // /// 当未绑定代理时，返回创建的代理；<br/>
    // /// 若已存在代理且类型兼容，则返回该代理；<br/>
    // /// 若已存在代理的类型不一致，则返回 <c>null</c>。
    // /// </returns>
    // public DataProxy<T>? SetProxy<T>(T value, IParser<T>? parser = null)
    // {
    //     if (Proxy is null)
    //     {
    //         parser ??= DefaultParsers.GetParser<T>();
    //         var proxy = new DataProxy<T>(this, value, parser);
    //         Proxy = proxy;
    //         return proxy;
    //     }
    //     else
    //     {
    //         var proxy = Proxy as DataProxy<T>;
    //         if (proxy is not null) proxy.Value = value;
    //         return proxy;
    //     }
    // }
}