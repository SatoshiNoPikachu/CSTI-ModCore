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
        /// <summary>
        /// 实例
        /// </summary>
        private static LoadingScreen? _instance;

        /// <summary>
        /// 游戏是否已尝试加载场景（GameLoad.Awake()是否已执行完毕）
        /// </summary>
        private bool _tryLoadScene;

        /// <summary>
        /// 出错
        /// </summary>
        private bool _error;

        /// <summary>
        /// 当异步加载场景
        /// </summary>
        /// <returns>是否允许加载</returns>
        public static bool OnAsyncLoadingLoadScene()
        {
            if (_instance is null) return true;

            AsyncLoading.IsLoading = true;
            _instance._tryLoadScene = true;

            return false;
        }

        /// <summary>
        /// 检查游戏加载
        /// </summary>
        /// <returns>是否开始加载</returns>
        public static bool CheckGameLoad()
        {
            if (_instance is null) return false;
            if (_instance._error) return false;
            if (_instance._tryLoadScene) return true;

            SetText(TextGameLoadError);
            return false;
        }

        public static void OnError()
        {
            _instance?._error = true;
            SetText(TextGameLoadError);
        }

        /// <summary>
        /// 加载开始
        /// </summary>
        public static void Loading()
        {
            if (_instance) return;

            var loading = AsyncLoading.Instance;

            SceneManager.LoadScene(loading.LoadingSceneIndex);
            _instance = loading.gameObject.AddComponent<LoadingScreen>();

            loading.LoadingVisuals.SetActive(true);
            loading.TipsVersion.SetActive(true);
            loading.InitLoadingVisuals.SetActive(false);
        }

        /// <summary>
        /// 加载完成
        /// </summary>
        public static void Loaded()
        {
            if (_instance?._error is null or true) return;

            _instance.StartCoroutine(_instance.LoadMenuScene());
        }

        /// <summary>
        /// 设置文本
        /// </summary>
        /// <param name="text"></param>
        public static void SetText(string text)
        {
            if (!_instance) return;
            AsyncLoading.Instance.TipsText.text = text;
        }

        /// <summary>
        /// 加载菜单场景
        /// </summary>
        /// <returns></returns>
        private IEnumerator LoadMenuScene()
        {
            while (!_tryLoadScene)
            {
                yield return null;
            }

            SetText(TextLoadCompleted);

            var operation = SceneManager.LoadSceneAsync(GameLoad.Instance.MenuSceneIndex)!;

            while (!operation.isDone)
            {
                yield return null;
            }

            AsyncLoading.IsLoading = false;
            yield return null;

            AsyncLoading.Instance.LoadingVisuals.SetActive(false);

            _instance = null;
            Destroy(this);
        }

        private const string KeyPrefix = "ModCore_LoadingScreen_";

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