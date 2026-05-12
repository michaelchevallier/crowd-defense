# SFX Completeness Audit — post-N2 (commit d6f150f)

Date: 2026-05-13. Read-only. Counts: **54 unique keys called in code**, **76 entries in registry**, **0 missing**, **~28 wrong mappings (semantic)**, **17 unused entries** (incl. 11 music tracks).

## Source of Clips (20 .ogg/.mp3 SFX + 11 music)

`Assets/Audio/SFX/`: achievement, blue_pill, boom, boss_charge, castle_hit, coin_pickup, enemy_die_basic, enemy_die_boss, enemy_die_medium, enemy_hit, gem_gain, hero_shoot, level_up, perk_pick, skin_equip, tower_built, tower_shoot, tower_upgrade, wave_clear, wave_start.

## Table — key -> in_code / in_registry / clip / match (0-3)

| Key | code | reg | clip mapped | match |
|---|---|---|---|---|
| achievement / achievement_chime / achievement_unlock | y | y | achievement.ogg | 3 |
| set_bonus | y | y | achievement.ogg | 2 acceptable |
| toast_generic/achievement/perk/synergy/combo/modifier/success | y | y | achievement.ogg | **1 generic ding for 7 distinct types** |
| toast_warning / toast_error / overrun_alert / warning_sound / castle_lost | y | y | castle_hit.ogg | **1 thud as alert** |
| toast_info / cutscene_start / path_reveal / wave_start | y | y | wave_start.ogg | 2 ok, but path_reveal/cutscene_start are off |
| blue_pill | y | y | blue_pill.ogg | 3 |
| cancel / menu_button_hover / menu_open_woosh / place_invalid / tower_select_click / tower_deselect_click / tower_sold / tutorial_progress | y | y | boom.ogg | **0 explosion mapped to clicks/UI/sell — DING dong** |
| boss_roar / boss_death_roar / boss_defeated / boss_kill_special / boss_spawn_drone | y | y | boss_charge.ogg | 2 (charge wind-up reused for death/spawn) |
| castle_hit | y | y | castle_hit.ogg | 3 |
| chest_open / coin_pickup / gold_earn / xp_pickup | y | y | coin_pickup.ogg | 3 / 3 / 2 / **1 xp = coin** |
| combo_up / hero_levelup / level_up / powerup / research_ding / star / tutorial_complete | y | y | level_up.ogg | 2 (one clip 7 keys) |
| enemy_die_basic | y | y | enemy_die_basic.ogg | 3 |
| enemy_die_medium | y | y | enemy_die_medium.ogg | 3 |
| enemy_die_boss / hero_death | y | y | enemy_die_boss.ogg | 3 / **1 hero=enemy_boss** |
| enemy_hit | y | y | enemy_hit.ogg | 3 |
| hero_shoot / hero_ult / step_dirt | y | y | hero_shoot.ogg | 3 / **1 ult=shoot** / **0 footstep=gunshot** |
| perk_pick | y | y | perk_pick.ogg | 3 |
| place_tower / tower_placed | y | y | tower_built.ogg | 3 |
| tower_upgrade | y | y | tower_upgrade.ogg | 3 |
| wave_clear | y | y | wave_clear.ogg | 3 |

## Unused registry entries (17)

SFX present but never called: **boom**, **boss_charge**, **gem_gain**, **skin_equip**, **tower_built** (code uses `place_tower`/`tower_placed`), **tower_shoot** (code uses Hero.cs `hero_shoot` only). Music: w1, w1_plaine, w2-w10 (11 entries — used via WeatherController.PlayLoop with dynamic clip ref, not via key — leave).

## Top 10 priority fixes

1. **step_dirt** mapped to `hero_shoot.ogg` (gunshot for every enemy step!) — need real footstep recording. Critical: spawned 100+/wave.
2. **cancel / place_invalid / tower_sold** mapped to `boom.ogg` (explosion) — needs proper UI click/sell jingle. Use existing `skin_equip` or generate.
3. **menu_button_hover / menu_open_woosh / tower_select_click / tower_deselect_click / tutorial_progress** mapped to `boom.ogg` — needs subtle UI click. Same fix as #2.
4. **hero_death** mapped to `enemy_die_boss.ogg` — needs distinct hero death cry.
5. **hero_ult** mapped to `hero_shoot.ogg` — needs whoosh/impact distinct from basic shot.
6. **xp_pickup** mapped to `coin_pickup.ogg` — needs higher-pitch sparkle, can reuse `gem_gain.ogg` (unused!).
7. **toast_warning / toast_error / castle_lost / overrun_alert / warning_sound** all `castle_hit.ogg` — needs alert siren distinct from castle-getting-hit feedback.
8. **toast_generic/achievement/perk/synergy/combo/modifier/success** all `achievement.ogg` — at least split achievement vs. generic toast.
9. **cutscene_start / path_reveal** mapped to `wave_start.ogg` — needs cinematic stinger.
10. **boss_*** family (5 keys -> `boss_charge.ogg`) — split death vs roar vs spawn.

## Reco

- **Quick wins (no new recording)**: remap `xp_pickup` -> `gem_gain.ogg`, `tower_sold` -> `tower_upgrade.ogg`, `tower_built` key can stay unused (`place_tower` already uses same clip). Frees 1 slot.
- **Placeholder OK** (procedural beep): `cancel`, `menu_*`, `tower_*_click`, `tutorial_progress` — keep `boom` mapping but add #if UNITY_EDITOR warning recommending real recording later.
- **Real recordings needed** (P1): `step_dirt`, `hero_death`, `hero_ult`, alert siren, cinematic stinger, boss death roar. 6 clips ~10s each.
- No missing keys means no code change needed; pure asset work.
