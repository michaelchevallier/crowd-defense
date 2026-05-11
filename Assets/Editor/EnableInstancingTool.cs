#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CrowdDefense.Editor
{
    public static class EnableInstancingTool
    {
        [MenuItem("Tools/CrowdDefense/Enable Instancing")]
        public static void EnableAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materials" });
            int count = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null && !mat.enableInstancing)
                {
                    mat.enableInstancing = true;
                    EditorUtility.SetDirty(mat);
                    count++;
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[EnableInstancingTool] GPU instancing enabled on {count} material(s) in Assets/Materials.");
        }
    }
}
#endif
