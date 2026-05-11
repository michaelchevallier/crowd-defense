#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace CrowdDefense.Data
{
    [CreateAssetMenu(menuName = "CrowdDefense/DoctrineRegistry", fileName = "DoctrineRegistry")]
    public class DoctrineRegistry : ScriptableObject
    {
        private static DoctrineRegistry? _cached;

        public static DoctrineRegistry Get()
        {
            if (_cached != null) return _cached;
            _cached = Resources.Load<DoctrineRegistry>("DoctrineRegistry");
            if (_cached == null) _cached = CreateDefault();
            return _cached;
        }

        [SerializeField] private DoctrineDef[] defs = System.Array.Empty<DoctrineDef>();

        private Dictionary<string, DoctrineDef>? _lookupCache;

        public DoctrineDef[] All => defs;

        public DoctrineDef? Find(string id)
        {
            if (_lookupCache == null) BuildCache();
            _lookupCache!.TryGetValue(id, out var def);
            return def;
        }

        private void BuildCache()
        {
            _lookupCache = new Dictionary<string, DoctrineDef>(defs.Length);
            foreach (var d in defs)
                if (d != null && !string.IsNullOrEmpty(d.id))
                    _lookupCache[d.id] = d;
        }

        private void OnEnable() => _lookupCache = null;

        // Built-in defaults (V5 doctrine school port).
        // Used when no DoctrineRegistry.asset exists in Resources.
        private static DoctrineRegistry CreateDefault()
        {
            var reg = CreateInstance<DoctrineRegistry>();
            reg.name = "DoctrineRegistry";
            reg.defs = new[]
            {
                MakeDef("sentinel",  "Sentinelle",  "sentinel",  10,
                    "Portée des tours +10%, PV château +15%.",
                    new DoctrineModifier { key = "TowerDamageMul", value = 1.10f },
                    new DoctrineModifier { key = "CastleHPBase",   value = 1.15f }),

                MakeDef("berserker", "Berserker",   "berserker", 10,
                    "Dégâts tours +15%, PV château -15%.",
                    new DoctrineModifier { key = "TowerDamageMul", value = 1.15f },
                    new DoctrineModifier { key = "CastleHPBase",   value = 0.85f }),

                MakeDef("merchant",  "Marchand",    "merchant",  10,
                    "Intérêt banque +50%, bonus vague rapide +40%, aimant pièces +20%.",
                    new DoctrineModifier { key = "BankInterestRate", value = 1.50f },
                    new DoctrineModifier { key = "SkipBonusGold",    value = 1.40f },
                    new DoctrineModifier { key = "MagnetCoinMul",    value = 1.20f }),

                MakeDef("paladin",   "Paladin",     "paladin",   10,
                    "PV château +30%, revente tours +10%.",
                    new DoctrineModifier { key = "CastleHPBase",    value = 1.30f },
                    new DoctrineModifier { key = "SellRefundRatio", value = 1.10f }),

                MakeDef("saboteur",  "Saboteur",    "saboteur",  10,
                    "Bonus streak +40%, multiplicateur de horde -10%.",
                    new DoctrineModifier { key = "StreakBonusPerWave", value = 1.40f },
                    new DoctrineModifier { key = "SwarmMul",           value = 0.90f }),

                MakeDef("alchemist", "Alchimiste",  "alchemist", 10,
                    "Intérêt banque +80%, portée aimant +30%.",
                    new DoctrineModifier { key = "BankInterestRate", value = 1.80f },
                    new DoctrineModifier { key = "MagnetRange",      value = 1.30f }),

                MakeDef("trickster", "Imposteur",   "trickster", 10,
                    "Bonus vague rapide +80%, dégâts tours -10%.",
                    new DoctrineModifier { key = "SkipBonusGold",  value = 1.80f },
                    new DoctrineModifier { key = "TowerDamageMul", value = 0.90f }),
            };
            return reg;
        }

        private static DoctrineDef MakeDef(string id, string displayName, string emoji,
            int gemCost, string desc, params DoctrineModifier[] modifiers)
        {
            var def = CreateInstance<DoctrineDef>();
            def.name        = id;
            def.id          = id;
            def.displayName = displayName;
            def.iconEmoji   = emoji;
            def.gemCost     = gemCost;
            def.description = desc;
            def.modifiers   = modifiers;
            return def;
        }
    }
}
