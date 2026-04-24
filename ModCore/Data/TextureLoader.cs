using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ModCore.Services;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ModCore.Data;

/// <summary>
/// 纹理加载器
/// </summary>
public static class TextureLoader
{
    /// <summary>
    /// 2D纹理路径
    /// </summary>
    public const string Texture2DPath = "Resource/Texture2D";

    /// <summary>
    /// 异步加载2D纹理和精灵
    /// </summary>
    internal static async Task LoadTexture2DAndSpriteAsync()
    {
        var sprites = Database.GetData<Sprite>();
        if (sprites is null)
        {
            sprites = [];
            Database.AddData(sprites);
        }

        foreach (var sprite in Resources.FindObjectsOfTypeAll<Sprite>())
        {
            sprites.TryAdd(sprite.name, sprite);
        }

        await Task.Yield();

        var sw = Stopwatch.StartNew();

        var semaphore = new SemaphoreSlim(10);
        var tasks = new Dictionary<string, Task>();
        
        foreach (var mod in ModService.Mods)
        {
            var path = Path.Combine(mod.RootPath, Texture2DPath);
            if (!Directory.Exists(path)) continue;

            var files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (Path.GetExtension(file).ToLower() is not (".png" or ".jpg" or ".jpeg")) continue;

                var name = $"{mod.Namespace}:{Path.GetFileNameWithoutExtension(file)}";
                if (sprites.ContainsKey(name) || tasks.ContainsKey(name))
                {
                    Plugin.Log.LogWarning($"{mod.Namespace} not load same name Texture2D from {name}.");
                    continue;
                }

                await semaphore.WaitAsync();
                tasks.Add(name, LoadTexture2DAndSpriteAsync(file, name, sprites, semaphore));
            }
        }

        await Task.WhenAll(tasks.Values);

        sw.Stop();
        Plugin.Log.LogMessage($"Texture2D loading time: {sw.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// 异步加载2D纹理和精灵
    /// </summary>
    /// <param name="path">纹理文件路径</param>
    /// <param name="name">纹理名称</param>
    /// <param name="sprites">精灵字典</param>
    /// <param name="semaphore">信号量</param>
    private static async Task LoadTexture2DAndSpriteAsync(string path, string name, Dictionary<string, Sprite> sprites,
        SemaphoreSlim semaphore)
    {
        // await semaphore.WaitAsync();

        try
        {
            var bytes = await File.ReadAllBytesAsync(path);
            // var bytes = await ReadFileAsync(path);
            var tex = new Texture2D(0, 0, TextureFormat.RGBA32, false)
            {
                name = name
            };
            if (!tex.LoadImage(bytes, true))
            {
                Object.Destroy(tex);
                Plugin.Log.LogWarning($"Texture2D {name} load failed from {path}");
                return;
            }

            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero, 100, 0,
                SpriteMeshType.FullRect);
            sprite.name = name;
            sprites.Add(name, sprite);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Texture2D {name} load failed from {path}: {ex}");
        }
        finally
        {
            semaphore.Release();
        }
    }

    // /// <summary>
    // /// 异步读取文件
    // /// </summary>
    // /// <param name="path">文件路径</param>
    // /// <returns>读取字节的任务</returns>
    // private static async Task<byte[]> ReadFileAsync(string path)
    // {
    //     await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, true);
    //     var buffer = new byte[file.Length];
    //     var count = 0;
    //     while (count < buffer.Length)
    //     {
    //         var read = await file.ReadAsync(buffer, count, buffer.Length - count);
    //         if (read == 0) break;
    //         count += read;
    //     }
    //
    //     return buffer;
    // }
}