# Active sprint

> Mis à jour par Mike en chat ou par superviseur quand sprint change.
> L'exec lit ce fichier pour savoir quel scope est sealed.

## 🛑 PIVOT V4 PARITY 14h48 (Mike chat direct)

Stratégie réorientée par Mike — priorité absolue = **parité V4 le plus vite possible**.

Mike's clarifications (chat 14h47-14h49) :
- V6 a "pas la moitié" de ce que V4 a → c'est ÇA le vrai drift.
- "Plus joli / propre / déco en +" sur V6 = OK (pas un problème à corriger).
- **Look & feel V4 >> V6** : textures, niveau de finition très inférieur côté V6.
- Bloat LOC : pas urgent, accept temporary.

## Current

- **Sprint** : **R6-PARITY-V4 ACTIVE 15h30** (Mike GO autonome 4h cap fin ~19h30)
- **Mode** : autonome dispatch P0 1-5 (5 tickets) + addendum Unity capabilities
- **Status** : 5 décisions Mike validées + addendum scope ajouté ; instruction PARITY-V4-GO publiée
- **Next action** : exec ack + dispatch batch P0-A (4 worktrees parallèles)
- **Audit source** : `.claude/audit/2026-05-12-v4-parity-gap.md` (V6 = 55-65% gameplay / 40-45% look&feel V4)

## R6-02 paused — historique partiel

- 10 commits DELETE faits (gardés, perte visuelle mineure acceptée)
- 14 tickets DELETE restants FROZEN
- LOC delta partial : -1413 LOC sur god classes (Hero -738, Castle -552, Enemy -106, Tower -17)
- Ne pas revert les 10 deletes (perte mineure pas worth revert effort)

## R6-PARITY-V4 — scope préparatoire

Audit attendu (cf PIVOT instruction) :
1. **Features V4** inventaire exhaustif depuis `milan project/src-v3/entities/*.js`, `systems/*.js`, `data/*.js`, `data/levels/world*.js`
2. **Features V6** inventaire depuis `Assets/Scripts/Entities/*.cs`, `Systems/*.cs`, `Data/*.cs`
3. **Diff** V4 → V6 : PRESENT / PARTIAL / MISSING / INVENTED-V6-ONLY
4. **Look & feel audit** : textures (V4 pipeline Flux Schnell → où sont-elles dans V6 ?), shaders, lighting, post-processing, materials, niveau finition général
5. Output : `.claude/audit/2026-05-12-v4-parity-gap.md` (~50-80 rows + section look&feel)

Backlog R6-PARITY-V4 (à construire post-audit + Mike validation) :
- 1 ticket par feature MISSING/PARTIAL (priorisé high gameplay impact d'abord)
- 1 ticket par axe look & feel (textures port, lighting setup, post-processing, etc.)
- Tickets Q1-Q18 implem fusionnés dans parity si Q-N correspond à feature V4

## Mode change conditions (post-audit)

Mike validera mode dispatch R6-PARITY-V4 :
- Supervisé batch (Mike valide chaque 3-4 tickets)
- Autonome time cap N heures
- Manuel ticket par ticket

## Sprints reportés / prioritisation revue

- R6-03 REFACTO god classes : **PARKED** (pas urgent vs parity)
- R6-04 Q1-Q18 implementation : **FUSIONNÉ** dans R6-PARITY-V4 (Q-N = V4 features)
- R6-05 V4 parity gap : **renommé R6-PARITY-V4, priorité ABSOLUE**
- R6-06 POLISH : reste low prio, garder pour fin

## R6-01 historique (DONE)

- Track A : ✅ `.claude/audit/2026-05-12-triage-table.md` 56 rows + 3 decisions Mike validées (commit `d87125c`)
- Track B : ✅ bug-fixer 3 commits (`08778de` shaders URP, `7d3b1af` NullRef MinimapController, `1f93e3c` ArgumentNull TowerInfoPanel) + gh-pages deploy `50c5420`

## Hard prohibitions actives

- Aucun ScheduleWakeup hors sprint autorisé Mike
- Aucun Sub-Opus spawn
- Aucun feature creep "en passant"
- Aucun DELETE supplémentaire (R6-02 frozen)
- Aucun revert des 10 DELETE déjà committed

## History sprints

- R6-01 : 2026-05-12 14h00 → DONE 14h22
- R6-02 DELETE : 14h22 → PAUSED 14h48 (10/24 tickets done, pivot V4 parity)
- R6-PARITY-V4 : en préparation, audit en cours
- R6-03 REFACTO god classes : PARKED
- R6-04 Q1-Q18 implementation : FUSIONNÉ dans R6-PARITY-V4
- R6-06 POLISH : pending fin de projet
