#nullable enable
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrowdDefense.Systems
{
    public sealed class LoaderToMenu : MonoBehaviour
    {
        private void Start() => SceneManager.LoadSceneAsync("Menu");
    }
}
