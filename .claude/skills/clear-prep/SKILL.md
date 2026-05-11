---
name: clear-prep
description: Run a status check and prepare the codebase for /clear when Mike says he is about to clear, mentions clearing context, ending the session, starting fresh, or asks "ready to clear?". Triggers on French and English phrasings — "je vais clear", "avant de clear", "/clear safe ?", "on peut clear ?", "ready to clear", "before I clear", "clearing soon", "fresh session", "before /clear". Verifies STATUS.md is current vs the master plan, surfaces any gaps the next Opus session needs to know about, and outputs the exact prompt Mike should paste after /clear.
version: 1.0.0
---

# Clear-prep — Status check & /clear-safety verification

## Quand ce skill se déclenche

Mike a indiqué qu'il s'apprête à faire `/clear` (effacer le contexte conversation pour repartir frais). Cette opération est **destructive en mémoire** : tout le contexte conversation est perdu. Seuls les **fichiers committés** survivent.

Ce skill doit **garantir que la prochaine session Opus peut reprendre le travail sans friction**, en vérifiant que tous les fichiers de suivi sont à jour avec le contexte courant.

## Source of truth

Status tracker multi-session : `STATUS.md` à la racine du projet `/Users/mike/Work/crowd-defense` (Unity 6 LTS migration). Le master plan historique Phaser (`/Users/mike/.claude/plans/rustling-nibbling-wirth.md`) est une archive — ne plus l'utiliser comme source pour la migration Unity.

Plans formels par sprint à créer via `/plan` + ExitPlanMode (ex: Phase 1 POC).

## Procédure obligatoire

### Étape 1 — Lire le STATUS.md actuel

```
Read /Users/mike/Work/crowd-defense/STATUS.md
```

Identifier la section "Where we are" et la section "Instructions PROCHAINE SESSION OPUS". Noter le sprint courant + next milestone.

### Étape 2 — Comparer STATUS.md vs réalité conversation courante

Examiner la conversation récente (~50-100 derniers tours) et identifier **toute information acquise pendant cette session qui n'est PAS dans STATUS.md** :

- **Décisions Mike** prises en chat (binaires arbitrées, préférences exprimées, choix de design).
- **Findings d'agents** reçus en notification (livrables, key insights, decision points soulevés).
- **Sprint-gates** runnés (résultats hard/soft assertions).
- **Commits** récents qui changent l'état (vérifier `git log --oneline -10`).
- **Nouvelles demandes Mike** non encore intégrées au plan (ex: nouveau sprint, nouvelle feature, scope changes).
- **Risks discovered** (perf bottleneck, blocked tickets, API limitations).

### Étape 3 — Update STATUS.md + plan si gaps détectés

Pour chaque gap identifié :

1. **Décisions Mike non consignées** → ajouter à STATUS.md "Decisions arbitrées Mike".
2. **Findings agents non consignés** → ajouter à `.claude/status/sprint-XX.md` correspondant.
3. **Nouvelles demandes Mike** → mettre à jour `STATUS.md` section appropriée (Phase plan, Open TODOs) + créer briefing dans `.claude/specs/MIGRATE-*.md` si applicable.
4. **Sprint-gates resultats** → archiver dans `.claude/qa/reports/sprint-XX-YYYY-MM-DD.md`.

Commit + push les updates **avant** que Mike clear.

### Étape 4 — Vérifier "Instructions PROCHAINE SESSION OPUS"

Section critique de STATUS.md. Doit contenir :

- Quoi lire en premier (STATUS.md, master plan, sprint files concernés).
- Quoi faire immédiatement (lancer agents ? attendre Mike ? run sprint-gate ?).
- Comment extraire les briefings (blocs ``` à copier verbatim).
- Quel `subagent_type` utiliser.
- Process validation Mike (au fil de l'eau, batch, etc.).
- Que faire après les agents (sprint-gates, decision trees, next sprint).
- Conditional flows si décisions critiques en attente (ex: pivot Unity post-R3-02).

**Si une de ces 7 sections manque ou est obsolète** → la mettre à jour avant /clear.

### Étape 5 — Briefings agents prêts ?

Pour chaque agent qui doit être lancé par la prochaine session Opus :

- Le briefing est-il dans un fichier `.claude/status/sprint-XX.md` (section "Briefings X préparés") ?
- Est-il dans un bloc de code ``` (triple backticks) extractable verbatim ?
- Mentionne-t-il le `subagent_type` à utiliser ?
- Référence-t-il les décisions Mike actuelles ?
- A-t-il un "Rendu final" attendu clair pour la validation ?

Si pas prêt → préparer le briefing manquant.

### Étape 6 — Check git clean

```
git status --short
git log --oneline -5
```

Aucun fichier modifié non committé. Le dernier commit doit refléter le plan le plus à jour.

### Étape 7 — Output au user

Rapport bref (≤ 300 mots) avec :

1. ✅ ou ⚠️ : STATUS.md à jour ? Briefings prêts ? Git clean ?
2. **Gaps comblés** (liste des updates faits pendant le skill).
3. **Gaps non comblés** (si majeurs : escalade à Mike avant /clear).
4. **Prompt exact à copier après /clear** :

   ```
   Reprends la migration Crowd Defense Unity. Lis dans cet ordre :
   1. .claude/status/STATUS.md (section "Instructions PROCHAINE SESSION OPUS")
   2. .claude/status/sprint-<SPRINT_COURANT>.md (briefings agents à lancer)
   3. .claude/status/sprint-<SPRINT_PARALLELE_SI_APPLICABLE>.md

   Puis exécute les actions immédiates listées dans "Instructions PROCHAINE SESSION OPUS".
   ```

   (Adapter `<SPRINT_COURANT>` au sprint réel en cours.)

5. **Confirmation explicite** : "Tu peux `/clear` en sécurité" OU "Stop, X à régler avant /clear".

## Hard rules

- **Ne JAMAIS dire à Mike qu'il peut clear si STATUS.md est incomplet**. Mieux vaut bloquer le clear et update.
- **Ne JAMAIS supposer qu'un fichier sur disque reflète la conversation courante**. Toujours vérifier.
- Si des **agents tournent en background**, les notifications seront perdues au /clear. **Avertir Mike** et lui demander : attendre les notifs OU clear maintenant et accepter perte.
- Si des **questions ouvertes critiques** n'ont pas de réponse Mike → **poser les questions avant /clear** (sinon décision floue archivée).
- Output final **toujours en français** (Mike préfère).

## Format du rapport final

```markdown
## ✅ État /clear-safety — <DATE>

**Sprint courant** : <X>
**Next milestone** : <Y>

### Updates faits dans ce skill
- ...

### Gaps non comblés (à régler avant /clear)
- ...

### Prompt à coller après /clear

\`\`\`
<prompt exact>
\`\`\`

### Verdict
✅ Tu peux /clear en sécurité.
OU
⚠️ Stop, X à régler avant /clear.
```
