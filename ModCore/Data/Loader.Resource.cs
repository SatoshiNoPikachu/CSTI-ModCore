using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ModCore.Services;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace ModCore.Data;

public static partial class Loader
{
    /// <summary>
    /// 2D纹理路径
    /// </summary>
    public const string Texture2DPath = "Resource/Texture2D";

    /// <summary>
    /// 音频路径
    /// </summary>
    public const string AudioClipPath = "Resource/AudioClip";

    private static async Task LoadResources()
    {
        await LoadTexture2DAndSpriteAsync();
        await LoadAudioClipAsync();

        if (LoadResourceEvent is null) return;
        foreach (var del in LoadResourceEvent.GetInvocationList())
        {
            var func = (Func<Task>)del;

            try
            {
                await func();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Error on LoadResourceEvent: {e}");
            }
        }
    }

    private static async Task LoadTexture2DAndSpriteAsync()
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

            var modDict = mod.GetData<Sprite>();
            if (modDict is null)
            {
                mod.AllData[typeof(Sprite)] = modDict = [];
            }

            var files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (Path.GetExtension(file).ToLower() is not (".png" or ".jpg" or ".jpeg")) continue;

                var fileName = Path.GetFileNameWithoutExtension(file);
                if (ModData.HasNamespace(fileName))
                {
                    Plugin.Log.LogWarning(
                        $"{mod.Namespace} not load the name contains namespace separator from {file}.");
                    continue;
                }

                var name = $"{mod.Namespace}:{fileName}";
                if (sprites.ContainsKey(name) || tasks.ContainsKey(name))
                {
                    Plugin.Log.LogWarning($"{mod.Namespace} not load same name Texture2D from {name}.");
                    continue;
                }

                await semaphore.WaitAsync();
                tasks.Add(name, LoadTexture2DAndSpriteAsync(file, name, fileName, semaphore, sprites, modDict));
            }
        }

        await Task.WhenAll(tasks.Values).ConfigureAwait(false);

        sw.Stop();
        Plugin.Log.LogMessage($"Texture2D loading time: {sw.ElapsedMilliseconds}ms");
    }

    private static async Task LoadTexture2DAndSpriteAsync(string path, string name, string fileName,
        SemaphoreSlim semaphore, Dictionary<string, Sprite> sprites, Dictionary<string, Sprite> modDict)
    {
        try
        {
            var bytes = await File.ReadAllBytesAsync(path);
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
            modDict.Add(fileName, sprite);
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

    private static async Task LoadAudioClipAsync()
    {
        var audios = Database.GetData<AudioClip>();
        if (audios is null)
        {
            audios = [];
            Database.AddData(audios);
        }

        foreach (var audio in Resources.FindObjectsOfTypeAll<AudioClip>())
        {
            audios.TryAdd(audio.name, audio);
        }

        var sw = Stopwatch.StartNew();

        var semaphore = new SemaphoreSlim(10);
        var tasks = new Dictionary<string, Task>();

        foreach (var mod in ModService.Mods)
        {
            var path = Path.Combine(mod.RootPath, AudioClipPath);
            if (!Directory.Exists(path)) continue;

            var modDict = mod.GetData<AudioClip>();
            if (modDict is null)
            {
                mod.AllData[typeof(AudioClip)] = modDict = [];
            }

            var files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var type = Path.GetExtension(file).ToLower() switch
                {
                    ".mp3" => AudioType.MPEG,
                    ".ogg" => AudioType.OGGVORBIS,
                    ".wav" => AudioType.WAV,
                    _ => AudioType.UNKNOWN
                };

                if (type is AudioType.UNKNOWN) continue;

                var fileName = Path.GetFileNameWithoutExtension(file);
                if (ModData.HasNamespace(fileName))
                {
                    Plugin.Log.LogWarning(
                        $"{mod.Namespace} not load the name contains namespace separator from {file}.");
                    continue;
                }

                var name = $"{mod.Namespace}:{fileName}";
                if (audios.ContainsKey(name) || tasks.ContainsKey(name))
                {
                    Plugin.Log.LogWarning($"{mod.Namespace} not load same name AudioClip from {name}.");
                    continue;
                }

                await semaphore.WaitAsync();
                tasks.Add(name, LoadAudioClipAsync(file, name, fileName, type, semaphore, audios, modDict));
            }
        }

        await Task.WhenAll(tasks.Values).ConfigureAwait(false);

        sw.Stop();
        Plugin.Log.LogMessage($"AudioClip loading time: {sw.ElapsedMilliseconds}ms");
    }

    private static async Task LoadAudioClipAsync(string path, string name, string fileName, AudioType type,
        SemaphoreSlim semaphore, Dictionary<string, AudioClip> audios, Dictionary<string, AudioClip> modDict)
    {
        try
        {
            using var uwr = UnityWebRequestMultimedia.GetAudioClip($"file://{path}", type);

            await uwr.SendWebRequest().WaitAsync();

            if (uwr.result is not UnityWebRequest.Result.Success)
            {
                Plugin.Log.LogWarning(uwr.error);
                return;
            }

            var audio = DownloadHandlerAudioClip.GetContent(uwr);
            audio.name = name;
            audios.Add(name, audio);
            modDict.Add(fileName, audio);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Audio {name} load failed from {path}: {ex}");
        }
        finally
        {
            semaphore.Release();
        }
    }
}