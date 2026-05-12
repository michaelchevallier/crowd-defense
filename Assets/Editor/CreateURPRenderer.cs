#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;

namespace CrowdDefense.Editor
{
    // Creates a UniversalRendererData asset and assigns it to URP_PipelineAsset.m_RendererDataList[0].
    // Fix for "Default Renderer is missing" build error.
    public static class CreateURPRenderer
    {
        private const string RendererPath = "Assets/Settings/URP_Renderer.asset";
        private const string PipelinePath = "Assets/Settings/URP_PipelineAsset.asset";

        [MenuItem("CrowdDefense/Build/Create URP Renderer (Fix Default Renderer)")]
        public static void Run()
        {
            // 1) Create UniversalRendererData via Unity's factory
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, RendererPath);
                Debug.Log($"[CreateURPRenderer] Created {RendererPath}");
            }
            else
            {
                Debug.Log($"[CreateURPRenderer] Reusing existing {RendererPath}");
            }

            // 2) Wire renderer into pipeline asset
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                Debug.LogError($"[CreateURPRenderer] Pipeline not found: {PipelinePath}");
                return;
            }

            var so = new SerializedObject(pipeline);
            var list = so.FindProperty("m_RendererDataList");
            if (list == null)
            {
                Debug.LogError("[CreateURPRenderer] m_RendererDataList prop not found on pipeline");
                return;
            }

            list.arraySize = 1;
            var elem = list.GetArrayElementAtIndex(0);
            elem.objectReferenceValue = rendererData;

            var idx = so.FindProperty("m_DefaultRendererIndex");
            if (idx != null) idx.intValue = 0;

            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[CreateURPRenderer] Pipeline.m_RendererDataList[0] = URP_Renderer.asset OK");
        }
    }
}
#endif
