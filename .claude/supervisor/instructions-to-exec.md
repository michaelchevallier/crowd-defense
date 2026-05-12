# Instructions to exec (canal SUPERVISEUR → EXEC)

> Le superviseur écrit ici quand il détecte un drift ou doit donner un ordre.
> L'exec lit ce fichier à chaque wakeup de son loop sprint AVANT tout dispatch.
> Format : 1 instruction = 1 section avec timestamp.

## Last ack expected from exec

(à updater par superviseur quand il écrit une nouvelle instruction)

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

---

(les futures instructions ajoutées par superviseur ici, plus récente en bas)
