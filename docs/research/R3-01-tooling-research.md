# R3-01 — Game Dev Tooling Patterns Benchmark

**Sprint** : R3 — Tooling & Portability Research
**Auteur** : td-researcher (Opus) adapté domaine tooling
**Date** : 2026-05-11
**Scope** : Patterns industrie pour outils de game dev (monsters, towers, levels, balance). 5-7 patterns analysés avec setup/maintenance cost et applicabilité descriptive Milan CD V3.
**Hors-scope** : aucune proposition de top 3 prioritaire ; aucune modification code Milan.

---

## Méthodologie

Sources : docs officielles, GitHub READMEs, blogs 2026. 7 patterns retenus (fusion Tiled+LDtk+Phaser en un). Format R1/R2 : H2 par pattern + tableau récap + universels/différenciants + 5 candidats descriptifs.

---

## 1. Unity ScriptableObjects + Custom Editors

### 1.1 Description courte

Pattern phare Unity (engine 2010+, popularisé massivement 2017+ via Unite talks). Une `ScriptableObject` est une classe C# Unity qui sert d'**asset de données** (fichier `.asset` dans le projet), indépendamment d'une scène. Combinée à `CustomEditor` ou `PropertyDrawer`, elle permet de modifier la data via l'Inspector Unity (champs typés, validation, drag&drop d'autres assets) sans toucher au code. Le projet de référence : "Unite Austin 2017 — Game Architecture with Scriptable Objects" (Ryan Hipple). Suite 2026 : **Odin Inspector** (Sirenix) ajoute attributs `[Required]`, `[Range]`, `[ValidateInput]`, validation profiles, sérialisation polymorphe ([odininspector.com](https://odininspector.com/tutorials/odin-project-validator/odin-validation-profiles-explained)).

### 1.2 Use case typique TD/2D

- 1 asset `TowerData.asset` par type de tour → champs cost/range/damage/projectilePrefab.
- 1 asset `EnemyData.asset` par mob → hp/speed/reward/lootTable.
- 1 asset `WaveDefinition.asset` qui référence d'autres `EnemyData` via liste typée.
- Designer ouvre l'asset, modifie la valeur, Ctrl+S → la prochaine session in-Editor utilise la nouvelle data. Aucune recompilation C#.
- Strategy pattern : assigner `BehaviorSO` (move/attack) à un mob via drag&drop ([dev.to/eriksk Strategy pattern via SO](https://dev.to/eriksk/implementing-the-strategy-design-pattern-using-scriptable-objects-in-unity-292i)).

### 1.3 Outil/SDK utilisé

- Unity Editor (engine bundle, gratuit jusqu'à $200k revenu).
- `[CreateAssetMenu(...)]` attribute pour créer depuis menu projet.
- `CustomEditor`, `EditorGUILayout`, `SerializedProperty` API pour custom inspectors.
- Optionnel : Odin Inspector (~$55/seat), drastiquement réduit le boilerplate ([odininspector.com/tutorials/using-attributes/...](https://odininspector.com/tutorials/using-attributes/how-to-use-odin-inspector-with-scriptable-objects)).

### 1.4 Setup cost

- Vanilla : 4-8h pour configurer un premier asset type + inspector custom (apprentissage Unity Editor API si pas connu).
- Odin : 1-2h, 90% des cas couverts par attributs prêts à l'emploi.
- Courbe d'apprentissage : faible si Unity déjà connu ; ~1 semaine si découverte Unity Editor pour designer non-dev.

### 1.5 Maintenance cost

- Ajout d'un champ : trivial (add field + reload Editor → champ apparaît, valeurs existantes conservées).
- Refactor schema : Unity gère migrations automatiquement pour 90% des cas ; sérialisation cassée → SerializationCallback à coder.
- Validation profiles Odin : sauvegardées comme SOs elles-mêmes, partagées via git.

### 1.6 Limites

- **Lock-in Unity total**. Inutile hors Unity. Si Milan reste Three.js → pattern non transposable directement, sauf via portage Unity (cf R3-02 portability).
- Designer doit ouvrir Unity Editor (pas un browser).
- Asset = fichier binaire `.asset` (YAML en interne), pas trivial à diff en git sans Unity Smart Merge.
- Pas de validation cross-asset native (nécessite Odin ou tooling custom).

### 1.7 Applicabilité à Milan CD V3 (descriptif)

Le pattern SO+CustomEditor représente le **gold standard** que tout autre éditeur de data game dev essaie de reproduire (séparation data/code, drag&drop, validation typée, hot-swap in-Editor). Pour Milan, le pattern peut inspirer une **architecture data-driven** : extraire les 12 `TOWER_TYPES` et 30 `ENEMY_TYPES` de leurs hardcoded JS files vers des fichiers JSON dédiés (`data/towers/lava.json`, `data/enemies/swarmer.json`), modifiables par un GUI dédié. Mais l'écosystème Unity Editor n'existe pas en web/Three.js — il faudrait reproduire à la main avec **schemas JSON + GUI auto-générée** (pattern §4) ou **standalone webapp** (pattern §5). Le coût d'imitation 1-pour-1 est élevé. Sa valeur conceptuelle (asset comme data immuable, init pattern, validation profiles) reste pertinente.

---

## 2. LDtk + Tiled + Phaser Editor 2D — Level Editors Desktop/Web

### 2.1 Description courte

Trois éditeurs de niveaux 2D matures, complémentaires :

- **Tiled** (Thorbjørn Lindeijer, 2008+, open source) : éditeur desktop polyvalent, .tmx (XML) ou JSON, custom properties sur tiles/layers/objects ([mapeditor.org](https://www.mapeditor.org/), [github.com/mapeditor/tiled](https://github.com/mapeditor/tiled)).
- **LDtk** (Sébastien Bénard / Deepnight, créateur de Dead Cells, 2020+) : desktop moderne (Windows/Mac/Linux), auto-tiling, entities, JSON export propre, "Super Simple Export" pour indies ([ldtk.io](https://ldtk.io/), [ldtk.io/docs/game-dev/loading/](https://ldtk.io/docs/game-dev/loading/)).
- **Phaser Editor 2D v3** (Arian Fornaris, 2022+) : IDE web pour Phaser, prefabs, script nodes, importe Tiled .json ([phaser.io/editor](https://phaser.io/editor), [github.com/PhaserEditor2D/PhaserEditor2D-v3](https://github.com/PhaserEditor2D/PhaserEditor2D-v3)).

### 2.2 Use case typique TD/2D

- Tiled : grille tiles + spawn points + waypoints comme objets, exporté .json, parsé par le runtime ([inspiredpython.com tower defense](https://www.inspiredpython.com/course/create-tower-defense-game/tower-defense-game-tile-engine-map-editor)).
- LDtk : levels avec entities typées (Tower, Enemy, Portal), fields custom par entity (rotation, type, level), exports JSON par level (un fichier par level + tileset PNG).
- Phaser Editor : pour studios Phaser, importe maps Tiled puis ajoute prefab characters/animations.

### 2.3 Outil/SDK utilisé

- Tiled : binaire desktop, free open-source, dons via OpenCollective.
- LDtk : binaire desktop, free, Haxe-based, source GitHub publiée.
- Phaser Editor : web (browser) + Electron desktop, freemium ($10-$50/mois pour features pros).

### 2.4 Setup cost

- Tiled : 2-3h pour définir un tileset + custom property schema + parser JS côté runtime.
- LDtk : 1-2h (Super Simple Export évite le parser custom).
- Phaser Editor : ~4-8h pour configurer prefabs reusable et workflow équipe.

### 2.5 Maintenance cost

- Schema custom properties : à syncro manuellement entre éditeur et runtime parser.
- LDtk versionne son JSON et casse rarement (semver respecté).
- Tiled gère bien les versions ; migrations parfois nécessaires sur major releases.

### 2.6 Limites

- **Desktop binary** (sauf Phaser Editor en web) : barrière pour collaborateurs non-techniques, install à faire.
- Pas natif Three.js : Tiled assume 2D top-down ou side-scroll, LDtk idem. Pour TD 3D Three.js (Milan v3 kingshot), parser custom requis.
- Spécifique level layout, ne couvre PAS towers/enemies stats (séparation forte data tile vs data unit).

### 2.7 Applicabilité à Milan CD V3

Milan utilise déjà une **grille ASCII** dans `data/levels/world*.js` ("XXXX.....PPP.." style), avec validation par `scripts/validate-maps.mjs`. Un éditeur dédié (LDtk en tête, vu sa qualité 2026) remplacerait l'édition manuelle de strings ASCII par un grid GUI visuel avec auto-tiling, snap, prévisualisation des paths. Custom properties peut porter spawn timing, wave compositions, portal links. Le coût "desktop binary" est neutre pour Mike (solo dev), problématique pour playtesters distants. La grammaire actuelle de Milan (~15 tile symbols + meta wave) tient en ~30 entity types LDtk. Conflit possible : Milan rend en Three.js 3D, LDtk montre 2D top-down — la coupe entre éditeur (2D plan) et runtime (3D camera) demande un layer de conversion explicite.

---

## 3. Tweakpane + lil-gui + dat.GUI — Live Tweak Browser

### 3.1 Description courte

Famille de **GUI flottantes JS** pour tweaker des valeurs en temps réel pendant l'exécution dans le browser. Lignée :

- **dat.GUI** (Google Data Arts Team, 2011+, devenu un standard Three.js) — simple, vieillissant.
- **lil-gui** (Georg Fischer, 2020+) — drop-in replacement, maintenu (51k downloads/week, 1.5k stars [npmtrends](https://npmtrends.com/control-panel-vs-controlkit-vs-dat.gui-vs-lil-gui-vs-tweakpane)).
- **Tweakpane** (Hiroki Kokubun / cocopon, 2017+, v4 2024) — moderne, plugins, design kit Figma, 80k downloads/week, 4.4k stars ([github.com/cocopon/tweakpane](https://github.com/cocopon/tweakpane), [tweakpane.github.io/docs](https://tweakpane.github.io/docs/)).
- **leva** (poimandres, 2020+) — React-first, mais utilisable hors React ([github.com/pmndrs/leva](https://github.com/pmndrs/leva)).

Tweakpane offre folders, tabs, monitor (read-only graphs), color pickers, plugins essentials (image, file inputs).

### 3.2 Use case typique TD/2D

- Pendant le gameplay, panel flottant top-right : sliders pour `tower.range`, `tower.damage`, `enemy.hp`, `enemy.speed`. Joueur/designer modifie valeur → effet visible immédiatement.
- Folders par catégorie (Towers / Enemies / Economy / Waves).
- Monitor pour KPIs : DPS effectif, gold/sec, kills/wave.
- Export du JSON state pour copier dans source.

### 3.3 Outil/SDK utilisé

- Tweakpane : `npm i tweakpane`, vanilla JS, zero deps, ~5 KB gz core.
- Plugins : `@tweakpane/plugin-essentials` (FPS graph, intervals), `@tweakpane/plugin-camerakit`.
- lil-gui : `npm i lil-gui`, drop-in dat.GUI replacement.
- leva : React only, plus de features visuelles (color palette presets, schema generator).

### 3.4 Setup cost

- Tweakpane / lil-gui : 1-2h pour un premier panel binding 20 paramètres.
- leva : 1-3h si projet React, sinon 4h+ pour intégrer React just for the GUI.
- Très faible apprentissage (~30 min docs).

### 3.5 Maintenance cost

- Ajout d'un param : `pane.addBinding(obj, 'newField')` une ligne.
- Schema sync : aucun automatique. Si on change le nom d'un champ, il faut le retrouver dans le code du pane.
- Risque : "panel zombie" si l'objet bindé est recréé/swappé (Tweakpane garde la ref initiale).

### 3.6 Limites

- **Live seulement** : modifications perdues au refresh. Persistence = localStorage manuel, ou copy-paste vers source.
- Pas de validation cross-field native (un slider HP qui dépendrait d'un autre champ).
- Browser-only (mais browser-only convient parfaitement à Milan).
- Tweakpane v4 : breaking changes vs v3, attention si tuto trouvé est v3.

### 3.7 Applicabilité à Milan CD V3

Tweakpane (ou lil-gui) est **le plus court chemin** vers du live tweak pour Milan. L'engine actuel charge `TOWER_TYPES` / `ENEMY_TYPES` une fois au démarrage ; un panel flottant exposant les fields écrits → mutation des objets globaux → effet immédiat sur la prochaine instance créée. Combiné avec un bouton "Export JSON" qui copie le state actuel, la boucle iterate-test-copy devient une question de secondes. Le bundle reste mince (~5 KB gz pour Tweakpane). Conflits possibles : valeurs déjà attachées à des instances live (towers déjà placées) ne se mettent pas à jour ; il faut soit re-placer, soit propager via un système de subscription. Le menu debug V3 (`__cd.debugOn`) offre déjà un précédent, Tweakpane le moderniserait drastiquement.

---

## 4. JSON Schema Editors — Auto-GUI from Schema

### 4.1 Description courte

Pattern "**décris ton data shape, on génère le formulaire**". Le développeur écrit un **JSON Schema** (standard IETF) qui décrit les champs, types, validations, énumérations. Une lib lit ce schema et génère un formulaire React/Vue/vanilla.

- **react-jsonschema-form (RJSF)** (Mozilla 2015, désormais rjsf-team) — le plus connu ([rjsf-team.github.io](https://rjsf-team.github.io/react-jsonschema-form/)).
- **JSON Forms** (EclipseSource, 2018+) — React/Angular/Vue ([jsonforms.io](https://jsonforms.io/)).
- **jsoneditor** (Jos de Jong) — éditeur JSON tree/text classique, non schema-driven mais avec validation.
- **UI Schema** (bemit) — séparation data schema / UI schema, automation poussée ([ui-schema.bemit.codes](https://ui-schema.bemit.codes/)).

### 4.2 Use case typique TD/2D

- Définir `tower.schema.json` : `{ "type": "object", "properties": { "cost": { "type": "integer", "minimum": 0 }, "range": { "type": "number" }, "projectileType": { "enum": ["lava", "frost", "magnet"] } } }`.
- RJSF prend ce schema + une instance JSON → formulaire avec sliders, dropdowns, validation rouge si valeur invalide.
- Workflow : designer ouvre `tower-editor.html`, charge `lava.json`, modifie via GUI, sauvegarde JSON.

### 4.3 Outil/SDK utilisé

- RJSF : `npm i @rjsf/core @rjsf/validator-ajv8`, React requis. Validateurs : ajv (le plus utilisé).
- JSON Forms : Material/Vanilla/Vue renderers ([jsonforms.io](https://jsonforms.io/)).
- jsoneditor : vanilla JS, embed dans `<div>`, support JSON Schema partiel.
- React JSON Schema Form Builder (ginkgobioworks) : éditeur du schema lui-même, drag&drop ([github.com/ginkgobioworks/react-json-schema-form-builder](https://github.com/ginkgobioworks/react-json-schema-form-builder)).

### 4.4 Setup cost

- RJSF : 3-5h pour premier schema + integration React.
- JSON Forms : 4-6h, plus de boilerplate mais plus puissant pour layouts complexes.
- jsoneditor : 1-2h en vanilla, mais GUI moins user-friendly (tree view technique).

### 4.5 Maintenance cost

- Schema = source of truth. Modifier le schema = formulaire évolue automatiquement.
- Validation centralisée : un seul fichier `.schema.json` partagé runtime+editor.
- Migration data : pas automatique. Si on supprime un field, instances existantes gardent le champ "orphan" en JSON (sauf cleanup script).

### 4.6 Limites

- React/Vue requis pour les libs les plus matures (RJSF, JSON Forms). leva non concerné mais plus orienté tweak que data edit.
- Browser-only.
- Pas d'export "JS file" natif : on édite JSON, runtime doit charger JSON et non importer un .js.
- UX par défaut peut être verbeuse (labels longs, espacement form-like) — customisation CSS nécessaire pour look game tool.

### 4.7 Applicabilité à Milan CD V3

Si Milan migre les `TOWER_TYPES` hardcoded JS vers des **fichiers JSON séparés** (`data/towers/lava.json` etc.), un éditeur schema-driven devient possible. Les avantages : validation forte (cost ≥ 0, range entre 50-500, projectileType dans un enum fermé), GUI auto-générée sans code spécifique par type, schema partagé entre l'éditeur et `scripts/validate-maps.mjs`. Le coût d'introduction React (Milan est JS vanilla) est non-trivial — alternative jsoneditor pour rester vanilla mais GUI moins polishée. Trade-off : centralisation et rigueur (schema = contract) vs simplicité (chaque field accessible directement en JS aujourd'hui). Pour un schéma complexe avec polymorphisme (towers ont attaque différentes selon `projectileType`), les `oneOf` JSON Schema gèrent ça nativement, ce qu'aucune édition manuelle JS ne fait élégamment.

---

## 5. Standalone Webapp Dashboards — React/Vue Custom Tools

### 5.1 Description courte

Pattern game studios MMO/F2P : **app séparée du jeu** (un site interne), branchée sur la même data, où game designers éditent stats, balance, économie. Souvent React ou Vue + backend léger (API REST ou direct git commits).

- **TailAdmin Vue**, **Vue Vben Admin**, **TailAdmin React**, **Apex** (shadcn/ui) : templates 2026 ([tailadmin.com/blog/react-admin-dashboard](https://tailadmin.com/blog/react-admin-dashboard), [adminlte.io/blog/vue-admin-dashboard-templates](https://adminlte.io/blog/vue-admin-dashboard-templates/)).
- **Vue Element Admin** (90k+ stars), référence open source ([github.com/PanJiaChen/vue-element-admin](https://github.com/PanJiaChen/vue-element-admin)).
- Pour MMOs : Riot Games / Wargaming / Supercell construisent souvent leurs propres (jamais open-source). Pour indies : adapter un template admin générique ([untitledui.com/blog/react-dashboards](https://www.untitledui.com/blog/react-dashboards)).

### 5.2 Use case typique TD/2D

- Dashboard "Balance" : table de toutes les tours, filtrable, sortable, edit inline, sauvegarde batch.
- Dashboard "Levels" : grille de niveaux avec heatmap difficulté (ratio kill/spend, ex Milan R2-06), drilldown sur un level → édition wave composition.
- Dashboard "Analytics" : graphs ratio kill/spend, win rate, time-to-clear par level (si telemetry connectée).

### 5.3 Outil/SDK utilisé

- React + shadcn/ui (Radix + Tailwind), Vite, TanStack Table, Recharts.
- Vue 3 + Element Plus ou Naive UI.
- Backend optionnel : Node Express, ou direct git (via gh CLI ou GitHub API) pour commit chaque save.

### 5.4 Setup cost

- Adapter template admin : 8-16h pour version minimale (login, tables data, save).
- From scratch : 20-40h+ pour parité.
- Apprentissage React/Vue : 1 semaine si pas connu.

### 5.5 Maintenance cost

- Plus important que les autres patterns. Le dashboard est une **app à part entière**, à build/deploy/maintenir.
- Schema sync avec le runtime : à coder manuellement (TS shared types ou JSON Schema pour valider).
- Si l'app utilise un backend, ops à maintenir (hosting, auth).

### 5.6 Limites

- **Overengineering** pour solo dev / petit projet. Adapté à équipes 5+ ou jeux live longs.
- Stack React/Vue introduit deps lourdes vs Milan JS vanilla.
- Si standalone (pas in-game), pas de live preview ; ou alors iframe game à côté, complexe.

### 5.7 Applicabilité à Milan CD V3

Pour Milan en solo, le pattern standalone webapp est probablement **surcalibré**. Il devient pertinent si Mike veut une vue agrégée multi-levels (heatmap par world, comparaison towers, analyse économie globale) que les patterns 1-4 n'offrent pas. L'avantage : séparation totale du jeu, pas de pollution bundle ; ouverture vers analytics et data viz lourde (Recharts, D3). Le coût : maintenance d'une 2e app et stack frontend différente du jeu. Compromis intermédiaire possible : un mini-dashboard intégré au jeu en mode debug (route `/debug-dashboard`), même bundle, mais activable seulement via `?debug=1` (déjà supporté par Milan). Pour analyses one-shot (ex le rapport R2-06 sur 80 levels), un script Node + HTML statique généré est probablement suffisant.

---

## 6. Spreadsheet → JSON Workflows (Google Sheets / Airtable / Notion DB)

### 6.1 Description courte

Pattern **Excel-first game balance** popularisé dans la mobile F2P (Supercell, King) : tout le balance se fait dans des spreadsheets, exporté en JSON via API ou plugin, consommé par le runtime.

- **Google Sheets** + Apps Script ou API → `gsx2json` ([gsx2json.com](https://gsx2json.com/)) ou export CSV via Google Sheets API.
- **Airtable** : tables relationnelles + GUI riche + API REST native ([airtable.com](https://airtable.com)), Coupler.io pour génération JSON URL ([blog.coupler.io/airtable-to-json](https://blog.coupler.io/airtable-to-json/)).
- **Notion DB** : moins mature, API existante, plus orienté docs.
- Pipeline build : script Node fetches data → outputs `data/towers.json` au commit time → bundlé par Vite.

### 6.2 Use case typique TD/2D

- 1 sheet "Towers" : colonnes name/cost/range/damage/projectile. Designer ajuste cellule, export.
- 1 sheet "Enemies" : colonnes hp/speed/reward/behavior.
- 1 sheet "Waves" : structure plus complexe (rows = spawn events), souvent moins ergonomique.
- Versioning : Airtable a un historique, Google Sheets aussi.

### 6.3 Outil/SDK utilisé

- Google Sheets : gratuit (jusqu'à limite quota), Apps Script (JS), Google Sheets API v4.
- Airtable : free tier 1000 rows/base, $20+ paid tiers.
- Coupler.io : SaaS payant pour pipelines automatisés.
- Node : pandas-like via `csv-parse`, `json2csv`, `node-fetch`.

### 6.4 Setup cost

- Google Sheets + API : 4-6h (auth OAuth, parser script, vite build hook).
- Airtable + REST : 2-4h, plus simple (API token, fetch).
- Pipeline `npm run build:data` qui regen JSON : 2h.

### 6.5 Maintenance cost

- Spreadsheet schema change = parser à mettre à jour.
- Si la collab est externe (autre human que dev), risque erreur de saisie (typo enum, valeur hors range). Validation côté script à coder.
- Airtable a des field types stricts ; Google Sheets non, plus fragile.

### 6.6 Limites

- Dépendance SaaS externe (offline = no edit).
- Pas de validation game-spécifique native (cost ≥ 0 OK, mais "cost ratio cohérent avec damage" non).
- Pas de live tweak in-game : workflow = edit sheet → re-run build → reload.
- Données sensibles dans le cloud (peu pertinent pour Milan).

### 6.7 Applicabilité à Milan CD V3

Pour Milan, l'avantage spreadsheet est l'**ergonomie tabulaire** : voir d'un coup les 12 towers et 30 enemies côte à côte, copier/coller, formules pour calculer le coût d'une formule donnée. Précisément les 12 lignes × 7 colonnes de TOWER_TYPES tiennent parfaitement en un seul sheet. Le pipeline build → JSON → bundlé est techniquement trivial avec Vite. Le risque est de fragmenter la source of truth (JS file vs sheet vs JSON). Pour Mike solo, le coût d'introduction d'un SaaS externe (Google ou Airtable) peut dépasser le gain. Pour collaborer avec un éventuel balance designer non-dev (cible R3 portability tooling, peut-être un Milan-2 collab futur), c'est le pattern le plus accessible aux non-techniciens. Pertinent si la décision est "preserver data hors-code dès aujourd'hui pour facilité future".

---

## 7. Hot-Reload Tweak Pattern (Vite HMR + Save-to-Source)

### 7.1 Description courte

Pattern hybride : **modif en jeu**, puis **propagation au fichier source** sans recompilation. Le plus proche de la "magic": le designer voit la valeur changer, et le fichier `tower.json` change avec elle, commit-friendly.

Implémentations possibles :
- **Vite HMR API** (`import.meta.hot.accept`) pour re-charger un module data sans full reload ([vite.dev/guide/api-hmr](https://vite.dev/guide/api-hmr)).
- **Vite plugin custom** avec hook `handleHotUpdate` qui détecte changes data → push WS → client update sans reload ([vite.dev/changes/hotupdate-hook](https://vite.dev/changes/hotupdate-hook)).
- Pour le **save-back** : client envoie diff via fetch POST → endpoint Vite dev server → écrit fichier.
- Pas de standard packagé, c'est du custom. Narrat game engine fait l'inverse (file change → reload UI) ([liana.one custom language plugin](https://liana.one/custom-language-plugin-for-vite)).

### 7.2 Use case typique TD/2D

- Designer joue, ouvre panel Tweakpane, slide `lava.damage` de 5 à 7.
- Bouton "Save to source" → POST /api/save-data { file: "lava.json", patch: { damage: 7 } } → endpoint Node Vite plugin écrit le file.
- File watcher Vite déclenche HMR → module data se met à jour côté client.
- Designer commit le file en sortie de session.

### 7.3 Outil/SDK utilisé

- Vite dev server + plugin custom.
- Tweakpane / lil-gui pour l'UI client.
- Côté serveur : Node `fs.writeFile` ou `fs.appendFile`, gardé en sandbox dev-only.

### 7.4 Setup cost

- Plugin Vite custom : 4-8h (familiarité Vite plugin API requise).
- Endpoint write + sécurité (no overwrite production, dev only) : 2-3h.
- Diff/patch UI client : 1-2h.

### 7.5 Maintenance cost

- Plugin custom = code à maintenir, pas de communauté large.
- Risque de désync : si plusieurs files data changés simultanément, conflits.
- À désactiver totalement en production (sinon vulnérabilité endpoint write).

### 7.6 Limites

- **Dev-only**. Ne marche pas sur le site déployé (sauf si on autorise PR via GitHub API, ce qui est un autre niveau de complexité).
- Pas un standard, peu de tutoriels publics.
- Risque sécu si endpoint write mal scoped.

### 7.7 Applicabilité à Milan CD V3

C'est le **pattern le plus différenciant** mais aussi le plus custom. Milan utilise déjà Vite ; un plugin custom save-back est techniquement faisable en ~1 jour. Le bénéfice : fermer la boucle "tweak → commit" complètement. Le développeur (Mike) joue, ajuste, et les modifs sont déjà dans des files prêts à commit. Pour le sprint TE (Tooling Execution), c'est probablement le pattern à plus haut retour si Milan veut industrialiser la boucle balance iterative. À l'inverse, c'est risqué côté complexité et debug. Variante simplifiée : pas de Vite plugin, plutôt un bouton "Download JSON" qui télécharge le fichier modifié — Mike le sauvegarde manuellement dans le repo. Moins automatique, beaucoup moins fragile.

---

## 8. AI/LLM-Assisted Content Generation

### 8.1 Description courte

Pattern émergent 2024-2026 : **prompt → spec → JS/JSON file**. Le designer décrit en langage naturel ("a fast fragile flying mob with magic resistance"), un LLM génère le JSON conformant au schema.

- Research recent : GameUIAgent (CHI 2026), GameGen via LLM (arxiv [2404.08706](https://arxiv.org/abs/2404.08706)), RPGAgent (CHI 2026 multi-agent).
- Approche pratique 2026 : Anthropic Claude / OpenAI GPT-4o avec tool use → output structuré JSON validé contre schema.
- Communautés : OpenAI Dev Community thread "GameDev + LLM" ([community.openai.com/t/...1372841](https://community.openai.com/t/ai-in-game-development-gamedev-tips-tools-techniques-and-gpt-llm-agent-integration/1372841)), CodeMag article "Can an LLM Make a Video Game?" ([codemag.com/Article/2411061](https://www.codemag.com/Article/2411061/Can-an-LLM-Make-a-Video-Game)).

### 8.2 Use case typique TD/2D

- "Génère un boss feu pour le world 5, HP ~3000, phase 1 spawn-spam, phase 2 dash, phase 3 lava trail, drops 80 gold" → LLM produit un EnemyData JSON conforme.
- Validation auto par schema (pattern §4).
- Variant : itération assistée. "Le boss actuel est trop facile, augmente HP et ajoute resistance to lava" → LLM patch le JSON existant.

### 8.3 Outil/SDK utilisé

- API Claude/OpenAI/Google avec tool use / structured output (JSON Schema enforced).
- Côté éditeur : input texte naturel + bouton "Generate" → render JSON dans GUI.
- Optionnel : agent local Ollama / llama.cpp pour gratuit / offline.

### 8.4 Setup cost

- 4-6h pour un POC avec Claude API et un schema simple.
- 1-2 jours pour boucle complète (prompt template + validation + UI insertion).
- Apprentissage prompt engineering + tool use API : 2-5h.

### 8.5 Maintenance cost

- Prompts à itérer (un mauvais prompt = mauvaises specs).
- Schema sync : si schema change, prompt template à refresher.
- Coût API (~$0.001-0.01 par génération avec modèles modernes).

### 8.6 Limites

- Pas de garantie cohérence économique (un LLM peut produire un boss à 100k HP sans réaliser que ça casse le balance).
- Hallucinations possibles (champs inventés, énums hors-schema).
- Coût récurrent API si beaucoup d'usage.
- Pas d'industrie 2026 mature pour ce workflow spécifique sur TD/2D (research-only ; peu de produits commerciaux livrés en 2026).

### 8.7 Applicabilité à Milan CD V3

Le pattern est **idéalement adapté au contexte Milan** : Mike code déjà avec Claude Code et a une grammaire data structurée (TOWER_TYPES, ENEMY_TYPES). Un outil "prompt → spec" qui produit un JSON conformant au schema serait une signature unique. Cas d'usage immédiat : générer 5-10 variantes d'un boss en explorant l'espace d'attaque possible, sélectionner les plus intéressantes manuellement. Risque : "soup" de data si génération en masse sans curation. Probabilité d'overengineering en 2026 : modérée — les API LLM sont maintenant assez fiables pour produire du JSON conformant, mais la valeur reste subjective (curation humaine nécessaire). Pertinent comme outil **secondaire** plutôt qu'unique : génération inspiration, designer adapte ensuite via éditeur classique (pattern §1, §3, §4).

---

## Tableau récapitulatif

| Pattern | Setup (h) | Maint. (h/an) | Web | Standalone | Live tweak | Validation | Différenciation |
|---|---|---|---|---|---|---|---|
| 1. Unity SO + Custom Editors | 4-8 | 5-10 | Non | Non (in-Editor) | Yes (in-Editor) | Strong (Odin) | Standard |
| 2. LDtk / Tiled / Phaser Editor | 1-6 | 2-5 | Partial | Yes (desktop) | Non | Light | Strong (lvl) |
| 3. Tweakpane / lil-gui / leva | 1-2 | 1-2 | Yes | In-game overlay | Yes | Light | Standard |
| 4. JSON Schema editors | 3-6 | 2-3 | Yes | Webapp | Non | Strong | Standard |
| 5. Custom dashboards (React/Vue) | 8-40 | 10-20 | Yes | Yes | Possible (iframe) | Custom | Differentiating (scale) |
| 6. Spreadsheet → JSON | 2-6 | 3-6 | Yes (SaaS) | SaaS | Non | Light | Standard |
| 7. Hot-reload tweak (Vite HMR) | 4-8 | 5-10 | Yes | In-game | Yes | Light | Strong (workflow) |
| 8. LLM-assisted gen | 4-12 | 3-8 | Yes | Embedded | Yes (gen-once) | Schema-checked | Strong (2026) |

---

## Patterns universels (les plus reproductibles, faisables web JS)

### A. JSON Schema as Single Source of Truth (Pattern 4)
- Stack vanilla JS-friendly (jsoneditor) ou React-based (RJSF, JSON Forms).
- Sépare structure de donnée du code de jeu, autorise tooling multiple sur même schema.
- 100% web, 0 dépendance binaire, 1 fichier `.schema.json` versionable git.
- Probabilité d'adoption succès : haute.

### B. In-Game Live Tweak Panel (Pattern 3)
- Tweakpane ou lil-gui est plug-and-play en quelques heures.
- Zéro infrastructure : c'est juste un script importé.
- Risque très bas, retour rapide.
- Probabilité d'adoption succès : très haute.

### C. Spreadsheet Pipeline (Pattern 6)
- Google Sheets ou Airtable + script Node → JSON ; aucun dev d'éditeur GUI requis.
- Optimal pour balance tabulaire (vue d'ensemble, copier formules).
- Très accessible pour non-devs si jamais collaboration future.
- Probabilité d'adoption succès : haute si workflow accepte SaaS.

---

## Patterns différenciants (signatures uniques)

### D. Hot-Reload Tweak with Save-to-Source (Pattern 7)
- Combine Tweakpane + Vite plugin custom = boucle iterate-commit en secondes.
- Pas de produit packagé existant pour Milan spécifiquement → signature unique du projet.
- Risque technique modéré (Vite plugin custom).
- Probabilité d'adoption succès : moyenne (dépend appétit pour outil custom).

### E. LDtk-style Visual Level Editor for TD (Pattern 2 variante)
- Bâtir un éditeur de levels VIsuels (grid web) avec preview du parcours mob, alternatif au LDtk binary mais Milan-spécifique (3D Three.js, vague compositions).
- Différencie nettement de "Tiled générique" car gère sémantique TD (portals, paths, castles).
- Coût élevé mais ouvre la voie à du community-generated content.

### F. LLM-Augmented Content Authoring (Pattern 8)
- Avec Claude Code déjà partie du workflow, intégrer une route "describe → JSON" est naturel pour Mike.
- Permettrait à Milan d'avancer plus vite sur du contenu (boss, niveaux variés) en gardant validation humaine.
- En 2026, peu de jeux indies utilisent ce pattern → différenciation forte.

---

## Synthèse pour Milan : 5 outils candidats à scorer

NB : descriptif uniquement. Mike priorise lui-même.

### Candidat 1 — Live Balance Panel (in-game)
- **Scope** : Tweakpane intégré dans `?debug=1` exposant TOWER_TYPES + ENEMY_TYPES en folders, sliders typés, bouton "Export JSON" qui DL le state.
- **Cost** : 8-12h.
- **Dependency** : browser only, Tweakpane (~5 KB gz), zero build change.

### Candidat 2 — Data Editor Webapp (standalone)
- **Scope** : page séparée `data-editor.html`, jsoneditor + schema validation, charge/save fichiers JSON towers/enemies/levels, tabs (Towers, Enemies, Waves, Levels), preview minimal.
- **Cost** : 24-40h.
- **Dependency** : standalone webapp (même build Vite), JSON Schema files, optional Vite plugin pour read/write `data/` files.

### Candidat 3 — Visual Level Editor (web, TD-aware)
- **Scope** : éditeur grid 5×12, palette tiles, validation paths multi-portal, wave composition GUI, preview parcours animée, export JSON conforme `data/levels/world*.js`.
- **Cost** : 40-80h.
- **Dependency** : standalone webapp, parser ASCII↔JSON existant `scripts/validate-maps.mjs` à étendre, peut s'inspirer de LDtk ou de Phaser Editor (visuel grid).

### Candidat 4 — Spreadsheet Pipeline (balance.csv)
- **Scope** : 1 Google Sheet "Milan Balance" (3 tabs Towers/Enemies/Waves), Apps Script export JSON, `npm run pull:balance` fetch + write `data/`, vite reload.
- **Cost** : 6-10h.
- **Dependency** : node script + Google API key, Google Sheets account, vite dev hook.

### Candidat 5 — AI Spec Generator (Claude-powered)
- **Scope** : page `ai-author.html`, input texte naturel ("génère un mob support qui heal les voisins"), call Claude API avec schema enforced, render JSON proposé dans GUI éditable (réutilise candidat 2), insert into data folder.
- **Cost** : 12-20h.
- **Dependency** : standalone webapp, Anthropic API key (cost variable), JSON Schema files (candidat 2 dépendance).

---

## Sources principales

- Unity : [ScriptableObject](https://docs.unity3d.com/6000.4/Documentation/Manual/class-ScriptableObject.html) | [Data-Driven Design](https://uhiyama-lab.com/en/notes/unity/unity-scriptableobject-data-driven-design/) | [Odin Validation](https://odininspector.com/tutorials/odin-validator/validators-vs-validation-rules)
- Level editors : [LDtk](https://ldtk.io/docs/game-dev/loading/) | [Tiled](https://www.mapeditor.org/) | [Phaser Editor 2D](https://github.com/PhaserEditor2D/PhaserEditor2D-v3) | [Inspired Python TD](https://www.inspiredpython.com/course/create-tower-defense-game/tower-defense-game-tile-engine-map-editor)
- Live tweak : [Tweakpane](https://tweakpane.github.io/docs/) | [lil-gui via Tweakpane repo](https://github.com/cocopon/tweakpane) | [pmndrs/leva](https://github.com/pmndrs/leva)
- JSON Schema : [react-jsonschema-form](https://rjsf-team.github.io/react-jsonschema-form/) | [JSON Forms](https://jsonforms.io/) | [ginkgobioworks Form Builder](https://github.com/ginkgobioworks/react-json-schema-form-builder)
- Dashboards : [TailAdmin React 2026](https://tailadmin.com/blog/react-admin-dashboard) | [Vue Element Admin](https://github.com/PanJiaChen/vue-element-admin)
- Vite + spreadsheet : [Vite HMR API](https://vite.dev/guide/api-hmr) | [handleHotUpdate](https://vite.dev/changes/hotupdate-hook) | [Coupler Airtable→JSON](https://blog.coupler.io/airtable-to-json/)
- AI gen : [Game Generation via LLM](https://arxiv.org/abs/2404.08706) | [GameUIAgent CHI 2026](https://dl.acm.org/doi/10.1145/3772318.3790326) | [CodeMag LLM video game](https://www.codemag.com/Article/2411061/Can-an-LLM-Make-a-Video-Game)
- Directory : [Web Game Dev Tools](https://www.webgamedev.com/engines-libraries/dev-tools)
