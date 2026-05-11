#!/usr/bin/env node
/**
 * generate_level_assets.js
 * Reads JSON files from Assets/Editor/LevelsRaw/ and writes Unity .asset YAML files
 * directly to Assets/ScriptableObjects/Levels/
 *
 * Does NOT require Unity Editor to be open.
 * Generates new GUIDs for new assets (stable: based on level id hash).
 * Skips world1-1 (overwrite-protected).
 *
 * Usage: node tools/generate_level_assets.js
 */

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const JSON_DIR = path.join(__dirname, '..', 'Assets', 'Editor', 'LevelsRaw');
const OUTPUT_DIR = path.join(__dirname, '..', 'Assets', 'ScriptableObjects', 'Levels');
const LEVEL_DATA_SCRIPT_GUID = 'ecf15f4edfd5f404fa83a12c7a61fe34';

// Enemy asset GUIDs (from Assets/ScriptableObjects/Enemies/*.asset.meta)
const ENEMY_GUIDS = {
  AiHub:         'f80d00cd3a6c48479a43611ea93e5a5b',
  ApocalypseBoss:'13ebf72e31754d73a59d48ce60b0214e',
  Assassin:      '53c26439f6ec4adcad7bc70ca2fd30dc',
  Basic:         'b88d0873b2dd244bc94ed0a02cbaa08a',
  Boss:          'bf5f19f1e87a4f8fa2e35f878f01cb90',
  BrigandBoss:   '831784d13ef248469c5fa6a367032eb9',
  Brute:         'b9f34994a48ae4139b54151e8489ee0b',
  CorsairBoss:   '295833dfe90749afb9a41b2b24f60062',
  CosmicBoss:    '64e30f2e7d3a4ae2b24c7fcac6f068be',
  CyberBasic:    '13bc9bbd1a374536b434c1974836c932',
  CyberBrute:    '2186bca57c0a4a548f3ee9f451b424b1',
  CyberFlyer:    '277d1bd3c17d42ff8ef7421a30b2d755',
  CyberRunner:   'b6f92f3b26dc4606a8721936c4200d18',
  DesertRunner:  'b7f6bdabafca416aa07e5b0e68ee3b5b',
  DragonBoss:    'dd55132548814fbab8eac9b19eb89d6b',
  Flyer:         '46c18104b7524370aded20016ebcef4f',
  ForestBee:     '30ee745ced0c49cc9758c5ae02f3af43',
  ForestBrute:   'e52ebe92cb354e7ea45f2346fceb3abc',
  Imp:           '57b80134c2304d3884080285b6a9b5f4',
  KrakenBoss:    '2b4b22f67b0f4685959b13b2888fdb4f',
  Midboss:       '7739f7da682048448f3198f53002e7b2',
  PlainePigeon:  '93d5e307027642a5ad4a76cdfed9c249',
  Runner:        'a9e91ba41c5e14d28bfa17fed79199bd',
  Shielded:      '7473487740904c92a3c3b0355ee3f8c3',
  SkeletonMinion:'7c2ba4d48ba842b5bc0fa9ee7cc0af9b',
  SubmarinRunner:'acda696e9ace41af92a3ba04d2dbe648',
  WarlordBoss:   '78c322ed781e42f8a0bb328ab5608da4',
  WizardKing:    '445c76c585f4497683e10135331c0d6d',
};

// Phaser enemyId -> Unity asset name
const ENEMY_NAME_MAP = {
  basic:           'Basic',
  runner:          'Runner',
  brute:           'Brute',
  shielded:        'Shielded',
  assassin:        'Assassin',
  flyer:           'Flyer',
  imp:             'Imp',
  midboss:         'Midboss',
  boss:            'Boss',
  skeleton_minion: 'SkeletonMinion',
  skeleton:        'SkeletonMinion',
  brigand_boss:    'BrigandBoss',
  warlord_boss:    'WarlordBoss',
  dragon_boss:     'DragonBoss',
  corsair_boss:    'CorsairBoss',
  apocalypse_boss: 'ApocalypseBoss',
  cosmic_boss:     'CosmicBoss',
  kraken_boss:     'KrakenBoss',
  ai_hub:          'AiHub',
  cyber_basic:     'CyberBasic',
  cyber_runner:    'CyberRunner',
  cyber_brute:     'CyberBrute',
  cyber_flyer:     'CyberFlyer',
  desert_runner:   'DesertRunner',
  forest_brute:    'ForestBrute',
  forest_bee:      'ForestBee',
  plaine_pigeon:   'PlainePigeon',
  pigeon:          'PlainePigeon',
  wizard_king:     'WizardKing',
  submarin_runner: 'SubmarinRunner',
};

function toPascalCase(s) {
  return s.split('_').map(w => w.length > 0 ? w[0].toUpperCase() + w.slice(1) : '').join('');
}

function resolveEnemy(enemyId) {
  const name = ENEMY_NAME_MAP[enemyId.toLowerCase()] || toPascalCase(enemyId);
  const guid = ENEMY_GUIDS[name];
  if (!guid) {
    console.warn(`  [WARN] Unknown enemy: ${enemyId} -> ${name} (no GUID)`);
    return null;
  }
  return { name, guid };
}

// Deterministic GUID from level id (so re-runs don't change GUIDs)
function deterministicGuid(id) {
  return crypto.createHash('md5').update('crowd-defense-level-' + id).digest('hex');
}

function assetNameFromId(id) {
  if (id.startsWith('world')) return 'W' + id.slice(5);
  return id;
}

// Escape special chars for YAML string (minimal: only backslash Unicode chars)
function yamlString(s) {
  // Use double-quoted YAML string with Unicode escapes for non-ASCII
  if (/^[\x20-\x7e]*$/.test(s)) return s; // pure ASCII: no quotes needed
  // Has non-ASCII: emit as double-quoted with \uXXXX escapes
  let escaped = '';
  for (const ch of s) {
    const code = ch.codePointAt(0);
    if (code > 127) {
      escaped += `\\u${code.toString(16).padStart(4, '0')}`;
    } else {
      escaped += ch;
    }
  }
  return `"${escaped}"`;
}

function generateYaml(data, assetName) {
  const guid = deterministicGuid(data.id);
  const lines = [];

  lines.push('%YAML 1.1');
  lines.push('%TAG !u! tag:unity3d.com,2011:');
  lines.push('--- !u!114 &11400000');
  lines.push('MonoBehaviour:');
  lines.push('  m_ObjectHideFlags: 0');
  lines.push('  m_CorrespondingSourceObject: {fileID: 0}');
  lines.push('  m_PrefabInstance: {fileID: 0}');
  lines.push('  m_PrefabAsset: {fileID: 0}');
  lines.push('  m_GameObject: {fileID: 0}');
  lines.push('  m_Enabled: 1');
  lines.push('  m_EditorHideFlags: 0');
  lines.push(`  m_Script: {fileID: 11500000, guid: ${LEVEL_DATA_SCRIPT_GUID}, type: 3}`);
  lines.push(`  m_Name: ${assetName}`);
  lines.push(`  m_EditorClassIdentifier: CrowdDefense::CrowdDefense.Data.LevelData`);
  lines.push(`  id: ${data.id}`);
  lines.push(`  displayName: ${yamlString(data.displayName)}`);
  lines.push(`  theme: ${data.theme}`);
  lines.push(`  world: ${data.world}`);
  lines.push(`  level: ${data.level}`);

  // mapRows
  if (data.mapRows && data.mapRows.length > 0) {
    lines.push('  mapRows:');
    for (const row of data.mapRows) lines.push(`  - ${row}`);
  } else {
    lines.push('  mapRows: []');
  }

  lines.push(`  cellSize: ${data.cellSize}`);
  lines.push(`  startCoins: ${data.startCoins}`);
  lines.push(`  overrideCastleHP: ${data.overrideCastleHP ? 1 : 0}`);
  lines.push(`  castleHPOverride: ${data.castleHPOverride}`);
  lines.push(`  lossMode: 0`);
  lines.push(`  allowMultiMagnet: ${data.allowMultiMagnet ? 1 : 0}`);

  // Waves
  const unknownEnemies = [];
  if (data.waves && data.waves.length > 0) {
    lines.push('  waves:');
    for (const wave of data.waves) {
      const keys = wave.typeKeys || [];
      const counts = wave.typeCounts || [];
      const entries = [];
      for (let i = 0; i < keys.length; i++) {
        const enemy = resolveEnemy(keys[i]);
        if (enemy) entries.push({ guid: enemy.guid, count: counts[i] });
        else unknownEnemies.push(keys[i]);
      }

      if (entries.length === 0) {
        lines.push('  - entries: []');
      } else {
        lines.push('  - entries:');
        for (const e of entries) {
          lines.push(`    - type: {fileID: 11400000, guid: ${e.guid}, type: 2}`);
          lines.push(`      count: ${e.count}`);
        }
      }
      lines.push(`    spawnRateMs: ${wave.spawnRateMs}`);
      lines.push(`    breakMs: ${wave.breakMs}`);
      lines.push(`    portalIdx: -1`);
    }
  } else {
    lines.push('  waves: []');
  }

  return { yaml: lines.join('\n') + '\n', guid, unknownEnemies };
}

function generateMeta(guid) {
  return `fileFormatVersion: 2\nguid: ${guid}\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`;
}

if (!fs.existsSync(OUTPUT_DIR)) fs.mkdirSync(OUTPUT_DIR, { recursive: true });

const jsonFiles = fs.readdirSync(JSON_DIR)
  .filter(f => f.endsWith('.json'))
  .sort();

console.log(`Processing ${jsonFiles.length} JSON files...`);
let created = 0, updated = 0, skipped = 0;
const allMissingEnemies = new Set();

for (const f of jsonFiles) {
  const id = path.basename(f, '.json');
  if (id === 'world1-1') {
    console.log('  [SKIP] world1-1 (overwrite-protected)');
    skipped++;
    continue;
  }

  const data = JSON.parse(fs.readFileSync(path.join(JSON_DIR, f), 'utf8'));
  const assetName = assetNameFromId(data.id);
  const assetPath = path.join(OUTPUT_DIR, `${assetName}.asset`);
  const metaPath = assetPath + '.meta';

  const exists = fs.existsSync(assetPath);
  const { yaml, guid, unknownEnemies } = generateYaml(data, assetName);

  // Preserve existing GUID if asset already exists (keeps Unity references stable)
  let finalGuid = guid;
  if (exists && fs.existsSync(metaPath)) {
    const metaContent = fs.readFileSync(metaPath, 'utf8');
    const m = metaContent.match(/^guid: ([a-f0-9]+)/m);
    if (m) finalGuid = m[1];
  }

  fs.writeFileSync(assetPath, yaml);
  if (!exists) {
    fs.writeFileSync(metaPath, generateMeta(finalGuid));
    created++;
  } else {
    updated++;
  }

  for (const e of unknownEnemies) allMissingEnemies.add(e);
  console.log(`  [${exists ? 'UPDATE' : 'CREATE'}] ${assetName}.asset (${data.waves.length} waves)`);
}

if (allMissingEnemies.size > 0)
  console.warn(`\nMissing enemy mappings: ${[...allMissingEnemies].join(', ')}`);

console.log(`\nDone. Created: ${created}, Updated: ${updated}, Skipped: ${skipped}`);
