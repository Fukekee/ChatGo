using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChatGo.Core
{
    public static class LevelManager
    {
        public static string CurrentLevelScene { get; private set; }
        public static string CurrentLevelId { get; private set; }

        public const string MainMenuScene = "Main";
        public const string GameBaseScene = "GameBase";

        public static event Action SceneLoadStarted;
        public static event Action SceneLoadCompleted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            CurrentLevelScene = null;
            CurrentLevelId = null;
            SceneLoadStarted = null;
            SceneLoadCompleted = null;
        }

        public static void LoadLevel(string sceneName, string levelId = null)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("LevelManager: sceneName 为空。");
                return;
            }

            SetOrientation(ScreenOrientation.LandscapeLeft);

            CurrentLevelScene = sceneName;
            CurrentLevelId = levelId;
            SceneLoadStarted?.Invoke();
            SceneManager.LoadScene(GameBaseScene, LoadSceneMode.Single);
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }

        public static void ReturnToMainMenu()
        {
            SetOrientation(ScreenOrientation.Portrait);

            CurrentLevelScene = null;
            CurrentLevelId = null;
            SceneLoadStarted?.Invoke();
            SceneManager.LoadScene(MainMenuScene, LoadSceneMode.Single);
        }

        public static void ReloadCurrentLevel()
        {
            if (string.IsNullOrEmpty(CurrentLevelScene))
            {
                Debug.LogWarning("LevelManager: 没有当前关卡，返回主菜单。");
                ReturnToMainMenu();
                return;
            }

            LoadLevel(CurrentLevelScene, CurrentLevelId);
        }

        private static void SetOrientation(ScreenOrientation orientation)
        {
#if UNITY_EDITOR
            Debug.Log($"LevelManager: 屏幕方向 -> {orientation}");
#else
            Screen.orientation = orientation;
#endif
        }
    }
}
