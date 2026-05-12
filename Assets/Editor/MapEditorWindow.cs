#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CrowdDefense.Data;

namespace CrowdDefense.Editor
{
    public class MapEditorWindow : EditorWindow
    {
        private const string PrefsKeyPrefix = "CrowdDefense.MapEditor.Save.";
        private const string PrefsIndexKey  = "CrowdDefense.MapEditor.Index";

        private LevelData? _levelData;
        private Vector2    _scroll;
        private string     _saveName = "";
        private List<string> _savedNames = new();

        [MenuItem("CrowdDefense/Map Editor Save-Load")]
        public static void Open() =>
            GetWindow<MapEditorWindow>("Map Editor Save-Load").Show();

        private void OnEnable()  => RefreshIndex();
        private void OnDisable() => _levelData = null;

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawSourcePicker();
            EditorGUILayout.Space(6);
            DrawSaveSection();
            EditorGUILayout.Space(6);
            DrawLoadSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawSourcePicker()
        {
            EditorGUILayout.LabelField("Source LevelData", EditorStyles.boldLabel);
            _levelData = (LevelData?)EditorGUILayout.ObjectField(
                "LevelData asset", _levelData, typeof(LevelData), false);
        }

        private void DrawSaveSection()
        {
            EditorGUILayout.LabelField("Sauvegarder carte", EditorStyles.boldLabel);
            _saveName = EditorGUILayout.TextField("Nom de la sauvegarde", _saveName);

            bool canSave = _levelData != null && !string.IsNullOrWhiteSpace(_saveName);
            using (new EditorGUI.DisabledScope(!canSave))
            {
                if (GUILayout.Button("Sauvegarder"))
                    SaveMap(_levelData!, _saveName.Trim());
            }
        }

        private void DrawLoadSection()
        {
            EditorGUILayout.LabelField("Charger carte sauvegardée", EditorStyles.boldLabel);

            if (_savedNames.Count == 0)
            {
                EditorGUILayout.HelpBox("Aucune sauvegarde trouvée.", MessageType.Info);
                return;
            }

            foreach (string name in _savedNames)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(name, GUILayout.ExpandWidth(true));

                if (GUILayout.Button("Charger", GUILayout.Width(80)))
                    LoadMap(name);

                if (GUILayout.Button("Supprimer", GUILayout.Width(80)))
                {
                    DeleteSave(name);
                    break; // list mutated — exit loop safely
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        // --- persistence helpers ---

        [Serializable]
        private class MapSaveData
        {
            public string   id          = "";
            public string   displayName = "";
            public string[] mapRows     = Array.Empty<string>();
            public string[] gridVariants = Array.Empty<string>();
            public float    cellSize    = 1f;
        }

        private void SaveMap(LevelData data, string name)
        {
            var save = new MapSaveData
            {
                id           = data.Id,
                displayName  = data.DisplayName,
                mapRows      = CopyRows(data.MapRows),
                gridVariants = data.GridVariants ?? Array.Empty<string>(),
                cellSize     = data.CellSize,
            };

            string json = JsonUtility.ToJson(save, prettyPrint: true);
            PlayerPrefs.SetString(PrefsKeyPrefix + name, json);

            if (!_savedNames.Contains(name))
                _savedNames.Add(name);
            FlushIndex();

            PlayerPrefs.Save();
            Debug.Log($"[MapEditor] Carte '{name}' sauvegardée ({data.MapRows.Count} lignes).");
        }

        private void LoadMap(string name)
        {
            string json = PlayerPrefs.GetString(PrefsKeyPrefix + name, "");
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning($"[MapEditor] Sauvegarde '{name}' introuvable.");
                return;
            }

            var save = JsonUtility.FromJson<MapSaveData>(json);
            if (save == null)
            {
                Debug.LogWarning($"[MapEditor] JSON invalide pour '{name}'.");
                return;
            }

            // If a LevelData is selected, apply map rows via SerializedObject
            if (_levelData != null)
            {
                var so = new SerializedObject(_levelData);
                var rowsProp     = so.FindProperty("mapRows");
                var variantsProp = so.FindProperty("gridVariants");
                var sizeProp     = so.FindProperty("cellSize");

                WriteStringArray(rowsProp, save.mapRows);
                WriteStringArray(variantsProp, save.gridVariants);
                sizeProp.floatValue = save.cellSize;

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(_levelData);
                AssetDatabase.SaveAssets();
                Debug.Log($"[MapEditor] Carte '{name}' chargée dans {_levelData.name}.");
            }
            else
            {
                Debug.Log($"[MapEditor] Carte '{name}' lue (pas de LevelData cible sélectionné).\n{json}");
            }
        }

        private void DeleteSave(string name)
        {
            PlayerPrefs.DeleteKey(PrefsKeyPrefix + name);
            _savedNames.Remove(name);
            FlushIndex();
            PlayerPrefs.Save();
            Debug.Log($"[MapEditor] Sauvegarde '{name}' supprimée.");
        }

        private void RefreshIndex()
        {
            string raw = PlayerPrefs.GetString(PrefsIndexKey, "");
            _savedNames = new List<string>(
                raw.Length > 0
                    ? raw.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    : Array.Empty<string>()
            );
        }

        private void FlushIndex() =>
            PlayerPrefs.SetString(PrefsIndexKey, string.Join("\n", _savedNames));

        private static string[] CopyRows(IReadOnlyList<string> src)
        {
            var arr = new string[src.Count];
            for (int i = 0; i < src.Count; i++) arr[i] = src[i];
            return arr;
        }

        private static void WriteStringArray(SerializedProperty prop, string[] values)
        {
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).stringValue = values[i];
        }
    }
}
