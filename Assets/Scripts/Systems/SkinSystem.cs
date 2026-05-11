#nullable enable
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Entities;

namespace CrowdDefense.Systems
{
    /// <summary>
    /// Singleton that resolves the active SkinDef for any Tower, Hero, or Enemy.
    /// Tower.Init and Enemy.Init query GetActiveSkin before spawning GLTF.
    /// ApplyToHero / ApplyToTowers refresh live instances after a skin equip from the picker.
    /// </summary>
    public class SkinSystem : MonoSingleton<SkinSystem>
    {
        /// <summary>
        /// Returns the active SkinDef for the given entity, or null if using default visuals.
        /// Returns null (not the default SkinDef) when no custom skin is equipped —
        /// callers check for null to skip alternate GLTF spawn.
        /// </summary>
        public SkinDef? GetActiveSkin(SkinTargetType targetType, string targetId)
        {
            var reg = SkinRegistry.Get();
            if (reg == null) return null;

            string typeKey = targetType.ToString();
            string? skinId = SaveSystem.GetEquippedSkin(typeKey, targetId);
            if (string.IsNullOrEmpty(skinId)) return null;

            var def = reg.FindById(skinId!);
            if (def == null) return null;

            if (def.IsDefault) return null;

            // Ensure owned (default skins are always valid; purchased checked here)
            if (!def.IsDefault && !SaveSystem.IsSkinOwned(skinId!))
                return null;

            return def;
        }

        /// <summary>
        /// Equips a skin for the given entity type+id. Validates ownership first.
        /// Returns false if the skin is not owned and not default.
        /// </summary>
        public bool EquipSkin(string skinId, SkinTargetType targetType, string targetId)
        {
            var reg = SkinRegistry.Get();
            if (reg == null) return false;

            var def = reg.FindById(skinId);
            if (def == null) return false;

            if (!def.IsDefault && !SaveSystem.IsSkinOwned(skinId))
                return false;

            SaveSystem.SetEquippedSkin(targetType.ToString(), targetId, skinId);
            return true;
        }

        public void UnlockAndEquip(string skinId, SkinTargetType targetType, string targetId)
        {
            SaveSystem.UnlockSkin(skinId);
            EquipSkin(skinId, targetType, targetId);
        }

        /// <summary>
        /// Refreshes the visual mesh and stat bonuses on the active Hero after a skin equip.
        /// Uses LevelRunner.Instance to resolve the baseline HeroType.
        /// </summary>
        public void ApplyToHero(Hero hero)
        {
            if (hero == null) return;

            var heroType = LevelRunner.Instance?.HeroTypeDef;
            string baseKey = heroType?.AssetKey ?? "";
            string heroId  = heroType?.Id ?? "";

            var skin = GetActiveSkin(SkinTargetType.Hero, heroId);

            if (skin?.AlternateGLTF != null)
                hero.ApplySkinVisual(skin.Id);
            else
                hero.ApplySkinVisual(baseKey);

            if (skin != null)
                hero.ApplySkinBonuses(skin);
        }

        /// <summary>
        /// Refreshes visuals on all currently placed Tower instances whose skin changed.
        /// Tower.Init already applies skin at spawn; this handles hot-swaps from the picker.
        /// </summary>
        public void ApplyToTowers(IReadOnlyList<Tower> towers)
        {
            if (towers == null) return;
            for (int i = 0; i < towers.Count; i++)
            {
                var t = towers[i];
                if (t == null || t.Config == null) continue;
                RefreshTowerVisual(t);
            }
        }

        private void RefreshTowerVisual(Tower tower)
        {
            if (tower.Config == null) return;

            var skin = GetActiveSkin(SkinTargetType.Tower, tower.Config.Id);

            // Remove existing Mesh_ / Skin_ children
            var toRemove = new List<Transform>();
            for (int i = 0; i < tower.transform.childCount; i++)
            {
                var ch = tower.transform.GetChild(i);
                if (ch.name.StartsWith("Mesh_") || ch.name.StartsWith("Skin_"))
                    toRemove.Add(ch);
            }
            foreach (var ch in toRemove) Destroy(ch.gameObject);

            // Re-enable placeholder primitives before attempting new spawn
            for (int i = 0; i < tower.transform.childCount; i++)
            {
                var ch = tower.transform.GetChild(i);
                if (ch.name == "Base" || ch.name == "Top")
                    ch.gameObject.SetActive(true);
            }

            GameObject? meshChild = null;

            if (skin?.AlternateGLTF != null)
            {
                for (int i = 0; i < tower.transform.childCount; i++)
                {
                    var ch = tower.transform.GetChild(i);
                    if (ch.name == "Base" || ch.name == "Top")
                        ch.gameObject.SetActive(false);
                }
                var inst = Instantiate(skin.AlternateGLTF, tower.transform);
                inst.name = "Skin_" + skin.AlternateGLTF.name;
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                inst.transform.localScale    = Vector3.one;
                meshChild = inst;
            }
            else
            {
                var registry = Resources.Load<AssetRegistry>("AssetRegistry");
                var prefab   = registry != null ? registry.Get(tower.Config.AssetKey) : null;
                if (prefab != null)
                {
                    for (int i = 0; i < tower.transform.childCount; i++)
                    {
                        var ch = tower.transform.GetChild(i);
                        if (ch.name == "Base" || ch.name == "Top")
                            ch.gameObject.SetActive(false);
                    }
                    var inst = Instantiate(prefab, tower.transform);
                    inst.name = "Mesh_" + tower.Config.AssetKey;
                    inst.transform.localPosition = Vector3.zero;
                    inst.transform.localRotation = Quaternion.identity;
                    inst.transform.localScale    = Vector3.one;
                    meshChild = inst;
                }
            }

            var toonRoot = meshChild != null ? meshChild : tower.gameObject;
            Color bodyColor = tower.Config.BodyColor;

            if (skin?.AlternateMaterial != null)
                CrowdDefense.Visual.MaterialController.ApplyOverrideMaterial(toonRoot, skin.AlternateMaterial);
            else if (skin != null && skin.UseBodyColorOverride)
            {
                bodyColor = skin.BodyColorOverride;
                CrowdDefense.Visual.MaterialController.ApplyToon(toonRoot, bodyColor);
            }
            else
                CrowdDefense.Visual.MaterialController.ApplyToon(toonRoot, bodyColor);

            CrowdDefense.Visual.Outline.ApplyToHierarchy(toonRoot.transform);

            if (skin != null && skin.ThemeIndex >= 0)
                CrowdDefense.Visual.AssetVariants.ApplyThemeIndex(toonRoot, skin.ThemeIndex);
            else if (skin != null && skin.UseBodyColorOverride)
                CrowdDefense.Visual.AssetVariants.ApplySkin(toonRoot, skin);
        }
    }
}
