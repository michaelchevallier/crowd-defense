# Models — V5 Asset Manifest

Inventaire complet des modèles 3D importés depuis le proto V5 (`milan project/src-v3/assets/`) vers Unity 6 LTS. Tous les packs sont **CC0** (domaine public). Voir les `License.txt` / `License_Standard.txt` dans chaque sous-dossier pour les détails et attributions facultatives.

**Total** : 832 fichiers GLTF/GLB, ~360 MB. Aucun fichier > 100 MB (pas de git LFS requis).

## Répartition par dossier

| Dossier | Pack source | Fichiers | Taille |
|---|---|---|---|
| `Heroes/KayKit/` | KayKit Adventurers 2.0 FREE | 39 | 6.1 MB |
| `Heroes/Stylized/` | GLBs root V5 (auteurs divers, CC0) | 4 | 1.8 MB |
| `Heroes/Quaternius/UltimateAnimatedCharacters/` | Quaternius Ultimate Animated Character Pack | 52 | 99 MB |
| `Props/KayKitDungeon/` | KayKit Dungeon Remastered 1.1 FREE | 211 | 7.2 MB |
| `Environment/Quaternius/UltimateFantasyRTS/` | Quaternius Ultimate Fantasy RTS | 128 | 37 MB |
| `Environment/Quaternius/MedievalVillageMegaKit/` | Quaternius Medieval Village MegaKit | 176 | 58 MB |
| `Environment/Quaternius/StylizedNatureMegaKit/` | Quaternius Stylized Nature MegaKit | 68 | 48 MB |
| `Environment/Quaternius/FantasyPropsMegaKit/` | Quaternius Fantasy Props MegaKit | 94 | 42 MB |
| `Towers/` (pré-existant) | Mixed legacy V5 | 18 | 3.5 MB |
| `Enemies/` (pré-existant) | Mixed legacy V5 | 42 | 58 MB |

## Catégorisation par usage

### Heroes (joueur — branches L3, 4 tours signature)

**`Heroes/KayKit/Characters/`** — 6 chars rigged (skeletal, partagent `Rig_Medium_*` dans `Animations/`) :
- `Barbarian.glb`, `Knight.glb`, `Mage.glb`, `Ranger.glb`, `Rogue.glb`, `Rogue_Hooded.glb`

**`Heroes/KayKit/Equipment/`** — 31 props équipables (armes, boucliers, accessoires) :
- Armes : `sword_1handed`, `sword_2handed[_color]`, `axe_1handed`, `axe_2handed`, `dagger`, `staff`, `wand`, `bow[_withString]`, `crossbow_1handed`, `crossbow_2handed`
- Munitions : `arrow_bow[_bundle]`, `arrow_crossbow[_bundle]`, `quiver`
- Boucliers : `shield_badge[_color]`, `shield_round[_color]`, `shield_round_barbarian`, `shield_square[_color]`, `shield_spikes[_color]`
- Divers : `spellbook_closed`, `spellbook_open`, `smokebomb`, `mug_empty`, `mug_full`

**`Heroes/KayKit/Animations/`** — `Rig_Medium_General.glb`, `Rig_Medium_MovementBasic.glb` (skeletal animations, à attacher aux 6 chars KayKit via Animator Override Controller).

**`Heroes/Stylized/`** — 4 GLBs root (dragon + 3 chars stylisés alternatifs) :
- `dragon_low_poly_animated.glb`, `low_poly_stylized_pirate_character.glb`, `low-poly_stylized_archer.glb`, `stylized_low_poly_mage__game_character.glb`

**`Heroes/Quaternius/UltimateAnimatedCharacters/`** — 52 personnages animés (Mixamo-style rigs) couvrant tous les archétypes possibles :
- Médiéval/Fantasy : `Knight_Male`, `Knight_Golden_Male/Female`, `Viking_Male/Female`, `VikingHelmet`, `Wizard`, `Witch`, `Elf`, `Goblin_Male/Female`, `Soldier_Male/Female`, `BlueSoldier_Male/Female`
- Casual/Modern : `Casual_Male/Female/Bald`, `Casual2_Male/Female`, `Casual3_Male/Female`, `Suit_Male/Female`, `Worker_Male/Female`, `Chef_Male/Female/Hat`, `Doctor_Male/Female_Young/Old`, `OldClassy_Male/Female`
- Asiatique : `Kimono_Male/Female`, `Ninja_Male[_Hair]/Female`, `Ninja_Sand[_Female]`
- Autres : `Cowboy_Male/Female/Hair`, `Pirate_Male/Female`, `Zombie_Male/Female`, `BaseCharacter`, `Cow`, `Pug`

### Enemies (vagues — Ultimate Animated Character Pack + Enemies/ legacy)

Source primaire pour AssetRegistry.Enemies : `Heroes/Quaternius/UltimateAnimatedCharacters/` (chars rigs animables = mobs swarmables).

**Mobs prioritaires** :
- Standard mobs : `Zombie_Male/Female`, `Goblin_Male/Female`, `BaseCharacter` (variants)
- Caster / élite : `Wizard`, `Witch`, `Ninja_Male/Female`
- Boss : `Knight_Golden_Male/Female` (mid-boss), `dragon_low_poly_animated.glb` (final boss)
- Filler : `Skeleton`, `Pirate_Male/Female`, `Cow` (cheptel), `Pug` (mobs comiques)

Voir aussi `Enemies/` legacy (pré-existant, 42 mobs déjà placés et nommés `mob_*.gltf` avec `Bosses/` subfolder).

### Towers

Source primaire : `Towers/` legacy (déjà mappé avec naming convention `tower_<role>[_l2|_l3].gltf|glb`).

**Ressource alternative** : `Environment/Quaternius/UltimateFantasyRTS/` contient des **tours réelles** + bâtiments fantasy :
- Tours archers : `Archery_FirstAge_Level1/2/3.gltf`, `Archery_SecondAge_Level1/2/3.gltf`
- Tours de garde : `WatchTower_FirstAge_Level1/2/3.gltf`, `WatchTower_SecondAge_Level1/2/3.gltf`
- Tours murailles : `WallTowers_FirstAge.gltf`, `WallTowers_SecondAge.gltf`, `WallTowers_Door[Closed]_*`
- Casernes : `Barracks_FirstAge_Level1/2/3.gltf`, `Barracks_SecondAge_Level1/2/3.gltf`
- Tour-maison : `TowerHouse_FirstAge.gltf`, `TowerHouse_SecondAge.gltf`

À mapper sur les 12 tower types via AssetRegistry quand on remplacera les `Towers/tower_*.gltf` legacy.

### Props (décor jouable, intérieur dungeon)

**`Props/KayKitDungeon/Assets/`** — 211 props dungeon haute densité, organisés par famille :
- Bannières : `banner_<solid|patternA|B|C|shield|thin|triple>_<blue|brown|green|red|white|yellow>` (~50 variants)
- Mobilier : `chair*`, `table_*`, `bench*`, `bed_*`, `cabinet*`, `bookcase*`, `dummy*`
- Stockage : `chest_*` (gold, wooden), `barrel*`, `crate*`, `coin*`
- Éclairage : `candle*`, `lantern*`, `torch*`, `chandelier*`
- Liquides : `bottle_*`, `pot_*`, `potion_*`, `mug*`, `cauldron`
- Décor mural : `pillar*`, `wall_*`, `floor_*`, `stairs*`, `door*`
- Misc : `coin`, `book*`, `scroll*`, `key*`, `weapon*`

### Environment (extérieur, paysage, village)

**`Environment/Quaternius/MedievalVillageMegaKit/`** — 176 modules construction médiéval :
- Modules muraux/sols : `Floor_*`, `Wall_*`, `Corner_*`, `Door_*`, `Roof_*`
- Toitures : `Overhang_*`, `Prop_Chimney*`, `RoofIncline_*`
- Détails extérieur : `Balcony_*`, `Prop_Vine*`, `Prop_Brick*`, `Prop_MetalFence_*`, `Prop_Support`

**`Environment/Quaternius/StylizedNatureMegaKit/`** — 68 props nature (terrain, végétation) :
- Arbres : `CommonTree_1..5`, `DeadTree_1..5`, `Pine_1..5`, `TwistedTree_1..5`
- Plantes : `Bush_Common[_Flowers]`, `Clover_1/2`, `Fern_1`, `Flower_3/4_[Single|Group]`, `Grass_Common_Short/Tall`, `Grass_Wispy_Short/Tall`, `Mushroom_*`, `Plant_1/7[_Big]`
- Rochers : `Pebble_Round_1..5`, `Pebble_Square_1..6`, `Rock_Medium_1..3`
- Chemins : `RockPath_Round/Square_{Small_1..3|Thin|Wide}`
- Pétales : `Petal_1..5`

**`Environment/Quaternius/FantasyPropsMegaKit/`** — 94 props fantasy interactifs (loot/décor) :
- Armes/forge : `Anvil[_Log]`, `Sword_Bronze`, `Axe_Bronze`, `Pickaxe_Bronze`, `Workbench[_Drawers]`, `Whetstone`, `WeaponStand`, `Shield_Wooden`, `Dummy`
- Mobilier : `Bed_Twin1/2`, `Bench`, `Chair_1`, `Cabinet`, `Stool`, `Table_Large`, `Bookcase_2`, `BookStand`, `Shelf_*`, `Nightstand_Shelf`, `Peg_Rack`
- Stockage : `Chest_Wood`, `Barrel[_Apples|_Holder]`, `Crate_{Metal|Wooden}`, `FarmCrate_{Apple|Carrot|Empty}`, `Cage_Small`, `Pouch_Large`, `Bag`
- Coin/loot : `Coin`, `Coin_Pile[_2]`, `Key_{Gold|Metal}`, `Chalice`, `Vase_2/4`, `Vase_Rubble_Medium`
- Éclairage : `Candle_1/2`, `CandleStick[_Stand|_Triple]`, `Chandelier`, `Lantern_Wall`, `Torch_Metal`
- Bannières/décor : `Banner_1/2[_Cloth]`, `Rope_1/2/3`, `Chain_Coil`
- Bouquins/scrolls : `Book_5/7`, `Book_Simplified_Single`, `Book_Stack_1/2`, `BookGroup_{Medium|Small}_1/2/3`, `Scroll_1/2`
- Liquides/recettes : `Pot_1[_Lid]`, `Potion_1/2/4`, `Bottle_1`, `SmallBottle[s_1]`, `Mug`, `Cauldron`, `Bucket_{Metal|Wooden_1}`
- Cuisine : `Table_{Fork|Knife|Plate|Spoon}`, `Carrot`
- Trading : `Stall_{Cart_Empty|Empty}`

**`Environment/Quaternius/UltimateFantasyRTS/`** — 128 bâtiments RTS (deux âges : "FirstAge" rural, "SecondAge" urbain) :
- Ressources : `Resource_{Gold_1/2/3|PineTree[_Group[_Cut]]|Rock_1/2/3|Tree_Group[_Cut]|Tree1/2}`
- Montagnes/rochers : `Mountain_{Group_1/2|Single}`, `MountainLarge_Single`, `Rock[_Group]`
- Habitations : `Houses_{FirstAge|SecondAge}_{1|2|3}_{Level1|2|3}`
- Économie : `Farm_{FirstAge|SecondAge}_{Level1|2|3}[_Wheat]`, `Farm_Dirt_{Level1|2|3}`, `Market_{FirstAge|SecondAge}_{Level1|2|3}`, `Storage_{FirstAge|SecondAge}_{Level1|2|3}` (sauf typo `Storage_FirstAge_Leve3.gltf`), `Mine`, `Windmill_{FirstAge|SecondAge}`, `Logs`, `Crate[_Stack1/Stack2|_Big_Stack2]`, `Barrel`
- Religion/wonders : `Temple_{FirstAge|SecondAge}_{Level1|2|3}`, `Wonder_{FirstAge|SecondAge}_{Level1|2|3}`, `WonderWalls_{FirstAge|SecondAge}`
- Centre ville : `TownCenter_{FirstAge|SecondAge}_{Level1|2|3}`, `Port_{FirstAge|SecondAge}_{Level1|2|3}`, `Dock_FirstAge`
- Défense (cf. section Towers) : tours archers, watch towers, walls, barracks, tower houses

## Licenses

Tous les packs sont **CC0 1.0 Universal (Public Domain Dedication)** :
- KayKit (Kay Lousberg, www.kaylousberg.com) → crédit facultatif : "Kay Lousberg, www.kaylousberg.com"
- Quaternius (www.patreon.com/quaternius) → crédit facultatif : "Quaternius"

Fichiers `License.txt` / `License_Standard.txt` préservés dans chaque sous-dossier de pack pour traçabilité.

## Mapping suggéré AssetRegistry

| Slot AssetRegistry | Source recommandée |
|---|---|
| `Towers[]` (12 entrées L1) | `Environment/Quaternius/UltimateFantasyRTS/{Archery,WatchTower,Barracks,TowerHouse}_FirstAge_Level1.gltf` |
| `Towers[]` upgrades L2 | mêmes familles `_Level2` |
| `Towers[]` upgrades L3 | mêmes familles `_Level3` + `Wonder_*` pour signature towers |
| `Heroes[]` (4-6 slots) | `Heroes/KayKit/Characters/{Knight,Mage,Ranger,Rogue,Barbarian,Rogue_Hooded}.glb` |
| `Heroes[].equipment` slots | `Heroes/KayKit/Equipment/*.gltf` |
| `Enemies[]` standard (W1-W10) | `Heroes/Quaternius/UltimateAnimatedCharacters/{Zombie_*,Goblin_*,Soldier_*,BlueSoldier_*}.gltf` |
| `Enemies[]` casters/élite | `Heroes/Quaternius/UltimateAnimatedCharacters/{Wizard,Witch,Ninja_*}.gltf` |
| `Enemies[]` boss | `Heroes/Quaternius/UltimateAnimatedCharacters/Knight_Golden_*.gltf`, `Heroes/Stylized/dragon_low_poly_animated.glb` |
| `Environment[]` map décor | `Environment/Quaternius/StylizedNatureMegaKit/*.gltf` (arbres, rochers, herbe) |
| `Environment[]` village décor | `Environment/Quaternius/MedievalVillageMegaKit/*.gltf` (murs, toits, façades) |
| `Props[]` interior/dungeon | `Props/KayKitDungeon/Assets/*.gltf` + `Environment/Quaternius/FantasyPropsMegaKit/*.gltf` |
