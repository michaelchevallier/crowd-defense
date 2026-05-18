#nullable enable
using UnityEngine;
using UnityEngine.SceneManagement;
using CrowdDefense.UI;

namespace CrowdDefense.Systems
{
    public sealed class LoaderToMenu : MonoBehaviour
    {
        private void Start()
        {
            BootstrapCoreSystems();
            SceneManager.LoadSceneAsync("Menu");
        }

        // Pre-instantiate "core" singletons in Loader scene with DontDestroyOnLoad,
        // so they survive Menu/WorldMap/Main transitions and don't auto-create with
        // [MonoSingleton] warnings every time a UI controller upstream reads them.
        // Scene-scoped singletons (UI controllers, Castle, LevelRunner, etc.) stay
        // in Main.unity — they depend on UIDocument or scene-placed GameObjects.
        private static void BootstrapCoreSystems()
        {
            if (GameObject.Find("[Bootstrap] CoreSystems") != null) return;

            var coreGo = new GameObject("[Bootstrap] CoreSystems");
            DontDestroyOnLoad(coreGo);

            // Order matters: SettingsRegistry.OnAwakeSingleton() calls AudioController.Instance
            // and PostProcessController.Instance. Add AudioController first so its
            // Awake runs before SettingsRegistry tries to read it.
            coreGo.AddComponent<AudioController>();
            coreGo.AddComponent<MusicManager>();
            coreGo.AddComponent<KeyBindings>();
            coreGo.AddComponent<EventManager>();
            coreGo.AddComponent<PerkSystem>();
            coreGo.AddComponent<MetaUpgradeSystem>();
            coreGo.AddComponent<LifetimeStats>();
            coreGo.AddComponent<Achievements>();
            // SettingsRegistry last — it reads other singletons in OnAwakeSingleton.
            coreGo.AddComponent<SettingsRegistry>();
        }
    }
}
