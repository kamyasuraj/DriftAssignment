# Claude Token Usage — Development Log

> A running log of Claude token consumption across the DriftAssignment build. Updated at every phase boundary. Included in the deliverable bundle for transparency about AI-assisted development spend.

## ⚠ Important caveat

Numbers in this table are **agent-side estimates** based on message count and rough per-message averages. They are **not authoritative** — Claude does not have direct visibility into its own token accounting from within a session.

**For final submission, pull the real totals from the Anthropic Console:**
- Dashboard: **https://console.anthropic.com/usage**
- Filter by date range covering the project window
- Sum "Input tokens" and "Output tokens" for the applicable API key / project

The estimates below are useful as *relative* signal (which phases were expensive vs. cheap) but should not be treated as accurate for billing.

---

## Estimate methodology

- **Input tokens per turn** — averaged assuming typical Claude Code turn includes: system prompt + accumulating conversation history + tool results. Estimates grow with conversation length due to context accumulation.
- **Output tokens per turn** — averaged from typical response length (usually 200–2000 tokens depending on task).
- **Turns per phase** — counted from the phase's user↔assistant exchanges.
- Estimates use Claude Opus 4.7 (1M context) pricing tier for reference:
  - Input: ~$15 / 1M tokens
  - Output: ~$75 / 1M tokens

The numbers below are order-of-magnitude, not precise.

---

## Phase-by-phase log

| Phase | Approx turns | ~Input tokens | ~Output tokens | Notes |
|---|---|---|---|---|
| **Phase 0** — Project setup + docs | ~30 | ~250K | ~40K | Big first commit: docs bundle, .editorconfig, folder tree, asmdefs, Android target |
| **Phase 1** — Desert circuit env | ~40 | ~500K | ~60K | Terrain, sand layers, HDRI, EasyRoads3D circuit (heavy MCP reflection into ER API), 84 track-side props |
| **Phase 2** — Car physics + damage-prep audio | ~50 | ~800K | ~90K | RMCar26 wired, CarController + Drivetrain/GearBox/HandBrake/SteeringAssist scaffolded, materials URP-fixed, Cinemachine chase, reverse logic added, CarAudio + SoundLibrary SO |
| **Phase 7** — Damage system | _pending_ | | | Dent + break on RMCar26 panels |
| **Phase 8** — Polish (SFX / particles / post-process) | _pending_ | | | Audio hook-in beyond current CarAudio, particles, bloom |
| **Phase 3** — Camera cycle (chase/hood/cinematic/look-back) | _pending_ | | | Cinemachine cams + input cycle |
| **Phase 4** — Touch HUD | _pending_ | | | Steering arrows, pedals, RPM, gear, speed |
| **Phase 5** — Tuning menu | _pending_ | | | Sliders wired to TuningState |
| **Phase 6** — Preset system (0–13) | _pending_ | | | TuningPreset SOs + picker |
| **Phase 9** — Optimization pass | _pending_ | | | Cross-cutting, see `Doc/OPTIMIZATION.md` |
| **Phase 10** — Character enter/exit (stretch) | _pending_ | | | Only if time permits |
| **Phase 11** — Documentation finalization + PDF export | _pending_ | | | Ship |
| **Running total (estimated)** | ~120 | **~1.55M** | **~190K** | Rough estimate — **verify via Anthropic Console** |

---

## Non-phase burn (also non-trivial)

Some token spend does not slot neatly into a phase:

- **Asset selection / evaluation** — inspecting LogicGo pack, pivoting to RMCar26, evaluating desert props → probably ~150K tokens of Explore + reflection queries
- **URP material fixes** (RMCar26 pink → URP/Lit swap; prop packs same fix) → ~50K tokens
- **MCP diagnostic churn** — connecting/disconnecting Unity MCP, restart cycles, ToolSearch queries → ~100K tokens
- **Documentation writing** — REQUIREMENTS, ARCHITECTURE, IMPLEMENTATION_LOG, OPTIMIZATION, PROJECT_SETUP, CREDITS, CLAUDE.md, PDF pipeline → ~200K input, ~100K output

---

## Rough cost estimate (informational only)

At current running total (~1.55M input, ~190K output) using Opus 4.7 (1M context) pricing:

```
Input:  1.55M × $15 / 1M = ~$23.25
Output: 0.19M × $75 / 1M = ~$14.25
                   Total: ~$37.50
```

**Again — check the Anthropic Console for the real number.** Depending on cache-read discounts (Claude API prompt caching gives 90% discount on cached input), real spend is likely **significantly lower** than the naive estimate above.

---

## Recommended actions before final delivery

1. Pull authoritative usage from https://console.anthropic.com/usage for the project date range
2. Replace the estimated totals in this doc with the real numbers
3. Include a note about which model was used (Opus 4.7 in a 1M-context configuration) and whether prompt caching was active
4. Add a screenshot of the console usage graph to `Doc/reference/token_usage_console.png` (optional but strong signal for the interviewer)
