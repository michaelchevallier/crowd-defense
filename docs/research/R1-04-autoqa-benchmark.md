# R1-04 — Auto-QA AI/LLM pour jeux : benchmark industrie

**Sprint** : R1 (recherche industrie pure)
**Auteur** : `td-researcher` instance "auto-QA AI"
**Date** : 2026-05-11
**Scope** : 7 patterns industrie pour automatiser le QA de jeux/apps interactives via AI/LLM. Aucune proposition de solution Milan — description seulement.

---

## Index des patterns étudiés

| # | Pattern | Domaine | Maturité | Coût indicatif |
|---|---------|---------|----------|----------------|
| 1 | Anthropic Claude Computer Use | Vision + agent autonome | GA depuis Q1 2026 | $15/MTok output (Opus) |
| 2 | Playwright MCP (accessibility tree) | Scripted + LLM-driven | Stable depuis mars 2025 | $0.02-0.08 par test (Haiku) |
| 3 | Chrome DevTools MCP | Browser automation + perf | Public preview sept 2025 | dépend du modèle client |
| 4 | LLM-as-judge (G-Eval, DeepEval, Langfuse) | Soft assertions UX | Industrie depuis 2024 | $9.5K/évaluation lourde, $0.01-0.10/scoring léger |
| 5 | DeepMind SIMA / Voyager | Agent généraliste game-playing | Recherche, pas prod QA | Inaccessible (compute lourd) |
| 6 | EA SEED / Ubisoft Client Bots / DICE AutoPlayers | RL bots production | En prod AAA depuis 2018-2022 | Infra GPU dédiée |
| 7 | Anthropic Routines / Claude Code scheduled tasks | Orchestration récurrente | GA avril 2026 | inclus pricing Claude Code |

---

## 1. Anthropic Claude Computer Use

### 1.1 Description

Annoncé octobre 2024 avec Claude 3.5 Sonnet, GA et stabilisé fin 2025 / début 2026. Pattern : un modèle Claude (Opus 4.5/4.6/4.7 ou Sonnet 4.6) reçoit screenshots d'écran et émet des actions souris/clavier via une API tool spéciale (`computer_20251124`). Inclut un `enable_zoom` permettant inspection région à pleine résolution depuis fin 2025. Anthropic le positionne comme "general computer skills" plutôt que tool spécialisé. ([Anthropic intro](https://www.anthropic.com/news/3-5-models-and-computer-use), [Docs computer use tool](https://platform.claude.com/docs/en/agents-and-tools/tool-use/computer-use-tool), [CLI integration](https://code.claude.com/docs/en/computer-use))

### 1.2 Use case typique gaming

Documenté côté Anthropic comme "Claude can interact with applications the way a QA tester would". Cas observés en blog tiers : automated testing de UI, data collection, exception handling pour des automations qui casseraient autrement. Pour les jeux, peu de cas publics frontaux mais le pattern correspond exactement à : "lance le jeu en browser, joue une série d'actions, vérifie l'écran final via screenshot + raisonnement".

### 1.3 Outil/SDK

Tool `computer_20251124` exposé via Claude API. Claude émet `{action: "screenshot"}`, `{action: "left_click", coordinate: [x,y]}`, `{action: "key", text: "ctrl+s"}`, etc. Le client (toi, dans une boucle agent) exécute l'action sur l'OS hôte (Linux VM recommandée par Anthropic) et retourne le screenshot suivant.

### 1.4 Hard vs soft assertions

- **Hard** : possible via OCR ou `evaluate_script` si le client expose une eval JS (souvent fait via combo Computer Use + Chrome DevTools MCP). Lecture pixels d'un compteur, présence d'un sélecteur DOM.
- **Soft** : très fort sur le visuel ("est-ce que la HUD est lisible ?", "le bouton est-il visible ?"). Claude raisonne en langage naturel sur le screenshot.

### 1.5 Coût

Pricing Opus 4.7 : $5 input / $25 output par MTok (1M context inclus, [Anthropic pricing](https://platform.claude.com/docs/en/about-claude/pricing)). Sonnet 4.6 : $3/$15. Chaque tour = 1 screenshot (~1500 tokens vision input) + reasoning (~200-500 output). Une session de 50 actions ≈ 80K tokens input + 25K output ≈ $1-3 en Opus, $0.30 en Sonnet. Ajouter 35% pour le nouveau tokenizer Opus 4.7 ([Finout report](https://www.finout.io/blog/claude-opus-4.7-pricing-the-real-cost-story-behind-the-unchanged-price-tag)).

### 1.6 Limites

- Latence par action ~3-8s (screenshot + LLM round-trip).
- Flakiness sur sélecteurs visuels : coordonnées pixel changent avec resize, rendu canvas (Three.js !) cache le DOM.
- Long context : sessions > 100 actions saturent le budget tokens même avec 1M context.
- Sécurité : Anthropic recommande sandbox VM, pas le desktop hôte du dev.

### 1.7 Applicabilité à Milan CD V3

Milan CD V3 est un canvas Three.js, donc l'accessibility tree DOM est quasi vide (toolbar HTML mais pas le board). Claude Computer Use ferait du raisonnement sur screenshots — adapté pour soft assertions ("le bouton 'Lancer la vague' est-il bien à droite ?", "la barre HP du château est-elle visible ?"). Le coût est non-négligeable si un sprint-gate tourne 10 scenarios × 30 actions chacun. Le déterminisme reposerait sur `__cd.metrics` exposé via `evaluate_script` côté navigateur. Computer use seul n'a pas de hook deterministic pour le PRNG du jeu : il faudrait que Milan expose un seed et que le scenario le set avant exécution. Latence cumulée d'un sprint-gate ~10 min par scenario non-trivial.

---

## 2. Playwright MCP (Microsoft + Anthropic plugin)

### 2.1 Description

`@playwright/mcp` publié par Microsoft mars 2025. Pattern : MCP server qui expose Playwright à un client LLM (Claude Desktop, Claude Code, Cursor) via des tools structurés. **Spécificité majeure** : utilise l'**accessibility tree** (ARIA) au lieu de screenshots — donc pas de vision model nécessaire, latence ~10x plus rapide qu'un agent computer-use pur. Anthropic a publié un plugin officiel `claude.com/plugins/playwright`. ([Microsoft repo](https://github.com/microsoft/playwright-mcp), [Anthropic plugin](https://claude.com/plugins/playwright), [Simon Willison teardown](https://simonwillison.net/2025/Mar/25/playwright-mcp/))

### 2.2 Use case typique gaming

Faiblement adapté aux jeux canvas (Three.js, Phaser) car l'accessibility tree d'un canvas est vide. **Très adapté aux UI HTML autour du jeu** : menus, shop, toolbar, modals. Un studio pourrait scripted-tester son menu launcher, ses dialogs de level-up, ses stores in-game. Le pattern Bug0/Decipher cite stable Playwright code generation par GPT-4o et Claude 4 sur la première itération avec role-based locators ([Bug0 article](https://bug0.com/blog/playwright-mcp-changes-ai-testing-2026)).

### 2.3 Outil/SDK

`@playwright/mcp` server (Node.js binary), connecté via stdio à un client MCP. Tools : `browser_navigate`, `browser_click`, `browser_type`, `browser_snapshot` (ARIA tree), `browser_take_screenshot`, `browser_wait_for`. Trace + recording exportables pour debug.

### 2.4 Hard vs soft assertions

- **Hard** : très fort. ARIA assertions stables (`expect(page.getByRole('button', {name: 'Lancer la vague'})).toBeVisible()`), `evaluate_script` pour lire window.* (donc `__cd.metrics` accessible directement).
- **Soft** : moyen — Claude peut analyser un trace.zip post-mortem (TestDino, Decipher cités) mais l'accessibility-only loupe le rendu visuel d'un canvas.

### 2.5 Coût

Génération de tests trace-based : $0.02-0.08/test avec GPT-4o-mini ou Claude Haiku ([buildbetter guide](https://blog.buildbetter.ai/playwright-test-generation-with-ai-complete-2026-guide/)). Exécution : gratuite (juste CPU local). Maintenance : Claude analyse stack trace + screenshot post-fail, ~$0.05 par diagnostic.

### 2.6 Limites

- **Canvas blind spot** : un canvas WebGL/Three.js apparaît comme `<canvas role="img">` sans children dans l'ARIA tree. Tout le gameplay du board Milan serait invisible.
- Flakiness sur animations CSS : `wait_for` requiert sélecteurs explicites.
- N'est pas un agent autonome : c'est un set de tools. L'autonomie vient du LLM client.

### 2.7 Applicabilité à Milan CD V3

Milan a déjà un Chrome MCP existant (cf CLAUDE.md). Playwright MCP serait un substitut/complément, avec l'avantage de la stabilité accessibility-tree mais l'inconvénient majeur du canvas blind spot. La toolbar HTML, les modals, le menu de niveau, les écrans résultat (s'ils sont DOM) seraient triviaux à tester. Le board Three.js nécessiterait un fallback `evaluate_script` qui lit `__cd.metrics` ou `__cd.runner.staticEnemies.length`. Le pattern "trace-based test generation" (Mike enregistre un playthrough manuel, Claude génère le .mjs) est applicable si Milan a déjà un mode debug enregistreur — sinon, à instrumenter. Coût d'une suite 10 scenarios reste sous $1 par run en Sonnet.

---

## 3. Chrome DevTools MCP (Google)

### 3.1 Description

MCP server officiel ChromeDevTools annoncé 23 septembre 2025, public preview. Donne aux agents (Claude Code, Cursor, Gemini CLI) un contrôle complet d'un Chrome live : 29 tools spanning automation, performance profiling, network analysis, debugging. Combine ce que faisait Chrome DevTools UI manuellement avec des hooks programmables. ([GitHub repo](https://github.com/ChromeDevTools/chrome-devtools-mcp), [Addy Osmani blog](https://addyosmani.com/blog/devtools-mcp/), [Anthropic plugin](https://claude.com/plugins/chrome-devtools-mcp))

### 3.2 Use case typique gaming

Profiler Chrome live d'un jeu web : FPS via Performance panel, memory leaks via Heap snapshots, network via Network panel (download d'assets, lazy load), console messages pour catch des `console.error`. Cas Milan-like : valider qu'un niveau ne fuit pas de mémoire, mesurer le FPS sur une vague stress-testée, capturer les warnings WebGL.

### 3.3 Outil/SDK

29 tools dans 5 catégories :
- **Input automation** (7) : `click`, `drag`, `fill`, `fill_form`, `handle_dialog`, `hover`, `upload_file`.
- **Navigation** (7) : `navigate_page`, `new_page`, `list_pages`, `select_page`, `close_page`, `navigate_page_history`, `wait_for`.
- **Debugging** (4) : `evaluate_script`, `list_console_messages`, `take_screenshot`, `take_snapshot`.
- **Performance/network** (~11) : profilage, traces, requêtes.

### 3.4 Hard vs soft assertions

- **Hard** : excellent. `evaluate_script` exécute du JS arbitraire dans la page (donc `window.__cd.metrics` accessible). `list_console_messages` capture les errors. Performance traces mesurent FPS, heap, layout shifts numériquement.
- **Soft** : `take_screenshot` puis raisonnement LLM (mais c'est le LLM client qui juge, pas l'outil).

### 3.5 Coût

L'outil est gratuit (open source Google). Le coût est celui du LLM client qui orchestre. Comparable à Playwright MCP : $0.05-0.50 par scenario en Sonnet, $1-3 en Opus.

### 3.6 Limites

- Encore en public preview (sept 2025) — API peut bouger.
- Setup demande Chrome installé local + MCP config dans le client.
- Performance trace export volumineux (MB) — peut saturer le contexte si analysé brut.

### 3.7 Applicabilité à Milan CD V3

Milan utilise déjà Chrome MCP (via `mcp__claude-in-chrome__*`). Chrome DevTools MCP de Google est le standard officiel concurrent — fonctionnellement très proche pour `evaluate_script`, `list_console_messages`, `take_screenshot`, `navigate_page`. La différence majeure côté Milan serait d'accéder aux **performance traces** (FPS desktop ≥ 45 sur sprint-gate E2 nécessite mesure réelle, pas une approximation `requestAnimationFrame` lue depuis `__cd.metrics`). Le hook `evaluate_script` permettrait de lire `window.__cd.metrics.lastRun` et de scripter setup `__cd.goto(id)` puis assertions. Aucune barrière technique — c'est essentiellement le même pattern que Milan utilise déjà, mais sourcé Google plutôt que MCP communautaire.

---

## 4. LLM-as-judge (G-Eval, Langfuse, DeepEval, Confident AI)

### 4.1 Description

Pattern formalisé en 2023, généralisé en 2024 avec le survey "LLMs-as-Judges" (arxiv 2412.05579). Principe : un LLM reçoit un input + une réponse + une rubric, retourne un score + un raisonnement. Frameworks : DeepEval (50+ métriques research-backed dont G-Eval), Langfuse (intégration tracing prod), Arize Phoenix (open-source, hallucination detection), AWS Bedrock Model Evaluation. ([Survey arxiv](https://arxiv.org/html/2412.05579v2), [Evidently guide](https://www.evidentlyai.com/llm-guide/llm-as-a-judge), [Langfuse docs](https://langfuse.com/docs/evaluation/evaluation-methods/llm-as-a-judge), [G-Eval Confident AI](https://www.confident-ai.com/blog/g-eval-the-definitive-guide))

### 4.2 Use case typique gaming

Pas spécifique aux jeux à l'origine (NLP eval). Mais le pattern transpose : "voici un screenshot du HUD pendant la wave 5, voici la rubric (lisibilité, hiérarchie visuelle, alerting), retourne un score 1-5 + raisonnement". Sert pour valider le **feel** d'un changement de balance, la **clarté** d'un nouveau VFX, la **lisibilité** d'un panel d'upgrade.

### 4.3 Outil/SDK

- **DeepEval** (Confident AI) : `from deepeval.metrics import GEval`, criteria string, evaluation_params. Open source, Python.
- **Langfuse** : intégration tracing, judges configurables via UI ou code.
- **Arize Phoenix** : open source, hallucination + general evaluators.
- **Direct prompt Claude** : pas besoin de framework — un user message structuré "Score this from 1 to 5 against criteria X, return JSON {score, reason}" suffit.

### 4.4 Hard vs soft assertions

- 100% **soft** par définition. La rubric peut être stricte ("le bouton est-il dans le tiers droit de l'écran ?") mais la mesure reste un jugement LLM.
- Validation : viser 75-90% d'agreement avec labels humains sur un golden dataset avant de scaler ([Monte Carlo best practices](https://www.montecarlodata.com/blog-llm-as-judge/)).

### 4.5 Coût

- Évaluation lourde académique (PaperBench) : $9,500 par run, $150K pour 6 modèles × 3 seeds ([HuggingFace eval costs](https://huggingface.co/blog/evaleval/eval-costs-bottleneck)).
- Évaluation légère per-output : $0.001-$0.10 selon longueur. Une rubric de 200 tokens + une réponse de 500 tokens + reasoning de 300 tokens ≈ $0.005 en Sonnet, $0.025 en Opus.
- Discount : Batch API 50%, prompt caching cache hit = 10% du prix input.

### 4.6 Limites

- **Bias notoires** : position bias (favorise réponse A), length bias (favorise réponses longues), self-preference bias (Claude favorise Claude).
- Need golden dataset humain pour calibrer.
- Non-déterministe sans `temperature=0` (et même là, pas garanti à 100%).
- Rubric drift : un même juge donne des scores différents à 3 mois d'écart si modèle update.

### 4.7 Applicabilité à Milan CD V3

LLM-judge est le mécanisme natif pour les soft assertions du sprint-gate (déjà mentionné dans le persona `auto-qa-runner` : "Acts as LLM judge for soft criteria UX feeling, balance"). La même instance Opus qui pilote le scenario peut fournir le jugement — pas besoin d'infra séparée. Pour Milan, les rubrics typiques : "le bouton wave est-il bien visible/cliquable ?", "le HUD est-il lisible pendant W5+ dense ?", "la difficulté ressentie de cette wave colle-t-elle à killSpendRatio mesuré ?". Le risque principal est le bias self-preference si le même Opus a généré le scenario ET juge — pattern industrie suggère séparer (ex Sonnet exécute, Opus juge) ou avoir un golden dataset Mike-validé. Coût d'un sprint-gate avec 10 soft assertions ≈ $0.50.

---

## 5. DeepMind SIMA / Voyager (NVIDIA-Caltech-Stanford)

### 5.1 Description

- **Voyager** (mai 2023, NVIDIA + Caltech + Stanford + UT) : premier agent embodied lifelong learning utilisant GPT-4 sur Minecraft. 3 composants : automatic curriculum, ever-growing skill library (code exécutable), iterative prompting avec self-verification. 3.3x plus d'items uniques, 2.3x plus de distance, 15.3x plus rapide à débloquer le tech tree que SOTA précédent. ([arxiv 2305.16291](https://arxiv.org/abs/2305.16291), [voyager.minedojo.org](https://voyager.minedojo.org/), [GitHub](https://github.com/MineDojo/Voyager))
- **SIMA 1** (mars 2024, DeepMind) : Scalable Instructable Multiworld Agent, suit instructions free-form sur 9 jeux 3D commerciaux. Surpasse les agents spécialisés single-game. ([arxiv 2404.10179](https://arxiv.org/abs/2404.10179), [DeepMind blog](https://deepmind.google/blog/sima-generalist-ai-agent-for-3d-virtual-environments/))
- **SIMA 2** (décembre 2025, DeepMind) : built on Gemini foundation, conversational, gère instructions complexes via langage + images, self-improvement via Gemini-generated tasks/rewards, generalize sur unseen environments. ([arxiv 2512.04797](https://arxiv.org/abs/2512.04797), [DeepMind blog SIMA 2](https://deepmind.google/blog/sima-2-an-agent-that-plays-reasons-and-learns-with-you-in-virtual-3d-worlds/))

### 5.2 Use case typique gaming

**Pas QA orienté à l'origine** — c'est de la recherche en agent généraliste. Mais le pattern "agent qui joue le jeu, génère sa propre curriculum, vérifie ses succès" se rapproche du QA exploratoire AAA (cf §6 EA/Ubisoft). Voyager illustre la skill library : le bot accumule des "snippets de code" qui sont des sous-routines réutilisables. SIMA 2 illustre la self-improvement : l'agent génère ses propres tâches.

### 5.3 Outil/SDK

- **Voyager** : MineDojo (sim Minecraft), GPT-4 API, Python orchestration. Open source.
- **SIMA** : closed (DeepMind interne, pas de SDK public). Gemini foundation. Connecte aux jeux commerciaux via leur input/output normal (mouse/keyboard + screen).

### 5.4 Hard vs soft assertions

- Voyager : self-verification interne (le LLM vérifie son propre code émis a-t-il atteint l'objectif). Mesures externes : nombre d'items uniques, distance parcourue, milestones tech tree.
- SIMA : human eval contre baselines + auto-eval via Gemini judge.

### 5.5 Coût

Inaccessible/non-publié pour usage QA prod. Voyager : usage GPT-4 API intensif (compteurs non publiés mais probablement >$100/expérience). SIMA : compute interne DeepMind, probablement clusters TPU.

### 5.6 Limites

- **Pas conçu pour QA reproductible** : ces agents EXPLORENT, ils ne valident pas un scénario précis.
- Compute lourd, latence élevée.
- Voyager Minecraft-only (l'API MineDojo est spécifique).
- SIMA closed source.

### 5.7 Applicabilité à Milan CD V3

Aucune adoption directe possible : ces frameworks sont research-grade et orientés generalist exploration, pas validation sprint-gate. **Mais** le pattern "skill library" de Voyager est intéressant conceptuellement : un agent QA pourrait accumuler des "snippets de scenario" réutilisables (set up wave 5, place tour X, mesure castleHpPercent). Le pattern "self-improvement via LLM-generated tasks" de SIMA 2 suggère qu'un Opus pourrait générer de nouveaux scenarios edge-case à partir d'une description du jeu, plutôt que Mike les écrire à la main. Aucun de ces patterns n'est pour Milan une dépendance — c'est de l'inspiration sur l'autonomie possible long terme.

---

## 6. EA SEED / Ubisoft Client Bots / DICE AutoPlayers (RL bots production AAA)

### 6.1 Description

Pattern industrie production AAA depuis fin 2010s. Bots autonomes (souvent RL ou comportements scripted/hybrides) qui jouent les jeux pour valider gameplay, balance, soak tests, performance.

- **EA SEED** : research division, "AI for Testing at EA: From Star Wars to Apex and Beyond" (GDC 2019). Test bots évolués depuis Battlefield V, étendus à Star Wars Battlefront 2, Apex Legends, Battlefield 2042, Dead Space. RL inclus. ([GDC Vault](https://gdcvault.com/play/1028718/AI-Summit-AI-for-Testing), [GTC 2021](https://www.ea.com/seed/news/gtc-2021-towards-advanced-game-testing-with-ai), [SEED automated game testing RL](https://www.ea.com/seed/news/automated-game-testing-deep-reinforcement-learning))
- **Ubisoft Client Bots** : "Automated Testing: Using AI Controlled Players to Test The Division" (GDC). AI prend contrôle du joueur, mime input humain, report missions incompletables et perf stats. Aussi Commit Assistant intégré pipeline QA. ([GDC Vault](https://gdcvault.com/play/1026382/Automated-Testing-Using-AI-Controlled))
- **DICE AutoPlayers** : "AI for Testing: The Development of Bots that Play Battlefield V" (GDC). Soak tests 64 player, scripted scenarios. ([GDC Vault](https://gdcvault.com/play/1026308/AI-for-Testing-The-Development))
- **arxiv 2103.15819** : "Augmenting Automated Game Testing with Deep Reinforcement Learning" — recherche académique sur le sujet.

### 6.2 Use case typique gaming

Soak tests (laisser tourner 64 bots pendant 8h pour catch crashes, leaks, désync), progression blockers detection (un bot ne peut pas finir mission X → ticket auto-créé), balance détection (winrate skewed sur map Y → ticket), performance stats sur scénarios répétables.

### 6.3 Outil/SDK

In-house propriétaire. EA SEED, Ubisoft, DICE n'ont pas open-sourcé. Stack typique : agent RL trainé sur game state (offert par game engine), récompense = progression + survie + objectifs. Stable Baselines, custom Unity ML-Agents, frameworks internes.

### 6.4 Hard vs soft assertions

- **Hard** : très fort. Métriques numériques quantitatives (FPS, RTT, win/loss, completion time, mission state). Un bot émet une trace structurée que le QA peut diff vs baseline.
- **Soft** : limité. Les RL bots ne "jugent" pas l'UX — ils mesurent.

### 6.5 Coût

Setup initial très lourd : équipe ML dédiée (3-10 ingés), infra GPU pour training, intégration au game engine pour state access. Once setup, runtime cheap (CPU bots peuvent tourner sur build farms). Pas de figure publique mais clairement >>$100K/an pour un studio AAA.

### 6.6 Limites

- **Pas adapté indé/solo dev** : ROI uniquement sur multi-million player base.
- Curse of dimensionality : un bot RL trainé sur Battlefield V ne marche pas sur Apex sans retraining.
- Pas LLM-driven — c'est du RL classique. Pas de raisonnement sémantique sur l'UX.

### 6.7 Applicabilité à Milan CD V3

Pattern overkill pour le scope Milan (un dev solo, jeu 60-80 niveaux, build < 1MB). Mais les **principes** sont applicables : (1) un bot scripted (pas RL) qui joue un niveau de bout en bout en mode autopilot et émet des métriques structurées (`__cd.metrics.lastRun`) est exactement ce que le persona `auto-qa-runner` ferait. (2) Le concept de "soak test" (10 niveaux back-to-back, regarder si crash/leak) est applicable via un .mjs qui boucle `__cd.goto(id)` et mesure heap. (3) La détection de progression blocker (le bot finit-il le château intact > 50% HP en W5+ ? cf objectif §1 du plan strat) est exactement le kill/spend ratio threshold du gate. Mais aucun framework AAA n'est utilisable en l'état — c'est du in-house à recoder.

---

## 7. Anthropic Routines / Claude Code scheduled tasks

### 7.1 Description

Trois mécanismes Anthropic de scheduling, dont **Routines** (avril 2026) qui transforme Claude Code en plateforme d'automatisation cloud :

- **Cloud Scheduled Tasks (Routines)** : tournent sur infra Anthropic, persistent across restarts, exécutent même machine éteinte. Cron-style scheduling, langage naturel ("every weekday at 9am") traduit. Quotas : Pro 5/jour, Max 15/jour, Team/Enterprise 25/jour.
- **Desktop Scheduled Tasks** : graphical scheduling Claude Desktop, machine doit être on.
- **CLI Session-Scoped (`/loop`)** : poll/repeat dans une session ouverte.

([Claude Code scheduled tasks docs](https://code.claude.com/docs/en/scheduled-tasks), [DevOps.com Routines coverage](https://devops.com/claude-code-routines-anthropics-answer-to-unattended-dev-automation/), [Tessl coverage](https://tessl.io/blog/anthropic-adds-routines-to-claude-code-for-scheduled-agent-tasks/))

### 7.2 Use case typique gaming

Pas spécifique gaming, mais directement applicable : nightly QA run, weekly perf audit, pre-release smoke test sur staging. Trigger via webhook GitHub (push main) → Claude Code lance la suite QA, commit le report.

### 7.3 Outil/SDK

Claude Code (>= avril 2026), `routines` config dans le projet. Combine prompt + repo connecté + tools dispo (incluant MCP servers). Trigger options : cron, API call, GitHub webhook event.

### 7.4 Hard vs soft assertions

Orthogonal — Routines est un orchestrateur. La nature des assertions dépend de ce que le prompt + tools font (lance Playwright MCP → hard, lance LLM-judge → soft).

### 7.5 Coût

Inclus dans le plan Claude Code (Pro, Max, Team, Enterprise). Pas de pricing per-routine séparé. Quota lim 5/15/25 par jour selon tier.

### 7.6 Limites

- Quota journalier = pas plus de 5 sprint-gates/jour en Pro.
- Cloud routine = environnement Anthropic, donc accès au repo via integration GitHub. Pas de filesystem local persistent custom.
- Encore récent (avril 2026) — patterns d'usage encore en train de se stabiliser.

### 7.7 Applicabilité à Milan CD V3

Routines correspond au "trigger" du sprint-gate auto-QA — la question n'est pas le runner (Claude Opus + Chrome MCP existant) mais le moment où il s'exécute. Trois options observables dans l'industrie : (1) cron fixe ("every Friday 6pm" = fin de sprint), (2) trigger sur push main d'un commit avec tag `[sprint-end]`, (3) invocation manuelle Mike depuis CLI. Le plan strat actuel est sur invocation manuelle Mike (cf §4bis.5 "Sprint-gate appliqué le dernier jour ouvré de chaque sprint"). Routines ouvrirait l'option cron auto sans Mike présent. Quota Pro 5/jour est largement suffisant pour 6 sprints sur 8 semaines. Pas de pré-requis technique côté Milan — c'est une décision opérationnelle.

---

## Patterns universels (3 reproductibles partout)

Ces 3 patterns se retrouvent dans **toutes** les approches industrie sérieuses :

1. **Hard assertions via DOM/state introspection** (Playwright MCP, Chrome DevTools MCP, Computer Use combo). Toujours un mécanisme `evaluate_script` ou équivalent qui lit l'état du jeu programmatiquement (équivalent `__cd.metrics`). Reposer uniquement sur la vision/screenshot est plus cher et plus flaky.

2. **Soft assertions via LLM-judge avec rubric explicite** (G-Eval, DeepEval, direct Claude prompt). Toujours une rubric codée dans le scenario, un score numérique, un raisonnement justificatif. Pattern stable depuis 2024, cité dans 100% des frameworks LLM eval modernes.

3. **Deterministic seed pour reproductibilité** (NetSecGame, OpenAI seed param, PyTorch reproducibility, Isaac Lab). Toute suite de regression sérieuse expose un seed param qui se propage au PRNG de la sim. Sans ça, un bug détecté n'est pas re-trigger-able. ([Algomimic deterministic seeding](https://algomimic.com/blog/deterministic-seeding.html), [OpenAI cookbook seed](https://cookbook.openai.com/examples/reproducible_outputs_with_the_seed_parameter))

---

## Patterns différenciants (3 signatures uniques)

Ces 3 patterns sont **distinctifs** et ne se retrouvent qu'à un endroit précis :

1. **Skill library auto-accumulée** (Voyager 2023). L'agent émet du code pour résoudre une tâche, et ces snippets sont stockés et réutilisés sur tâches futures. Personne d'autre ne fait ça — la plupart des autres patterns scriptent les scenarios à la main.

2. **Accessibility tree-only browser automation** (Playwright MCP). Choix architectural fort : pas de vision, juste ARIA. Trade-off : très rapide et stable, mais aveugle au canvas. Computer Use d'Anthropic fait l'inverse (full vision). Personne d'autre sur le marché n'est aussi explicite sur ce trade-off.

3. **Cloud scheduling natif intégré au LLM platform** (Anthropic Routines avril 2026). Les autres frameworks (GitHub Actions, cron classique) requièrent infra séparée. Anthropic est le premier à intégrer scheduling directement dans Claude Code en GA.

---

## Synthèse pour Milan : 3 axes recommandés à creuser en design D1/D2

### Axe A — Choix du substrat browser automation : Chrome MCP existant vs Chrome DevTools MCP officiel vs Playwright MCP

Les 3 outils sont fonctionnellement proches pour les besoins Milan (`evaluate_script` + `take_screenshot` + `list_console_messages`). Le Chrome MCP actuel de Milan a déjà ses 25+ tools dispos. Chrome DevTools MCP de Google offre des `performance traces` natives (utile pour le gate FPS ≥ 45). Playwright MCP a l'avantage stabilité accessibility-tree pour les UI HTML mais le canvas Three.js du board est un blind spot. Décision à prendre en D1 : rester monolithique (Chrome MCP only) ou multi-outil (Chrome MCP + Chrome DevTools MCP pour perf traces).

### Axe B — Granularité des assertions hard vs soft, et coût d'un sprint-gate

Les patterns industrie convergent sur un mix : **hard assertions** (numériques, déterministes, reproductibles via seed) + **soft assertions** (LLM-judge avec rubric, score 1-5). Le plan Milan cible 90% hard pass + 75% soft pass. Coût d'un sprint-gate complet = N scenarios × (token cost Computer Use OU Playwright MCP) + M soft judgments × $0.005-0.025 (selon Sonnet/Opus). 6 sprint-gates × 5-10 scenarios × $0.50 = ~$15-30 sur 8 semaines. Décision D1/D2 : combien de soft assertions par scenario, et qui juge (même Opus ou Sonnet séparé pour éviter self-preference bias).

### Axe C — Trigger du sprint-gate : manuel Mike vs Routines cron vs GitHub webhook

Trois patterns industrie observables : (1) **manuel** (le dev lance le gate fin de sprint depuis CLI — actuel plan Milan), (2) **scheduled cron** (Anthropic Routines, GitHub Actions schedule — gate auto le vendredi 18h), (3) **event-driven** (push main avec commit tag `[sprint-end]` déclenche un webhook qui lance la suite). Le pattern (1) garde Mike in-the-loop mais peut être skippé si Mike est AFK. Le pattern (2) garantit la régularité mais peut tourner sur un sprint pas vraiment fini. Le pattern (3) couple bien avec un workflow git mais demande discipline tagging. Décision D1 ou plus tard (E1/E2) : choisir le trigger par sprint, pas forcément un seul pour tous.

---

## Sources

### Anthropic Computer Use
- [Anthropic intro 3.5 Sonnet + computer use](https://www.anthropic.com/news/3-5-models-and-computer-use)
- [Computer use tool docs](https://platform.claude.com/docs/en/agents-and-tools/tool-use/computer-use-tool)
- [Claude Code CLI computer use](https://code.claude.com/docs/en/computer-use)
- [Claude pricing 2026](https://platform.claude.com/docs/en/about-claude/pricing)
- [Opus 4.7 pricing analysis Finout](https://www.finout.io/blog/claude-opus-4.7-pricing-the-real-cost-story-behind-the-unchanged-price-tag)

### Playwright MCP
- [Microsoft Playwright MCP repo](https://github.com/microsoft/playwright-mcp)
- [Anthropic Playwright plugin](https://claude.com/plugins/playwright)
- [Simon Willison Playwright MCP teardown](https://simonwillison.net/2025/Mar/25/playwright-mcp/)
- [Bug0 Playwright MCP 2026](https://bug0.com/blog/playwright-mcp-changes-ai-testing-2026)
- [BuildBetter AI Playwright generation guide 2026](https://blog.buildbetter.ai/playwright-test-generation-with-ai-complete-2026-guide/)

### Chrome DevTools MCP
- [Google ChromeDevTools MCP repo](https://github.com/ChromeDevTools/chrome-devtools-mcp)
- [Addy Osmani: Give your AI eyes](https://addyosmani.com/blog/devtools-mcp/)
- [Anthropic Chrome DevTools plugin](https://claude.com/plugins/chrome-devtools-mcp)
- [DataCamp Chrome DevTools MCP tutorial](https://www.datacamp.com/tutorial/chrome-devtools-mcp)

### LLM-as-judge
- [Survey LLMs-as-Judges arxiv 2412.05579](https://arxiv.org/html/2412.05579v2)
- [Evidently LLM-as-judge guide](https://www.evidentlyai.com/llm-guide/llm-as-a-judge)
- [Langfuse LLM-as-judge](https://langfuse.com/docs/evaluation/evaluation-methods/llm-as-a-judge)
- [Confident AI G-Eval](https://www.confident-ai.com/blog/g-eval-the-definitive-guide)
- [Monte Carlo 7 best practices](https://www.montecarlodata.com/blog-llm-as-judge/)
- [HuggingFace eval costs bottleneck](https://huggingface.co/blog/evaleval/eval-costs-bottleneck)

### DeepMind SIMA / Voyager
- [Voyager arxiv 2305.16291](https://arxiv.org/abs/2305.16291)
- [Voyager project page](https://voyager.minedojo.org/)
- [Voyager GitHub](https://github.com/MineDojo/Voyager)
- [SIMA 1 arxiv 2404.10179](https://arxiv.org/abs/2404.10179)
- [DeepMind SIMA blog](https://deepmind.google/blog/sima-generalist-ai-agent-for-3d-virtual-environments/)
- [SIMA 2 arxiv 2512.04797](https://arxiv.org/abs/2512.04797)
- [DeepMind SIMA 2 blog](https://deepmind.google/blog/sima-2-an-agent-that-plays-reasons-and-learns-with-you-in-virtual-3d-worlds/)
- [TechCrunch SIMA 2 coverage](https://techcrunch.com/2025/11/13/googles-sima-2-agent-uses-gemini-to-reason-and-act-in-virtual-worlds/)

### EA SEED / Ubisoft / DICE / RL bots
- [GDC AI for Testing at EA](https://gdcvault.com/play/1028718/AI-Summit-AI-for-Testing)
- [GDC Battlefield V AutoPlayers](https://www.gdcvault.com/play/1026308/AI-for-Testing-The-Development)
- [GDC The Division Client Bots](https://gdcvault.com/play/1026382/Automated-Testing-Using-AI-Controlled)
- [GDC Smart Bots Ubisoft](https://www.gdcvault.com/play/1026281/ML-Tutorial-Day-Smart-Bots)
- [EA SEED GTC 2021](https://www.ea.com/seed/news/gtc-2021-towards-advanced-game-testing-with-ai)
- [EA SEED RL game testing](https://www.ea.com/seed/news/automated-game-testing-deep-reinforcement-learning)
- [arxiv 2103.15819 Augmenting Automated Game Testing with DRL](https://arxiv.org/abs/2103.15819)

### Anthropic Routines / scheduled
- [Claude Code scheduled tasks docs](https://code.claude.com/docs/en/scheduled-tasks)
- [DevOps.com Claude Code Routines](https://devops.com/claude-code-routines-anthropics-answer-to-unattended-dev-automation/)
- [Tessl Anthropic Routines](https://tessl.io/blog/anthropic-adds-routines-to-claude-code-for-scheduled-agent-tasks/)
- [MindStudio Claude Code Routines](https://www.mindstudio.ai/blog/claude-code-routines-scheduled-agents)

### Deterministic seed / regression
- [Algomimic Deterministic Seeding](https://algomimic.com/blog/deterministic-seeding.html)
- [OpenAI Cookbook reproducible seed](https://cookbook.openai.com/examples/reproducible_outputs_with_the_seed_parameter)
- [NetSecGame v0.2.0 seed](https://www.stratosphereips.org/blog/2026/4/1/netsecgame-v020-reproducible-experiments-and-a-more-robust-game-server)
- [Isaac Lab reproducibility](https://isaac-sim.github.io/IsaacLab/main/source/features/reproducibility.html)

### Industry tools complémentaires
- [Sentry Session Replay product](https://sentry.io/product/session-replay/)
- [Sentry Seer AI debugger GA](https://blog.sentry.io/seer-sentrys-ai-debugger-is-generally-available/)
- [Sentry AI code review press release sept 2025](https://sentry.io/about/press-releases/sentry-announces-ai-code-review/)
