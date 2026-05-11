#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.SceneManagement;

namespace CrowdDefense.Editor
{
    [InitializeOnLoad]
    public static class POC07Setup
    {
        private const string SessionKey = "POC07Setup_Done";

        static POC07Setup()
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            EditorApplication.delayCall += AutoSetupHUD;
        }

        private static void AutoSetupHUD()
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            if (GameObject.Find("HUD") != null)
            {
                SessionState.SetBool(SessionKey, true);
                return;
            }
            SetupHUD();
            SessionState.SetBool(SessionKey, true);
        }

        [MenuItem("CrowdDefense/POC-07: Setup HUD in Scene")]
        public static void SetupHUD()
        {
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/HUDPanelSettings.asset");
            if (panelSettings == null)
            {
                Debug.LogError("[POC07Setup] HUDPanelSettings.asset not found at Assets/UI/HUDPanelSettings.asset");
                return;
            }

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/HUD.uxml");
            if (visualTree == null)
            {
                Debug.LogError("[POC07Setup] HUD.uxml not found at Assets/UI/HUD.uxml");
                return;
            }

            var hudGO = GameObject.Find("HUD");
            if (hudGO == null)
            {
                hudGO = new GameObject("HUD");
                Undo.RegisterCreatedObjectUndo(hudGO, "Create HUD GO");
            }

            var doc = hudGO.GetComponent<UIDocument>();
            if (doc == null)
                doc = Undo.AddComponent<UIDocument>(hudGO);

            var so = new SerializedObject(doc);
            so.FindProperty("m_PanelSettings").objectReferenceValue = panelSettings;
            so.FindProperty("sourceAsset").objectReferenceValue = visualTree;
            so.ApplyModifiedProperties();

            if (hudGO.GetComponent<CrowdDefense.UI.HudController>() == null)
                Undo.AddComponent<CrowdDefense.UI.HudController>(hudGO);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("[POC07Setup] HUD GameObject created with UIDocument + HudController, refs assigned");
        }
    }
}
#endif
