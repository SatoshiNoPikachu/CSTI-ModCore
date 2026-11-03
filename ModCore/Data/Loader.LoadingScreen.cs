using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ModCore.Data;

public static partial class Loader
{
    /// <summary>
    /// 加载屏幕
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        internal static Task<bool>? Ready => _instance?._ready.Task;

        private static LoadingScreen? _instance;

        private bool _error;

        private readonly TaskCompletionSource<bool> _ready = new();

        /// <summary>
        /// 当异步加载场景
        /// </summary>
        /// <returns>是否允许加载</returns>
        internal static bool OnAsyncLoadingLoadScene()
        {
            return _instance is null;
        }

        internal static void OnError()
        {
            if (_instance is null) return;
            _instance._error = true;
            _instance._ready.SetResult(false);
            SetText(TextGameLoadError);
        }

        internal static bool OnReady()
        {
            if (_instance is null) return true;

            _instance._ready.SetResult(true);
            return false;
        }

        /// <summary>
        /// 加载开始
        /// </summary>
        internal static void Loading()
        {
            if (_instance) return;

            var loading = AsyncLoading.Instance;

            SceneManager.LoadScene(loading.LoadingSceneIndex);
            _instance = loading.gameObject.AddComponent<LoadingScreen>();

            SetText(TextWait);

            loading.LoadingVisuals.SetActive(true);
            loading.TipsVersion.SetActive(true);
            loading.InitLoadingVisuals.SetActive(false);
        }

        /// <summary>
        /// 加载完成
        /// </summary>
        internal static void Loaded()
        {
            if (_instance?._error is null or true) return;

            _instance.StartCoroutine(_instance.LoadMenuScene());
        }

        /// <summary>
        /// 设置文本
        /// </summary>
        /// <param name="text">文本</param>
        public static void SetText(string text)
        {
            if (_instance is null) return;
            AsyncLoading.Instance.TipsText.text = text;
        }

        /// <summary>
        /// 加载菜单场景
        /// </summary>
        /// <returns></returns>
        private IEnumerator LoadMenuScene()
        {
            SetText(TextLoadCompleted);

            var operation = SceneManager.LoadSceneAsync(GameLoad.Instance.MenuSceneIndex)!;

            while (!operation.isDone)
            {
                yield return null;
            }

            _instance = null;
            Destroy(this);

            AsyncLoading.SetInitLoadingScreen(InitLoadingState.ClickToContinue);
            AsyncLoading.Instance.LoadingVisuals.SetActive(false);
        }

        private const string KeyPrefix = "ModCore_LoadingScreen_";

        private static LocalizedString TextWait => new()
        {
            DefaultText = "正在等待游戏加载...",
            LocalizationKey = $"{KeyPrefix}Wait"
        };

        public static LocalizedString TextInit => new()
        {
            DefaultText = "正在初始化...",
            LocalizationKey = $"{KeyPrefix}Init"
        };

        public static LocalizedString TextLoadAsset => new()
        {
            DefaultText = "正在加载数据和资源...",
            LocalizationKey = $"{KeyPrefix}LoadAsset"
        };

        public static LocalizedString TextApplyModify => new()
        {
            DefaultText = "正在应用修改...",
            LocalizationKey = $"{KeyPrefix}ApplyModify"
        };

        public static LocalizedString TextRunScript => new()
        {
            DefaultText = "正在执行脚本...",
            LocalizationKey = $"{KeyPrefix}RunScript"
        };

        private static LocalizedString TextLoadCompleted => new()
        {
            DefaultText = "加载完成，正在进入主菜单...",
            LocalizationKey = $"{KeyPrefix}LoadCompleted"
        };

        private static LocalizedString TextGameLoadError => new()
        {
            DefaultText = "<color=red>无法进入游戏\n游戏加载时出错\n（可能是某个插件导致的）",
            LocalizationKey = $"{KeyPrefix}GameLoadError"
        };
    }
}