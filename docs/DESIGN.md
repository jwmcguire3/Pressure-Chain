# Pressure Chain — Design Document v1.0

---

## 1. CORE FEEL STATEMENT

A great turn feels like pulling a pin on a mechanism you built three moves ago. The player sees a node about to burst, recognizes that its neighbors are also near threshold, and — instead of defusing it — nudges one adjacent node into criticality so the burst cascades in a direction that clears the half of the board they've been ignoring. Five minutes in, the player understands that pressure is a resource, not a threat; the board isn't a bomb to defuse, it's a Rube Goldberg machine they're arming. The session arc runs: **early calm** (reading the board, venting safely) → **mid-tension** (multiple nodes converging, the player is behind the curve) → **catharsis or collapse** (one decisive chain either resolves everything in a single satisfying cascade, or fires into a region they misread and the board eats itself). Both outcomes feel earned. The player leaves either grinning or immediately restarting.

---

## 2. BOARD ARCHITECTURE

**Grid dimensions and shape.** Hexagonal grid, default 7×9 (63 cells). Hex is chosen over square because six neighbors produce richer chain topology than four, and diagonal ambiguity is eliminated — every adjacency is explicit. Variant boards: 5×7 (puzzle-dense, short sessions), 9×11 (late-game sprawl), and irregular "cut" boards where cells are removed to force pressure through chokepoints. Special boards include toroidal wrap (edges connect, rare, endgame only) and layered boards (see below).

**Node anatomy.** Every node carries seven data fields: `pressure` (0–100 integer), `state` (stable/swelling/critical/volatile/locked), `type` (one of the taxonomy in §3), `connections` (which of six neighbors it accepts/rejects pressure from — a 6-bit mask), `modifiers` (temporary tags: frozen, insulated, charged, contaminated), `capacity` (the pressure at which it becomes volatile — default 100, varies by type), and `release_value` (base score on burst — default 10, modified by type and chain context).

**Pressure propagation.** Event-driven, not real-time. Pressure rises on three triggers: the player ending a turn (global tick: every node gains pressure based on its type, typically +5 to +15), a neighboring node bursting (directional shove: the burst pushes a fraction of its pressure into each connected neighbor per its direction rules), and environmental sources on certain levels (vents, leaks, reactors). Adjacency always matters — pressure never jumps non-adjacent cells except through explicitly-defined conduit node types.

**Chain mechanics.** A chain triggers when a node bursts and its release pushes at least one neighbor across its own volatile threshold within the same resolution step. Chains resolve in breadth-first waves: all nodes that become volatile in wave N burst simultaneously at the start of wave N+1, and their combined output propagates outward. This prevents order-of-operations ambiguity and makes chains readable as expanding rings. A chain is **good** when its final wave clears a critical node the player couldn't have reached manually, or clears ≥4 nodes in a single resolution. A chain is **wasted** when it only clears stable nodes (burning fuel you didn't need to burn) or when it terminates before reaching a node the player was targeting — the telltale sign of misreading direction.

**Board layers.** Two layers maximum: surface and substrate. Substrate nodes are buried — visible as a darker cell tint with a faint shape hint, but their type and pressure are hidden. A substrate node reveals when an adjacent surface node bursts against it (it absorbs the shove and flips to surface state with full pressure transferred). Substrate exists only on specific level types; most boards are single-layer. No third layer — stacking beyond two destroys spatial reasoning.

---

## 3. NODE TYPE TAXONOMY

Ten types. Each has a distinct silhouette (shape), a state-coded color (see §8), and a motion signature.

**1. Cell (standard).** Round, neutral. Gains +10 pressure per tick. Bursts outward evenly to all six neighbors at 100. Baseline against which all others are read. Strategic role: fuel. Most of the board is Cells.

**2. Vent.** Triangle pointing in one of six directions. Gains +8 per tick. On burst, sends 80% of release in its pointing direction (one neighbor gets a huge shove), 20% split among the other five. Strategic role: the primary chain-direction tool. Rotating a vent is a paid action (see §4).

**3. Conduit.** Elongated capsule with two aligned end-caps. Does not accumulate pressure on its own (+0 per tick). Passes pressure through: if shoved from one end, it releases from the opposite end on the next wave, unchanged in magnitude, to the next neighbor. Allows chains to reach across stable intermediate cells. Strategic role: range extension, bridge-building.

**4. Bulwark (locked/insulated).** Hexagonal with a dark metallic ring. Pressure stays at 0. Does not burst, does not propagate, blocks pressure entirely on all six faces. Strategic role: wall — shapes the board, forces chains into specific corridors. Some levels let the player destroy a Bulwark with a charged burst (≥3-wave chain hitting it).

**5. Amplifier (multiplier).** Diamond with an inward-facing chevron pattern. Gains +5 per tick. When it bursts as part of a chain (not standalone), it multiplies the outgoing pressure by 2× and the score release of every subsequent wave in that chain by 1.5×. Standalone burst is weak — amplifiers are useless unless chained into. Strategic role: the "build-around" piece. Entire puzzles revolve around routing a chain through an Amplifier.

**6. Timer (punishes mistiming).** Circular with a visible segmented ring that depletes each tick. Starts at 6 segments. Gains +15 per tick (fast builder). If it reaches 0 segments *before* the player bursts it as part of a chain, it detonates catastrophically: full outward burst at 3× release value, and every neighbor gains +50 pressure instead of the normal shove. If the player bursts it via chain before timeout, it releases at 2× value cleanly. Strategic role: forces the player to commit to a deadline — the single most common source of "I should have acted two turns ago" moments.

**7. Sink.** Inverted funnel shape. Gains +3 per tick (slow). On receiving pressure from neighbors, absorbs up to 50 pressure without propagating — eats shoves. Bursts weakly (half release). Strategic role: chain-terminator, safety valve. Placing actions near a Sink means chains die there. Clearing a level with Sinks intact is usually a design signal.

**8. Contaminant.** Irregular, spiked silhouette with a sickly tint. Gains +12 per tick. On burst, converts up to 2 adjacent Cells into Contaminants (spreads). If not cleansed (burst as part of a chain of length ≥3), spread is permanent for the level. Strategic role: creates "cleanse contaminated zones" objectives; punishes passive play directly.

**9. Reactor.** Large, takes up a single hex but visually heavier, with a core that fills as it charges. Gains +0 per tick. Must be fed pressure by chain shoves — each shove adds to its charge meter. Bursts only when fully charged (typically 300 accumulated pressure), and its burst is a board-clearing event (objective completion on "charge reactor" levels). Strategic role: long-term objective, focus point. Chains must be engineered *toward* it.

**10. Mirror.** Hexagonal with an internal reflective axis. Gains +6 per tick. When shoved, redirects the shove 120° clockwise around its own center — pressure coming in from the north exits to the southeast. Allows the player to route chains around Bulwarks. Strategic role: spatial puzzle piece, rewards geometric thinking.

*Design note:* No more node types in launch scope. Ten is the readable ceiling; every additional type taxes the player's recognition budget. Expansions should reskin behaviors (seasonal variants of Amplifier, etc.) before adding genuinely new types.

---

## 4. PLAYER ACTION SYSTEM

**Base actions (no consumables required):**

- **Merge** — select two adjacent nodes of the same type; combine their pressure into one, leaving the other as a fresh stable Cell. Net pressure is conserved. Primary tool for pushing a specific node to critical on demand.
- **Vent-redirect** — rotate the facing direction of any Vent or Mirror by one hex-step (60°). Repointing only; no pressure change.
- **Trigger early** — force a node at ≥critical (pressure ≥ 75) to burst immediately, before it reaches volatile. Release is 75% of a natural burst. The controlled-demolition move.
- **Isolate** — toggle one face of a node's connection mask closed for one turn. Pressure can't enter or exit that face. One isolate per turn.
- **Pass** — end turn without acting. Pressure ticks occur.

**Cost system: move-based, not energy or cooldown.** Each level has a move budget (typically 12–25 moves). Every base action except Pass costs exactly one move. This is non-negotiable: move-based economy makes decisions legible (the counter is right there), avoids free-to-play energy gate feel, and preserves pure puzzle tension. Moves do not regenerate. When moves run out, the player can only Pass — meaning the board runs to resolution on its own momentum, which is often intentional and sometimes the winning line.

**Consumable tools (4 items):**

1. **Freeze Pod** — target one node; it gains 0 pressure for the next 3 ticks, cannot burst, cannot propagate. Its existing pressure is preserved. Does not cost a move. Use: buying a turn when a Timer is about to catastrophe.
2. **Pressure Siphon** — target one node; drain 40 pressure from it into the player's "bank." On a later turn (free action), inject banked pressure into any node at up to 40. Does not cost a move to deploy or inject. Use: hand-crafting chain starts.
3. **Stabilizer** — target a 1-hex radius (7 nodes including center); all gain insulated modifier for one tick — they cannot receive shoves. Costs a move. Use: containing a bad cascade.
4. **Reroute Lance** — target a node; force its next burst to fire in a single chosen direction at 100% magnitude (overrides Vent/Mirror rules). One-shot. Costs a move. Use: threading chains through tight corridors.

**Decision space on a given turn.** The player is choosing between three axes simultaneously: *which node to affect* (proximity to threshold, role in planned chain), *which action to use* (merge builds, isolate defends, trigger commits), and *whether to spend a move at all* (passing accelerates the board — sometimes correct, sometimes fatal). A good turn resolves all three with one action that does multiple things: merging two critical nodes both reduces board pressure and sets up a chain start. The game rewards compound moves and punishes single-purpose ones.

---

## 5. PRESSURE TIMING SYSTEM

**Escalation numbers.** Pressure is a 0–100 scale per node. Every turn's tick adds pressure based on node type (see §3), typically +5 to +15 per Cell-class node. A standard Cell reaches burst in ~10 turns from empty, ~5 from half, ~2–3 from critical. Thresholds are universal across node types — only the *rate* varies.

**Threshold crossings.**
- **0–24: Stable.** Cool color, gentle ambient motion. No propagation risk.
- **25–49: Swelling.** Warming color, visible pulse synced to global tick. Can receive shoves without bursting.
- **50–74: Critical.** Saturated warning color, faster pulse with a slight jitter. Any shove ≥25 will push it volatile.
- **75–99: Volatile.** The node is armed. It emits a faint crackling aura at its border. Next tick or any shove will detonate it. This is the "act now" zone.
- **100: Burst.** Resolves immediately in the next wave step.

**Reading danger at a glance.** Three overlapping signals for redundancy: (1) color temperature per state (§8), (2) pulse rate — a Volatile node pulses ~3× faster than a Swelling one, pre-attentive, (3) an edge glow whose thickness scales with pressure. A player should be able to squint at the board and immediately identify the two or three most dangerous nodes. If they can't, the level design has failed, not the player.

**Catastrophic failure vs. recoverable bad state.** 
- **Recoverable bad:** multiple nodes at Critical or Volatile, but no Timer at 0 segments, no Contaminant spread in progress, and the player still has ≥3 moves. This is *supposed* to happen in mid-level — it's the tension the design is built on.
- **Catastrophic:** any of: a Timer hits 0 segments uncontrolled (the 3× burst with +50 shoves almost always triggers a board-wide chain the player didn't author), a Contaminant spreads to a node adjacent to a rescue target, or the player runs out of moves with ≥2 Volatile nodes still active on an objective they haven't met. These states are usually unrecoverable within 1–2 turns. The game should not pretend otherwise — when the board is lost, show the player it's lost and let them restart fast (<3 seconds to retry).

**Waiting too long, mechanically.** Each turn the player Passes or spends moves on non-reductive actions, the *average* board pressure rises by ~10% of capacity. By turn 8 of a 15-move level on average tuning, a passive player is looking at half the board at Critical or higher, with chains they can no longer control because the Vents are pointed wrong and their Siphon is spent. The visual signature is unmistakable: the board goes from two-thirds cool tones to two-thirds hot tones within three turns of peak tension. That color flip *is* the tell.

---

## 6. LEVEL STRUCTURE AND PROGRESSION

**Chapter structure.** Six chapters at launch, ~20 levels each, ~120 levels total. Thematic arc runs from small and contained to large and systemic:

1. **The Boiler Room** — intro fundamentals. Cells, Vents, Bulwarks only. "Pop clusters under move cap" objective dominates.
2. **The Plumbing** — Conduits and Mirrors introduced. Objective: rescue trapped targets. The chapter teaches spatial routing.
3. **The Greenhouse** — Contaminants introduced. Objective: cleanse zones. Introduces spreading threats and the cost of inaction.
4. **The Clockworks** — Timers and Amplifiers introduced. Objective: clear all critical nodes, now with deadlines. The chapter teaches commitment.
5. **The Reactor Core** — Reactors and Sinks introduced. Objective: charge reactors in sequence. Long-horizon planning chapter.
6. **The Depths** — Substrate layer introduced, all prior mechanics compound. Objective: mixed per level. Mastery chapter.

**Layering.** Each chapter introduces at most two new node types and at most one new objective type. The first three levels of every chapter are tutorial-shaped (mechanic in isolation, then in simple combination, then in a minimal puzzle). Levels 4–15 are the chapter's main body. Levels 16–20 are high-difficulty combinatorial tests that assume full mastery of the chapter's content.

**Difficulty curve.** Three dimensions ramp independently: *board size* (more cells = more to track), *volatile node density* (more simultaneous threats), and *objective compounding* (chapter 4 and later stack objectives — "clear criticals AND rescue target AND don't let Contaminants spread"). The curve is not smooth; it's a staircase. Every third level should be noticeably harder than the two before it, and every tenth level should be a genuine wall that forces skill consolidation.

**Mastery moment.** The transition from reactive to deliberate play happens on a specific level — Chapter 4, Level 7 (tentative number, to be confirmed in playtest). It's the first level designed so that no reactive play can succeed: the player must plan a 3-wave chain from turn one, using an Amplifier and two Vents they must rotate before any node reaches Critical. Players who try to play turn-by-turn fail. Players who pause, read the board, and author the chain succeed. The moment a player beats this level, they internalize that Pressure Chain is a planning game wearing puzzle clothes. Every subsequent level assumes that understanding. The level exists specifically to force this cognitive shift — it is the pedagogical spine of the game.

---

## 7. SCORING AND CHAIN VALUE SYSTEM

**Chain quality measurement.** Every chain produces a score: `sum(release_value of each node bursting) × wave_multiplier × efficiency_bonus`. Wave multiplier scales with chain length: 1-wave = 1.0×, 2-wave = 1.3×, 3-wave = 1.7×, 4-wave = 2.2×, 5+wave = 3.0× and a special designation. Efficiency bonus rewards completing the level objective *within the chain* — a chain that simultaneously clears all critical nodes and rescues the target earns +50%.

**Multipliers and what they reward.**
- **Amplifier participation** (1.5× to the downstream waves): rewards routing, not just triggering.
- **Objective-in-chain** (+50% efficiency): rewards planning the *winning move* as a chain rather than cleanup.
- **Move conservation** (bonus = unused_moves × 100): rewards finishing under budget, which pushes players toward fewer, denser actions.
- **Unbroken chain** (no player input during resolution): the default state, but explicitly rewarded so players don't feel punished for letting chains run.

**Display without cluttering.** Score is shown only at chain resolution — a single floating number rises from the center of the chain's origin node, scales with the wave multiplier, and settles into the top-right HUD. No running score ticker, no per-node popups, no combo meter crawling the screen during play. During play, the HUD shows exactly three things: moves remaining, objective progress, and a single pressure-gauge silhouette showing the board's aggregate pressure. Everything else is on the board itself.

**Highest-tier chain.** Five or more waves is a **Cascade**. A Cascade triggers a distinct audio sting (§9), a brief time dilation (resolution plays at 60% speed for clarity), and the screen edges get a single golden pulse. It does not stop play, does not demand acknowledgment, does not require a popup. The player feels it without being interrupted. A Cascade is the implicit best-play target, not a marketed feature — earning one should feel like the game quietly acknowledging a thing you did, not a slot machine paying out.

---

## 8. VISUAL DESIGN LANGUAGE

**Color language per state.** Pressure maps to a cool-to-hot gradient, but with *shape* and *motion* doing equal work — never color alone.
- Stable: deep teal, low saturation.
- Swelling: desaturated amber.
- Critical: saturated orange.
- Volatile: red-white, high saturation.
- Locked/Insulated: neutral slate gray with a visible metallic ring.

**Colorblind solution:** every state additionally carries a unique geometric badge on the node's surface — a small symbol (circle, crescent, triangle, starburst, cross) that identifies state without color. High-contrast mode replaces the gradient with a luminance-only ramp (dark to bright) and thickens the state badges. This is not a toggle buried in accessibility menus — it's exposed on the first-run screen alongside sound and difficulty.

**Motion grammar.** Pressure building reads as a slow inhale: nodes gently swell and contract at a rate that increases with pressure (1Hz at Stable, 3Hz at Volatile). A chain resolving reads as an exhale: bursts emit a clean expanding ring, the shove into neighbors is a visible directional streak (not a generic particle cloud), and each wave of the chain is clearly separated by ~150ms of visual breath. The player sees the ring, sees the streak, sees where the next wave will hit, and feels the rhythm. Motion is the chain's teacher.

**Negative space rules.** The board is "readable" when ≤30% of nodes are at Critical or above, and no two Volatile nodes are adjacent. The board is "chaos" when ≥50% are Critical+, or any three adjacent nodes are all Volatile. Levels are *designed to move through* chaos states toward resolution — but chaos should always last ≤2 turns. If a level sits in chaos for 3+ turns without the player having a clear action, the level is broken and gets cut or retuned. This is a hard guardrail, not a guideline.

**Visual motif.** **The ring.** Every pressure state is a ring (node outline), every burst is a ring (expanding), every chain wave is a ring (concentric), every objective target is a ring (progress arc), the chapter map is rings within rings. The ring is pressure visualized — a container under strain, a release propagating outward. One motif, applied everywhere, at every scale. Any visual element that breaks the ring logic requires a specific justification.

---

## 9. AUDIO DESIGN LANGUAGE

**Pressure build.** A low, sustained drone that layers additional harmonic partials as global board pressure rises. No beeping, no ticking — the danger signal is *harmonic density*. A quiet board is a single sine tone; a board on the edge has six partials stacking into something that feels almost dissonant. Individual Volatile nodes add a faint, high, localized flutter positioned in stereo corresponding to their board location. The player can hear where the danger is.

**Chain resolution.** Each burst is a short, pitched, percussive hit. The pitch steps *up* with each successive wave of a chain — wave 1 is the root, wave 2 is a fifth, wave 3 is an octave, wave 4 is a tenth, wave 5+ resolves on a major third above the octave. A chain is literally a melodic phrase; a Cascade resolves on a consonant chord. The player hears the chain's shape and quality before they see the score.

**Failure vs. success.** Success collapses the pressure drone to silence over ~1 second, leaving only the final chain's pitched tail ringing. Failure (catastrophic burst) inverts: the drone spikes into full harmonic stack, distorts briefly, then cuts to a dead silence held for one full second before the UI returns. The absence of sound is the failure signature — not a buzzer, not a sad trombone. Silence after catastrophe.

**Audio motif principle.** **Sound is pressure, silence is resolution.** Every audio decision must serve this. If a sound effect does not encode pressure state, chain progression, or the relief of release, it should not be in the game. This kills the instinct to add UI click sounds, achievement stingers, and ambient "life" — all of which would corrupt the signal-to-noise ratio of the core mechanic.

---

## 10. FAILURE ANALYSIS AND TUNING GUARDRAILS

**The five most likely fun-killers:**

1. **Visual unreadability during chains.** *Symptom:* players can't tell what happened after a chain resolves. *Root cause:* too many simultaneous bursts, particle overload, camera not focusing attention. *Guardrail:* wave-based resolution with mandatory 150ms inter-wave pauses, camera subtly framing the chain's bounding box, and a hard cap on concurrent particle systems (if the engine is asked to render more than N bursts simultaneously, later bursts queue into the next wave). The ring motif (§8) exists partly to enforce this — rings are cheap to render cleanly.

2. **Solution determinism.** *Symptom:* players find one solution per level and the game becomes a memorization exercise. *Root cause:* levels that admit only one chain path. *Guardrail:* every hand-authored level must have at least two distinct solutions of comparable score (verified by an internal solver). Levels with one solution get cut or redesigned. Solver tool is part of the level editor pipeline, not an afterthought.

3. **Consumable dependency.** *Symptom:* players feel they can't beat levels without spending Freeze Pods and Siphons, the game feels pay-to-win. *Root cause:* difficulty tuned assuming consumable use. *Guardrail:* every level must be fully solvable with zero consumables, verified by the solver. Consumables reduce move count or increase score ceiling — they never gate completion. This is the line between a puzzle game with store support and a predatory monetization product, and we do not cross it.

4. **Timer node feels unfair.** *Symptom:* players blame the game, not themselves, when Timers detonate. *Root cause:* Timer state was not legible enough in the 1–2 turns before detonation. *Guardrail:* Timers get the most aggressive visual treatment in the game — their segment ring is always readable at a glance, their final-segment state has a unique audio cue (a single hollow tick, audible under any board density), and their catastrophic burst plays a brief "this is what you should have done" ghost animation showing the chain they could have triggered. The player must feel their own mistake, not the game's.

5. **Mid-game plateau.** *Symptom:* players stop progressing around chapter 3–4 and churn. *Root cause:* the reactive-to-deliberate cognitive shift (§6) didn't land, so players hit a wall they can't climb without changing how they think. *Guardrail:* the mastery-moment level is playtested *obsessively*, with explicit instrumentation measuring time-to-first-solve, attempts-before-solve, and post-solve difficulty curve completion rate. If <60% of players who reach that level eventually solve it, the level is redesigned — not made easier, but restructured to teach the lesson more clearly. The wall stays; the ramp to it gets rebuilt.

**The single most important playtesting question for the first prototype:** *When a chain resolves across 3+ waves, can the player articulate — unprompted, after the chain ends — which node they triggered, why they triggered it, and whether the chain did what they expected?* If yes, the core mechanic works and everything else is tuning. If no, the game doesn't exist yet regardless of how polished the surrounding systems are. Everything in this document depends on that answer being yes.

---

## 11. FIRST PROTOTYPE SCOPE

**Node types (exactly 4):** Cell, Vent, Bulwark, Amplifier. This covers: baseline fuel (Cell), directional control (Vent), board shaping (Bulwark), and the strategic build-around (Amplifier). Timers, Contaminants, Reactors, Conduits, Mirrors, and Sinks are all deferred — they are variations on tension and routing that only matter once the core loop proves legible.

**Player actions (exactly 3):** Merge, Vent-redirect, Trigger early. No Isolate (it's a defensive utility that only matters on harder boards). No consumables at all — the prototype tests whether the base game has a core before we test whether the store has a product.

**Objective type (exactly 1):** Pop clusters under move cap. Chosen because it tests chain authorship directly — the player is scored on *how* they clear the board, not merely *whether* they do. Rescue and cleanse objectives introduce spatial constraints that would muddy the signal we're trying to read. Reactor charging requires long-horizon play the prototype hasn't earned the right to ask for yet.

**What is being tested.** Three questions, in order of priority:

1. *Does the player experience chain authorship?* Do they plan a multi-wave chain, trigger it intentionally, and feel the outcome as earned — not lucky? This is the mastery-moment question in miniature and answers whether the game exists.

2. *Is the board readable under pressure?* Can a new player, within their first 10 minutes, identify the most dangerous node, the best Vent to rotate, and the likely chain path — without tutorial text? This is the clarity question, and failing it means the visual language (§8) needs to be rebuilt before any further work.

3. *Does a 3-wave chain feel different from a 1-wave chain?* Mechanically the score multiplier makes it different. But does it *feel* different in the hands — does the audio-visual-tactile bundle scale with chain depth? If a 3-wave chain feels like three 1-wave chains in a row, the game is additive and will flatten; it must feel multiplicative.

The prototype needs 8–12 hand-authored levels of increasing complexity within these constraints, an internal solver to verify each has ≥2 solutions, and a playtest harness that records every action with timestamps so we can reconstruct decision-making after the fact. Anything beyond this scope is noise until these three questions are answered.

---

*End of document.*
