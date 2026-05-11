#!/usr/bin/env node
/**
 * extract_levels.js
 * Reads Phaser world*.js level files and outputs JSON to Assets/Editor/LevelsRaw/
 * Usage: node tools/extract_levels.js
 *
 * Processes only canonical world files matching worldX-Y.js pattern.
 * Skips: debug-*, endless, boss_rush, world*-mazetest, world*-showcase, world*-streamtest
 */

const fs = require('fs');
const path = require('path');
const vm = require('vm');

const SOURCE_DIR = '/Users/mike/Work/milan project/src-v3/data/levels';
const OUTPUT_DIR = path.join(__dirname, '..', 'Assets', 'Editor', 'LevelsRaw');

if (!fs.existsSync(OUTPUT_DIR)) {
  fs.mkdirSync(OUTPUT_DIR, { recursive: true });
}

const files = fs.readdirSync(SOURCE_DIR)
  .filter(f => /^world\d+-\d+\.js$/.test(f))
  .sort();

console.log(`Found ${files.length} canonical level files.`);

let ok = 0;
let skipped = 0;

for (const file of files) {
  const filePath = path.join(SOURCE_DIR, file);
  let src = fs.readFileSync(filePath, 'utf8');

  // Strip ES6 import lines and export default wrapper
  src = src
    .replace(/^import\s+.*?;\s*$/gm, '')
    .replace(/export\s+default\s*/, 'module.exports = ');

  const sandbox = { module: { exports: {} }, require: () => ({}) };
  try {
    vm.runInNewContext(src, sandbox);
    const data = sandbox.module.exports;

    // Extract world and level numbers from filename
    const match = file.match(/^world(\d+)-(\d+)\.js$/);
    const worldNum = parseInt(match[1], 10);
    const levelNum = parseInt(match[2], 10);

    // Build normalized JSON
    const json = {
      id: data.id || `world${worldNum}-${levelNum}`,
      displayName: data.name || `World ${worldNum}-${levelNum}`,
      theme: data.theme || 'plaine',
      world: worldNum,
      level: levelNum,
      cellSize: typeof data.cellSize === 'number' ? data.cellSize : 4.0,
      mapRows: Array.isArray(data.map) ? data.map : [],
      startCoins: typeof data.startCoins === 'number' ? data.startCoins : 120,
      overrideCastleHP: typeof data.castleHP === 'number',
      castleHPOverride: typeof data.castleHP === 'number' ? data.castleHP : 200,
      allowMultiMagnet: false,
      waves: [],
    };

    if (data.waves && Array.isArray(data.waves.list)) {
      json.waves = data.waves.list.map((w, i) => {
        const types = (w.types && typeof w.types === 'object') ? w.types : {};
        const typeKeys = Object.keys(types);
        const typeCounts = typeKeys.map(k => types[k]);
        return {
          index: i,
          spawnRateMs: typeof w.spawnRateMs === 'number' ? w.spawnRateMs : 600,
          breakMs: typeof w.breakMs === 'number' ? w.breakMs : 0,
          typeKeys,
          typeCounts,
        };
      });
    }

    const outFile = path.join(OUTPUT_DIR, `${json.id}.json`);
    fs.writeFileSync(outFile, JSON.stringify(json, null, 2));
    console.log(`  [OK] ${file} -> ${json.id}.json (${json.waves.length} waves)`);
    ok++;
  } catch (err) {
    console.error(`  [SKIP] ${file}: ${err.message}`);
    skipped++;
  }
}

console.log(`\nDone: ${ok} exported, ${skipped} skipped.`);
