#nullable enable
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;

namespace CrowdDefense.Systems
{
    /// <summary>
    /// Singleton that resolves the active SkinDef for any Tower, Hero, or Enemy.
    /// Tower.Init and Enemy.Init query GetActiveSkin before spawning GLTF.
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
    }
}
