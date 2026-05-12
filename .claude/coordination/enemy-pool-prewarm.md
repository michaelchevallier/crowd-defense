# EnemyPool Prewarm Audit — 2026-05-12

## Constat

`GetOrCreatePool` utilisait `defaultCapacity: 20`. Unity `ObjectPool` alloue seulement la capacité de la **List interne** — il n'instancie **aucun objet** à l'avance. Résultat : chaque spawn de la vague 1 (et de toute nouvelle vague avec un type non vu) déclenchait `Instantiate + GetComponent + AddComponent<EnemyHpBar>` en plein frame, causant des spikes GC sur le premier burst.

## Fix appliqué

### EnemyPool.cs
- `defaultCapacity: 20 → 50` : réduit les réallocations de liste interne en wave late.
- Nouvelle méthode `PrewarmType(EnemyType type, int requestedCount)` : Get × N puis Release × N avant la vague, cap boss = 2, cap mob = 30.

### WaveManager.cs
- Appel `EnemyPool.Instance.PrewarmType(entry.type, count)` dans `BeginWave`, après le calcul des counts et avant le Fisher-Yates shuffle. Timing optimal : tous les Instantiate se produisent dans le temps de préparation de vague (avant premier spawn), pas mid-wave.

## Gains attendus

- Vague 1 : 0 Instantiate mid-frame (tous pré-alloués).
- Vagues suivantes avec types déjà vus : pool déjà peuplée → 0 Instantiate.
- Boss (id contient "boss") : cap à 2 pour ne pas sur-allouer.
- Mobs réguliers : cap à 30 (wave 30+ ≈ 40 mobs mais les 10 premiers sont pré-chauds, les suivants recyclent des released).
