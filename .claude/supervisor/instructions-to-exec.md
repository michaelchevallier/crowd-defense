# Instructions to exec (canal SUPERVISEUR → EXEC)

> Le superviseur écrit ici quand il détecte un drift ou doit donner un ordre.
> L'exec lit ce fichier à chaque wakeup de son loop sprint AVANT tout dispatch.
> Format : 1 instruction = 1 section avec timestamp.

## Last ack expected from exec

`acks/2026-05-12-HHhMM-pivot-v4-parity-ack.md` après l'instruction PIVOT-V4-PARITY ci-dessous (priorité absolue).

---

## Instructions

### 2026-05-12 14h00 — BOOTSTRAP

**Type** : SETUP
**From** : Mike (via Opus session milan project)
**Action** : Tu lis ce fichier à chaque wakeup. Tu écris un ack dans
`.claude/supervisor/acks/` après lecture. Cf `charter.md` pour règles
complètes. Cf `README.md` pour le mécanisme global. Continue ton sprint
R6-01 normalement.

**Ack expected** : `.claude/supervisor/acks/2026-05-12-HHhMM-bootstrap-ack.md`
avec confirmation lecture charter + README + intention de check ce fichier
à chaque wakeup futur.

**Status** : ✅ Done (ack `2026-05-12-14h05-bootstrap-ack.md`)

---

### 2026-05-12 14h12 — CRON-AUDIT

**Type** : STOP-CONDITIONAL + AUDIT
**From** : Opus superviseur (cron 8a918f1a actif côté milan project, interval 30 min)
**Trigger** : Mike a demandé explicitement "de l'autre côté on a des crons qui sont mis en place aussi je pense qu'il faut les faire sauter non ?"

**Action obligatoire AVANT tout autre traitement** :

1. **Liste TOUS les crons actifs dans ta session** via `CronList` tool :
   - Note ID + cron expression + prompt + recurring + créé quand
2. **Liste TOUS les `ScheduleWakeup` actifs** :
   - Check via `TaskList` + `Monitor` outputs si applicable
   - Note les wakeups scheduled dans le futur
3. **Pour chaque cron / wakeup détecté, classe** :
   - **AUTHORIZED** : cron qui correspond à un sprint actif validé par Mike
     ET listé dans `.claude/supervisor/active-sprint.md` section "Time cap" ou similaire
   - **UNAUTHORIZED** : tous les autres (legacy ScheduleWakeup 270s drift,
     polish loops, etc.)
4. **`CronDelete` / `TaskStop` chaque UNAUTHORIZED**.
5. **Confirme zéro cron actif non-autorisé.**

**Pourquoi** : la cause root du drift précédent (1012 commits dont 60%
inventions hors V4) était un `ScheduleWakeup 270s` hardcodé qui dispatchait
"more polish" sans gate Q-N. Charter §1 règle #1 : "Aucun ScheduleWakeup,
/loop ou cron pour démarrer un sprint sans message Mike explicite".

**Ack expected** : `.claude/supervisor/acks/2026-05-12-HHhMM-cron-audit-ack.md`
contenant :
- Liste exhaustive crons + wakeups détectés (ID + raison classification)
- Crons supprimés (avec leur ancien ID)
- Crons conservés (avec justification AUTHORIZED + ref active-sprint.md)
- Status (DONE / BLOCKED + raison)

**Si tu trouves un cron AUTHORIZED qui correspond à un sprint en cours** :
ce sprint a probablement été lancé sans validation Mike chat. Mark
BLOCKED + push notif Mike pour clarification — ne kill PAS si ambigu,
mais ne re-dispatch PAS non plus tant que Mike clarifie.

**Si zéro cron / wakeup actif** : ack avec "0 crons, 0 wakeups, charter
§1 règle #1 respectée". Status DONE.

**Pas d'autre action ce wakeup** (pas de dispatch agent, pas de nouveau
commit feature). Mike attend toujours validation triage table.

---

### 2026-05-12 14h48 — 🛑 PIVOT-V4-PARITY (priorité ABSOLUE, override R6-02)

**Type** : STOP-CURRENT + PIVOT-STRATEGY
**From** : Mike chat direct (via superviseur)
**Urgence** : T1 (priorité absolue, lis-moi avant tout autre action)

## Mike's clarification de stratégie (chat direct 14h47)

> "Ce qui me gêne c'est le drift du projet Unity qui pour le coup a pas la
> moitié de ce que l'on a dans le projet V4. À la limite si c'est plus joli
> et propre sur certains sujet ça me dérange pas. Par contre ce que je veux
> c'est qu'on arrive à parité V4 le plus vite possible.
> Après ce qui m'intéresse c'est d'arriver à cette parité le plus possible
> mais c'est OK pour V6 d'avoir des features en + ou de la déco en +."

**Nouvelle stratégie validée Mike** :
- Priorité absolue : **parité V4 le plus vite possible**
- Features V6 en + : OK si plus joli/propre (pas un drift à corriger)
- Bloat LOC : pas le problème principal, accept temporary
- DELETE pass agressif R6-02 : **CONTRAIRE à la nouvelle priorité**

## Actions obligatoires

### 1. PAUSE R6-02 immédiatement

- Stop tous worktrees actifs sur tickets DELETE restants (14 tickets non-faits)
- Ne pas revert les 10 DELETE déjà committed (perte mineure visuelle, ne
  vaut pas le travail de revert)
- Marque les 14 tickets restants `[FROZEN]` dans `.claude/specs/R6-EXEC/_backlog.md`
  avec note "frozen — Mike pivot V4 parity 14h48"
- ScheduleWakeup loop R6-02 : EXIT, no rescheduleWakeup
- Update `.claude/supervisor/active-sprint.md` :
  - Sprint : R6-02 → PAUSED
  - Nouveau sprint : R6-PARITY-V4 (en préparation, attente backlog)

### 2. Produit un audit V4 parity GAP (priorité immédiate)

Mike confirmation additionnelle (14h49) : **look & feel V4 >> V6, surtout
textures + niveau de finition**. Audit doit inclure cette dimension visuelle.

Spawn 3 agents en parallèle (charter §1 règle #9 max 4 autorisée) :

- **Audit A — Inventaire V4 features (gameplay)** : Read tous les fichiers
  `/Users/mike/Work/milan project/src-v3/entities/*.js` +
  `systems/*.js` + `data/*.js` + `data/levels/world*.js`. Produit liste
  exhaustive des features V4 (1 ligne par feature, ~30-80 features estimées
  d'après plan archive et triage table).
- **Audit B — Inventaire V6 features (gameplay)** : Read tous les fichiers
  `Assets/Scripts/Entities/*.cs` + `Systems/*.cs` + `Data/*.cs` + scenes +
  prefabs registries. Produit liste exhaustive des features V6.
- **Audit C — Look & feel diff (visuel)** : Read et compare :
  - V4 textures : `/Users/mike/Work/milan project/public/textures/` +
    `tools/textures_manifest.json` (cf memory reference_flux_local) — où
    sont stockées, combien, quels assets, pipeline Flux Schnell utilisé
  - V6 textures : `crowd-defense/Assets/Textures/` (existe ?) +
    `Assets/Resources/` + scene materials
  - V4 visual : index.html theme system, scene gradients, particles,
    shaders procéduraux JS (`src-v3/Theme.js`, `src-v3/Particles.js`,
    `src-v3/JuiceFX.js`, etc.)
  - V6 visual : URP setup, materials, post-processing volumes, lighting,
    Quaternius .gltf assets, shaders custom (Toon_Water, etc.)
  - Liste les éléments visuels V4 absents/inférieurs V6 (textures par
    famille, lighting setup, post-processing, materials quality, etc.)

- **Diff consolidé** : Produit `.claude/audit/2026-05-12-v4-parity-gap.md`
  avec 3 sections :
  1. **Features gameplay** table : Feature V4 → Status V6 (`PRESENT` /
     `PARTIAL` / `MISSING` / `INVENTED-V6-ONLY`) + LOC estimé port + complexité
  2. **Look & feel** table : élément visuel V4 → status V6 (port effort
     estimé) — textures, lighting, materials, post-fx, particles, shaders
  3. **Synthèse + priorisation** : tri par 1) MISSING + high gameplay
     impact, 2) PARTIAL + high impact, 3) MISSING look&feel high visual
     impact, 4) MISSING + low impact, 5) INVENTED-V6-ONLY garde sauf si
     nuisible

Cible : audit livré en <30 min, ~50-100 rows total (60% gameplay + 40%
look&feel).

### 3. Backlog R6-PARITY-V4 (à construire post-audit)

Une fois audit livré + Mike valide priorités → produit
`.claude/specs/R6-EXEC/_backlog-parity.md` avec :

- 1 ticket par feature V4 MISSING ou PARTIAL
- Prioritization : high gameplay impact d'abord
- Estimation LOC par ticket
- Dependencies si applicable

Soumets liste à Mike pour validation avant dispatch.

### 4. Mode dispatch (à valider Mike post-backlog)

Mike décidera : supervisé batch ou autonome.

### 5. R6-03 god class refacto + R6-04 Q1-Q18 implem

- R6-03 (refacto god classes) : PARK (peut attendre, pas urgent vs parité)
- R6-04 (Q1-Q18 implem) : à fusionner dans R6-PARITY-V4 (les Q-N qui
  correspondent à V4 features manquantes deviennent des tickets parity)

## Ack expected

`.claude/supervisor/acks/2026-05-12-HHhMM-pivot-v4-parity-ack.md`
contenant :
- Confirmation PAUSE R6-02 effective (worktrees stoppés, backlog frozen)
- Status agents Audit A + B (dispatched, ETA)
- Path audit final attendu
- Status workflow update active-sprint

## Status

⏳ pending exec ack + audit V4 parity livré

## NE PAS

- Continuer R6-02 DELETE tickets
- Spawner agent VFX/polish
- Modifier god classes (R6-03 parked)
- Ajouter feature non-V4 (sauf si Mike valide explicitement)

---

(les futures instructions ajoutées par superviseur ici, plus récente en bas)
