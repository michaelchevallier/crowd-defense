#nullable enable
using UnityEngine;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // F5 = quick save current RunState to a dedicated PlayerPrefs key.
    // F9 = restore that snapshot and apply it back to SaveSystem.
    // Sibling of HudController — auto-added via EnsureSibling<QuickSaveHotkey>().
    public class QuickSaveHotkey : MonoBehaviour
    {
        private const string QUICKSAVE_KEY = "cd_quicksave_runstate_v2";

        private void Update()
        {
            if (Input.GetKeyDown(KeyBindings.GetKey("save")))
                QuickSave();
            else if (Input.GetKeyDown(KeyBindings.GetKey("load")))
                QuickLoad();
        }

        private static void QuickSave()
        {
            var rs = SaveSystem.GetRunState();
            PlayerPrefs.SetString(QUICKSAVE_KEY, JsonUtility.ToJson(rs));
            PlayerPrefs.Save();
            Toast.Show("Sauvegarde rapide", "Etat de run sauvegarde (F9 pour restaurer)", 2000, null, ToastType.Generic);
        }

        private static void QuickLoad()
        {
            string json = PlayerPrefs.GetString(QUICKSAVE_KEY, "");
            if (string.IsNullOrEmpty(json))
            {
                Toast.Show("Aucune sauvegarde", "Appuie sur F5 d'abord pour sauvegarder", 2000, null, ToastType.Generic);
                return;
            }

            var rs = JsonUtility.FromJson<RunState>(json);
            if (rs == null)
            {
                Toast.Show("Sauvegarde corrompue", "Snapshot quicksave illisible", 2000, null, ToastType.Generic);
                return;
            }

            SaveSystem.SetRunState(rs);
            Toast.Show("Sauvegarde restauree", "Run recharge depuis le snapshot F5", 2000, null, ToastType.Generic);
        }
    }
}
