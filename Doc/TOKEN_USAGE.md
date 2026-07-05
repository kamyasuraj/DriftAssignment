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

| Phase | Status | Approx turns | ~Input tokens | ~Output tokens | Notes |
|---|---|---|---|---|---|
| **Phase 0** — Project setup + docs | ✅ done | ~30 | ~250K | ~40K | Docs bundle, .editorconfig, folder tree, asmdefs, Android target |
| **Phase 1** — Desert circuit env | ✅ done | ~40 | ~500K | ~60K | Terrain, sand layers, HDRI, EasyRoads3D circuit, 84 track-side props |
| **Phase 2** — Car physics | ✅ done | ~50 | ~800K | ~90K | RMCar26 wired, `CarController` + Drivetrain/GearBox/HandBrake/SteeringAssist, materials URP-fixed, reverse logic |
| **Phase 3** — Camera cycle (chase / cinematic / broadcast / look-back) | ✅ done | ~15 | ~200K | ~25K | Cinemachine 3-cam cycle, hold-to-look-back, `CameraRig` priority pattern |
| **Phase 4** — Touch HUD | ✅ done | ~35 | ~450K | ~55K | Steering arrows, pedals, RPM/gear/speed readouts, `TouchInputProvider`, keyboard fallback |
| **Phase 5** — Settings / tuning panel | ✅ done | ~30 | ~400K | ~50K | 6-tab settings, drivetrain / transmission / camber / audio / graphics / about, live TuningState binding |
| **Phase 6** — Preset system (0–13) | ⏭ deferred | – | – | – | Sliders + drivetrain shipped in Phase 5; numbered-preset picker not built (out of scope after Phase 5 covered the tuning surface) |
| **Phase 7** — Damage system | ✅ done | ~40 | ~500K | ~65K | `ImpactReceiver`, `PaintDamage` clone-material trick (saved 132 MB), `DamageCascade`, `ImpactVfx` sparks + dust, `CarHealth` |
| **Phase 8** — Polish (post-FX / SFX / particles) | ✅ done | ~20 | ~250K | ~35K | Desert URP post-process profile, 4-source engine mixer, tire screech, skid trails |
| **Phase 9** — Optimization pass | ✅ done | ~25 | ~300K | ~45K | 12-item catalogue, StressTestCapture stress test, before/after commit, on-device M31 validation |
| **Phase 10** — Character enter/exit (stretch) | ⏭ skipped | – | – | – | Deferred per user — not on the critical path for the interview submission |
| **Phase 11** — Documentation finalization + PDF export | 🔄 in progress | ~10 | ~120K | ~20K | OPTIMIZATION consolidated, PDF pipeline via Chrome headless, all Doc PDFs generated |
| **Running total (estimated)** | – | ~295 | **~3.77M** | **~485K** | Rough estimate — **verify via Anthropic Console** |

---

## Non-phase burn (also non-trivial)

Some token spend does not slot neatly into a phase:

- **Asset selection / evaluation** — inspecting LogicGo pack, pivoting to RMCar26, evaluating desert props → probably ~150K tokens of Explore + reflection queries
- **URP material fixes** (RMCar26 pink → URP/Lit swap; prop packs same fix) → ~50K tokens
- **MCP diagnostic churn** — connecting/disconnecting Unity MCP, restart cycles, ToolSearch queries → ~100K tokens
- **Documentation writing** — REQUIREMENTS, ARCHITECTURE, IMPLEMENTATION_LOG, OPTIMIZATION, PROJECT_SETUP, CREDITS, CLAUDE.md, PDF pipeline → ~200K input, ~100K output

---

## Rough cost estimate (informational only)

At the updated running total (~3.77M input, ~485K output) using Opus 4.7 (1M context) pricing:

```
Input:  3.77M × $15 / 1M = ~$56.55
Output: 0.485M × $75 / 1M = ~$36.38
                    Total: ~$92.93
```

**Again — check the Anthropic Console for the real number.** Depending on cache-read discounts (Claude API prompt caching gives 90% discount on cached input), real spend is likely **significantly lower** than the naive estimate above.

---

## Recommended actions before final delivery

1. Pull authoritative usage from https://console.anthropic.com/usage for the project date range
2. Replace the estimated totals in this doc with the real numbers
3. Include a note about which model was used (Opus 4.7 in a 1M-context configuration) and whether prompt caching was active
4. Add a screenshot of the console usage graph to `Doc/reference/token_usage_console.png` (optional but strong signal for the interviewer)
